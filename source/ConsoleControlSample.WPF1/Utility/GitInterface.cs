using ConsoleControlAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms.Design;
using System.Xml.Linq;

namespace ConsoleControlSample.WPF1.Utility
{
    public class GitInterface
    {
        private ProcessInterface _processInterface;

        private string _workingDirectory;

        public ProcessInterface ProcessInterface
        {
            get { return _processInterface; }
        }

        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set
            {
                if (_workingDirectory != value)
                {
                    _workingDirectory = value;
                }
            }
        }

        public GitInterface(ProcessInterface processInterface)
        {
            _processInterface = processInterface;
        }

        public async Task StartProcessAsync(string fileName, string arguments)
        {
            // TODO: Initial directory is currently location of current EXE. Consider tracking user's last selected Repo.
            _workingDirectory = Directory.GetCurrentDirectory();
            await _processInterface.StartProcess(fileName, arguments, _workingDirectory);
        }

        public async Task<ICollection<string>> ValidateSelectedDirectoryAsync(string folderPath)
        {
            ICollection<string> validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(folderPath))
                validationErrors.Add("Directory path cannot be empty or whitespace.");
            else if (!Directory.Exists(folderPath))
                validationErrors.Add("Directory path does not exist.");
            else
            {
                bool noCommandError = true;

                string newDirectoryRoot = Directory.GetDirectoryRoot(folderPath);
                if (newDirectoryRoot != Directory.GetDirectoryRoot(WorkingDirectory))
                {
                    // To change logical drives in cmd line, can't use 'cd'.
                    //_gitInterface.WriteInput($"{newDirectoryRoot.TrimEnd(Path.DirectorySeparatorChar)}", out stderr_str, out stdout_str);
                    noCommandError = await ExecuteCommandAsync($"{newDirectoryRoot.TrimEnd(Path.DirectorySeparatorChar)}");
                    if(!noCommandError)
                    {
                        validationErrors.Add("The supplied path is not a valid Git repo.");
                    }
                    //System.Threading.Thread.Sleep(200);
                    // while (error == null) { }
                    //error = null;
                }

                if (noCommandError)
                {
                    WorkingDirectory = folderPath;
                    noCommandError = await ExecuteCommandAsync($"cd {folderPath}");
                    if (!noCommandError)
                    {
                        validationErrors.Add("The supplied path is not a valid Git repo.");
                    }

                    if (noCommandError)
                    {
                        //System.Threading.Thread.Sleep(200);
                        //while (error == null) { }
                        //error = null;
                        noCommandError = await ExecuteCommandAsync($"git status");
                        //System.Threading.Thread.Sleep(200);
                        //while (error == null) { }
                        //error = null;

                        if (!noCommandError)
                        {
                            validationErrors.Add("The supplied path is not a valid Git repo.");
                        }
                    }
                }
            }
            //else if (!GitHelper.Instance.IsDirectoryValidRepo(folderPath))
            //    validationErrors.Add("The supplied path is not a valid Git repo.");

            return validationErrors;
        }

        public async Task<bool> ExecuteCommandAsync(string command)
        {
            bool commandOutput = await _processInterface.ExecuteCommandAsync(command);

            return commandOutput;
        }

        //public async Task<bool> WriteInputAsync(string input)
        //{
        //    TaskCompletionSource<bool> IsProcessOutput = new TaskCompletionSource<bool>();

        //    string stderr_str;
        //    string stdout_str;

        //    bool? error = null;

        //    await Task.Run(() =>
        //    {
        //        _processInterface.ProcessError += delegate (object sender, ProcessEventArgs args)
        //        {
        //            error = true;
        //            stderr_str = args.Content;
        //            //IsProcessOutput.SetResult(true);
        //        };
        //        _processInterface.ProcessOutput += delegate (object sender, ProcessEventArgs args)
        //        {
        //            error = false;
        //            stdout_str = args.Content;
        //            //IsProcessOutput.SetResult(false);
        //        };
        //    })
        //    .ConfigureAwait(false);

        //    /* Call service asynchronously */
        //    await Task.Run(() =>
        //    {
        //        _processInterface.WriteInput(input);
        //    })
        //    .ConfigureAwait(false);

        //    //await IsProcessOutput.Task;
        //    //while (error == null)
        //    //{
        //    //    await Task.Yield();
        //    //}

        //    return true;
        //}
    }
}
