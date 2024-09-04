using System;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;

namespace ConsoleControlAPI
{
    /// <summary>
    /// A ProcessEventHandler is a delegate for process input/output events.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
    public delegate void ProcessEventHandler(object sender, ProcessEventArgs args);

    /// <summary>
    /// A class the wraps a process, allowing programmatic input and output.
    /// </summary>
    public class ProcessInterface: IDisposable
    {
        /// <summary>
        /// Occurs when process output is produced.
        /// </summary>
        public event ProcessEventHandler ProcessEvent;

        /// <summary>
        /// The current process.
        /// </summary>
        private Process _process;

        /// <summary>
        /// The input writer.
        /// </summary>
        private StreamWriter _standardInput;

        private readonly object _syncLock = new object();

        private string _commandInProgress;

        private Task _outputTask;
        private Task _errorTask;
        private TaskCompletionSource<bool> _processExitedSource;
        private TaskCompletionSource<bool> _stderr_str_WriteComplete;
        private TaskCompletionSource<bool> _stdout_str_WriteComplete;

        private bool _startInProgress;

        /// <summary>
        /// Current process file name.
        /// </summary>
        private string _processFileName;

        /// <summary>
        /// Arguments sent to the current process.
        /// </summary>
        private string _processArguments;

        // Regex to match typical command prompts like "C:\>" or "/home/user$"
        private readonly Regex _cmdPromptRegex = new Regex(@"^[A-Za-z]:\\.*>|^[A-Za-z0-9_/-]+@.*[$#]", RegexOptions.Multiline);

        /// <summary>
        /// Gets a value indicating whether this instance is process running.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is process running; otherwise, <c>false</c>.
        /// </value>
        public bool IsProcessRunning
        {
            get
            {
                try
                {
                    return (_process != null && _process.HasExited == false);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the internal process.
        /// </summary>
        public Process Process
        {
            get { return _process; }
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        /// <value>
        /// The name of the process.
        /// </value>
        public string ProcessFileName
        {
            get { return _processFileName; }
        }

        /// <summary>
        /// Gets the process arguments.
        /// </summary>
        public string ProcessArguments
        {
            get { return _processArguments; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessInterface"/> class.
        /// </summary>
        public ProcessInterface()
        {
        }

        /// <summary>Finalizes an instance of the <see cref="ProcessInterface"/> class.</summary>
        ~ProcessInterface()
        {
            Dispose(true);
        }

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="native">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected void Dispose(bool native)
        {
            //if (_outputWorker != null)
            //{
            //    _outputWorker.Dispose();
            //    _outputWorker = null;
            //}
            //if (_errorWorker != null)
            //{
            //    _errorWorker.Dispose();
            //    _errorWorker = null;
            //}
            if (_process != null)
            {
                _process.Dispose();
                _process = null;
            }
            if (_standardInput != null)
            {
                _standardInput.Dispose();
                _standardInput = null;
            }
            //if (_outputReader != null)
            //{
            //    _outputReader.Dispose();
            //    _outputReader = null;
            //}
            //if (_errorReader != null)
            //{
            //    _errorReader.Dispose();
            //    _errorReader = null;
            //}
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Runs a process.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="arguments">The arguments.</param>
        public async Task StartProcess(string fileName, string arguments, string workingDirectory)
        {
            //  Create the process start info.
            var processStartInfo = new ProcessStartInfo(fileName, arguments);
            processStartInfo.WorkingDirectory = workingDirectory;

            await StartProcess(processStartInfo);
        }

        /// <summary>
        /// Runs a process.
        /// </summary>
        /// <param name="processStartInfo"><see cref="ProcessStartInfo"/> to pass to the process.</param>
        public async Task StartProcess(ProcessStartInfo processStartInfo, int timeoutMilliseconds = 5000)
        {
            //  Set the options.
            processStartInfo.UseShellExecute = false;
            processStartInfo.ErrorDialog = false;
            processStartInfo.CreateNoWindow = true;

            //  Specify redirection.
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;

            //  Create the process.
            _process = new Process();
            _process.EnableRaisingEvents = true;
            _process.StartInfo = processStartInfo;
            _process.Exited += Process_Exited;

            _startInProgress = true;
            _processExitedSource = new TaskCompletionSource<bool>();

            //  Start the process.
            try
            {
                _process.Start();
                ProcessEvent?.Invoke(this, new ProcessEventArgs(ProcessType.Start));
            }
            catch (Exception e)
            {
                //  Trace the exception.
                Trace.WriteLine("Failed to start process " + processStartInfo.FileName + " with arguments '" + processStartInfo.Arguments + "'");
                Trace.WriteLine(e.ToString());
                return;
            }

            //  Store name and arguments.
            _processFileName = processStartInfo.FileName;
            _processArguments = processStartInfo.Arguments;

            //  Create the readers and writers.
            _standardInput = _process.StandardInput;

            // Begin reading from output and error streams synchronously in separate tasks
            _outputTask = ReadOutputAsync(_process.StandardOutput.BaseStream, false);
            _errorTask = ReadOutputAsync(_process.StandardError.BaseStream, true);

            await WaitForCommandPromptAsync(timeoutMilliseconds);

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
                                ProcessEvent?.Invoke(this, new ProcessEventArgs(ProcessType.CommandPrompt, commandPrompt));
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
                ProcessEvent?.Invoke(this, new ProcessEventArgs(isErrorStream ? ProcessType.Error : ProcessType.StartOutput, commandOutput));
            }
            else
            {
                ProcessEvent?.Invoke(this, new ProcessEventArgs(isErrorStream ? ProcessType.Error : ProcessType.CommandOutput, commandOutput));
            }
        }

        private bool MatchPrompt(StringBuilder outputBuilder, out string commandOutput, out string commandPrompt)
        {
            commandOutput = null;
            commandPrompt = null;

            Match match = _cmdPromptRegex.Match(outputBuilder.ToString());
            if (match.Success)
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

        public void WriteInput(string input)
        {
            lock (_syncLock)
            {
                if (!IsProcessRunning)
                    throw new InvalidOperationException("Process has exited.");

                _stdout_str_WriteComplete = new TaskCompletionSource<bool>();
                _stderr_str_WriteComplete = new TaskCompletionSource<bool>();

                _standardInput.Write(_commandInProgress);
                _standardInput.Flush(); // Ensure that the input is actually sent to the process

                // Notify the consumer that input was written
                ProcessEvent?.Invoke(this, new ProcessEventArgs(ProcessType.Input, _commandInProgress));
            }
        }

        private async Task<string> WaitForCommandPromptAsync(int timeoutMilliseconds = 5000)
        {
            // Wait for the prompt to return
            var tcs = new TaskCompletionSource<bool>();
            StringBuilder commandOutput = new StringBuilder(); // Conceivable command output could span multiple events (if it is larger than read buffer)

            ProcessEvent += (sender, e) =>
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

        private async Task<bool> WaitForReadStreamAsync(int timeoutMilliseconds = 5000)
        {
            // Wait for either output or error to be read
            var delayTask = Task.Delay(timeoutMilliseconds);
            var completedTask = await Task.WhenAny(_stdout_str_WriteComplete.Task, _stderr_str_WriteComplete.Task, delayTask);

            if (completedTask == _stderr_str_WriteComplete.Task || completedTask == delayTask)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Stops the process.
        /// </summary>
        public void StopProcess()
        {
            //  Handle the trivial case.
            if (IsProcessRunning == false)
                return;

            //  Kill the process.
            _process.Kill();
        }

        public void Close()
        {
            // TODO: Trying to offer graceful shutdown. Need to update this, not sure why it is locked here...
            lock (_syncLock)
            {
                _standardInput.Close();
                _process.WaitForExit();
                _process.Close();
            }
        }

        /// <summary>
        /// Handles the Exited event of the currentProcess control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Process_Exited(object sender, EventArgs e)
        {
            //  Fire process exited.
            OnProcessExit(_process.ExitCode);

            //  Disable the threads.
            //_outputWorker.CancelAsync();
            //_errorWorker.CancelAsync();
            _standardInput = null;
            //_outputReader = null;
            //_errorReader = null;
            _process = null;
            _processFileName = null;
            _processArguments = null;
        }

        /// <summary>
        /// Fires the process exit event.
        /// </summary>
        /// <param name="code">The code.</param>
        private void OnProcessExit(int code)
        {
            _processExitedSource?.TrySetResult(true);

            ProcessEvent?.Invoke(this, new ProcessEventArgs(ProcessType.Exit, code));
        }

        public async Task<bool> ExecuteCommandAsync(string command, int timeoutMilliseconds = 5000)
        {
            _commandInProgress = command + Environment.NewLine;

            WriteInput(_commandInProgress);

            // Wait for the prompt to return
            string commandOutput = await WaitForCommandPromptAsync(timeoutMilliseconds);

            _commandInProgress = null;

            var task = await WaitForReadStreamAsync(timeoutMilliseconds);

            return task;
        }
    }
}
