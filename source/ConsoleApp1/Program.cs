using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

public class TimedInteractiveCommand
{
    private readonly Process _process;
    private StreamWriter _standardInput;
    private readonly object _syncLock = new object();
    private Task _outputTask;
    private Task _errorTask;
    private TaskCompletionSource<bool> _processExitedSource;
    private TaskCompletionSource<bool> _stderr_str_WriteComplete;
    private TaskCompletionSource<bool> _stdout_str_WriteComplete;

    private string _commandInProgress;
    private bool _startInProgress;

    // Regex to match typical command prompts like "C:\>" or "/home/user$"
    private readonly Regex _cmdPromptRegex = new Regex(@"^[A-Za-z]:\\.*>|^[A-Za-z0-9_/-]+@.*[$#]", RegexOptions.Multiline);

    public Action<CommandEventArgs> ProcessEvent;
    public Action<int> ProcessExited;

    public TimedInteractiveCommand(string fileName, string arguments)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
    }

    public void StartProcess(int timeoutMilliseconds = 5000)
    {
        _startInProgress = true;
        _processExitedSource = new TaskCompletionSource<bool>();
        _process.Exited += OnProcessExited;

        //  Start the process.
        try
        {
            _process.Start();
        }
        catch (Exception e)
        {
            //  Trace the exception.
            Console.WriteLine(e.ToString());
            return;
        }

        ProcessEvent?.Invoke(new CommandEventArgs(ProcessType.Start));

        _standardInput = _process.StandardInput;

        // Begin reading from output and error streams synchronously in separate tasks
        _outputTask = ReadOutputAsync(_process.StandardOutput.BaseStream, false);
        _errorTask = ReadOutputAsync(_process.StandardError.BaseStream, true);

        WaitForPromptAsync(timeoutMilliseconds).GetAwaiter().GetResult();

        _startInProgress = false;
    }

    private async Task ReadOutputAsync(Stream stream, bool isErrorStream)
    {
        byte[] buffer = new byte[4096]; // 4 KB buffer
        int bytesRead;
        StringBuilder outputBuilder = new StringBuilder();

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            // NOTE: This design accomodates Output to be parsed for more than one Process Event in each Read loop.
            lock (_syncLock)
            {
                string output = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                outputBuilder.Append(output);

                // Strip off the input that is echoed in the output. Note, it is possible there is more than just the input in the output that was read.
                // Possibly related to race condition between StandardError being read and the command prompt returning before StandardOutput is read.
                if (_commandInProgress != null && output.StartsWith(_commandInProgress))
                {
                    outputBuilder.Remove(0, _commandInProgress.Length);
                }

                // Are any prompts in the output? Only need to check Output, not Error (error won't contain prompt).
                if (!isErrorStream)
                {
                    string commandOutput;
                    string commandPrompt;

                    // NOTE: outputBuilder is modified by MatchPrompt.
                    // If syncronization is working correctly, should only be one command prompt in output.
                    while (MatchPrompt(outputBuilder, out commandOutput, out commandPrompt))
                    {
                        if (!string.IsNullOrEmpty(commandOutput))
                        {
                            ProcessOutput(commandOutput, isErrorStream);
                        }

                        if (!string.IsNullOrEmpty(commandPrompt))
                        {
                            ProcessEvent?.Invoke(new CommandEventArgs(ProcessType.CommandPrompt, commandPrompt));
                        }
                    }
                }

                // If data is waiting, read it now that we have processed what we could.
                // HACK: If data is continously streaming, outputBuilder will just fill up and this approach will not work.
                // Would rather determine if data is really available to be read (rather than assume) but Stream does not support Peek. 
                if (output.Length == buffer.Length && output.Substring(buffer.Length - 1, 1) != Environment.NewLine)
                {
                    // Read the stream to fill the buffer again.
                    continue;
                }

                if (outputBuilder.Length > 0)
                {
                    ProcessOutput(outputBuilder.ToString(), isErrorStream);

                    outputBuilder.Clear();
                }

                if (isErrorStream)
                    _stderr_str_WriteComplete?.TrySetResult(false);
                else
                    _stdout_str_WriteComplete?.TrySetResult(true);
            }
        }
    }

    private void ProcessOutput(string commandOutput, bool isErrorStream)
    {
        if (_startInProgress)
        {
            ProcessEvent?.Invoke(new CommandEventArgs(isErrorStream ? ProcessType.Error : ProcessType.StartOutput, commandOutput));
        }
        else
        {
            ProcessEvent?.Invoke(new CommandEventArgs(isErrorStream ? ProcessType.Error : ProcessType.CommandOutput, commandOutput));
        }
    }

    private bool MatchPrompt(StringBuilder outputBuilder, out string commandOutput, out string commandPrompt)
    {
        commandOutput = null;
        commandPrompt = null;

        Match match = _cmdPromptRegex.Match(outputBuilder.ToString());
        if(match.Success)
        {
            // The part before the prompt is command output
            commandOutput = outputBuilder.ToString(0, match.Index);

            // The prompt itself
            commandPrompt = match.Value;

            // Remove processed part from the builder
            outputBuilder.Remove(0, match.Index + match.Length);
        }

        return match.Success;
    }

    public async Task<bool> WriteInputAsync(string input, int timeoutMilliseconds = 5000)
    {
        lock (_syncLock)
        {
            if (_process.HasExited)
                throw new InvalidOperationException("Process has exited.");

            _stdout_str_WriteComplete = new TaskCompletionSource<bool>();
            _stderr_str_WriteComplete = new TaskCompletionSource<bool>();

            _commandInProgress = input;

            _standardInput.Write(_commandInProgress);
            _standardInput.Flush();

            // Notify the consumer that input was written
            ProcessEvent?.Invoke(new CommandEventArgs(ProcessType.Input, _commandInProgress));
        }

        // Wait for the prompt to return
        string completedTask = await WaitForPromptAsync(timeoutMilliseconds);

        Task<bool> completedReadTask = await WaitForReadStreamAsync(timeoutMilliseconds);

        lock (_syncLock)
        {
            _commandInProgress = null;
        }

        return completedReadTask.Result;
        //return true;
    }

    private async Task<string> WaitForPromptAsync(int timeoutMilliseconds = 5000)
    {
        // Wait for the prompt to return
        var tcs = new TaskCompletionSource<bool>();
        StringBuilder commandOutput = new StringBuilder(); // Conceivable command output could span multiple events (if it is larger than read buffer)

        ProcessEvent += (e) =>
        {
            if (e.ProcessType == ProcessType.CommandOutput)
            {
                commandOutput.Append(e.Content);
            }
            else if (e.ProcessType == ProcessType.CommandPrompt)
            {
                tcs.TrySetResult(true);
            }
        };

        // Block until command output is received or timeout occurs
        var completedTask = await Task.WhenAny(tcs.Task, _processExitedSource.Task, Task.Delay(timeoutMilliseconds));

        if (completedTask == tcs.Task)
        {
            return commandOutput.ToString();
        }
        if (completedTask == _processExitedSource.Task)
        {
            return commandOutput.ToString();
        }
        else
        {
            throw new IOException("Timed out waiting for command prompt to return.");
        }
    }

    private async Task<Task<bool>> WaitForReadStreamAsync(int timeoutMilliseconds = 5000)
    {
        // Wait for either output or error to be read
        return await Task.WhenAny(_stdout_str_WriteComplete.Task, _stderr_str_WriteComplete.Task);
    }

    private void OnProcessExited(object sender, EventArgs e)
    {
        _processExitedSource?.TrySetResult(true);

        ProcessEvent?.Invoke(new CommandEventArgs(ProcessType.Exit, _process.ExitCode));

        //ProcessExited?.Invoke(_process.ExitCode);
    }

    public void Close()
    {
        lock (_syncLock)
        {
            _standardInput.Close();
            _process.WaitForExit();
            _process.Close();
        }
    }
}

public enum ProcessType
{
    Start,  // Created from Output
    StartOutput,  // Created from Output
    Input,
    CommandOutput,  // Created from Output
    CommandPrompt,  // Created from Output
    Error,
    Exit   // Created from Output
}

public class CommandEventArgs : EventArgs
{
    /// <summary>
    /// Gets the content.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets or sets the code.
    /// </summary>
    /// <value>
    /// The code.
    /// </value>
    public int? Code { get; }

    public ProcessType ProcessType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEventArgs"/> class.
    /// </summary>
    public CommandEventArgs(ProcessType processType) : this(processType, "", 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEventArgs"/> class.
    /// </summary>
    /// <param name="content">The content.</param>
    public CommandEventArgs(ProcessType processType, string content) : this(processType, content, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEventArgs"/> class.
    /// </summary>
    /// <param name="code">The code.</param>
    public CommandEventArgs(ProcessType processType, int code) : this(processType, "", code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEventArgs"/> class.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="code">The code.</param>
    public CommandEventArgs(ProcessType processType, string content, int code)
    {
        ProcessType = processType;
        Content = content;
        Code = code;
    }
}

class Program
{

    private static readonly object _syncLock = new object();

    static async Task Main(string[] args)
    {
        var cmd = new TimedInteractiveCommand("cmd.exe", "");

        cmd.ProcessEvent = (e) =>
        {
            //lock (_syncLock)
            //{
                switch (e.ProcessType)
                {
                    case ProcessType.Start:
                    case ProcessType.StartOutput:
                    case ProcessType.CommandPrompt:
                        Console.ForegroundColor = ConsoleColor.Gray;  // Output in white
                        break;
                    case ProcessType.Input:
                        Console.ForegroundColor = ConsoleColor.DarkCyan;  // Input in cyan
                        break;
                    case ProcessType.CommandOutput:
                        Console.ForegroundColor = ConsoleColor.DarkGreen;  // Output in green
                        break;
                    case ProcessType.Error:
                        Console.ForegroundColor = ConsoleColor.DarkRed;  // Error in red
                        break;
                    case ProcessType.Exit:
                        break;
                }

                Debug.Write($"<{e.ProcessType}>{e.Content}");

                Console.Write(e.Content);
                Console.ResetColor();
            //}
        };

        //cmd.ProcessExited = (exitCode) => Console.WriteLine($"Process exited with code: {exitCode}");

        // Simulated interactive session
        cmd.StartProcess();

        //// 1. List directory contents
        //if (!ExecuteCommand(cmd, "dir"))
        //    return;

        //// 2. Echo a string
        //if (!ExecuteCommand(cmd, "echo Hello, World!"))
        //    return;

        //// 3. Check git status in a non-repo directory
        //if (!ExecuteCommand(cmd, "git status"))
        //    return;

        //// 4. Change to a valid git repo directory
        //if (!ExecuteCommand(cmd, "cd C:\\MyRepo"))
        //    return;

        //// 5. Check git status in a valid repo
        //if (!ExecuteCommand(cmd, "git status"))
        //    return;

        //// 6. Exit the session
        //if (!ExecuteCommand(cmd, "exit"))
        //    return;

        // 1. List directory contents
        ExecuteCommand(cmd, "dir");

        //await cmd.WriteInputAsync("dir" + Environment.NewLine);
        //await cmd.WriteInputAsync("dir" + Environment.NewLine);
        //await cmd.WriteInputAsync("dir" + Environment.NewLine);
        //await cmd.WriteInputAsync("dir" + Environment.NewLine);
        //await cmd.WriteInputAsync("dir" + Environment.NewLine);
        //await cmd.WriteInputAsync("dir" + Environment.NewLine);

        // 2. Echo a string
        ExecuteCommand(cmd, "echo Hello, World!");

        // 3. Check git status in a non-repo directory
        ExecuteCommand(cmd, "git status");

        // 4. Change to a valid git repo directory
        ExecuteCommand(cmd, "cd C:\\MyRepo");

        // 5. Check git status in a valid repo
        ExecuteCommand(cmd, "git status");

        // 6. Exit the session
        ExecuteCommand(cmd, "exit");


        cmd.Close();

        Thread.Sleep(5000);
    }

    private static bool ExecuteCommand(TimedInteractiveCommand cmd, string command)
    {
        return cmd.WriteInputAsync(command + Environment.NewLine).GetAwaiter().GetResult();
    }
}