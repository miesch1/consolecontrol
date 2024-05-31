using ConsoleControlAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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

        public void StartProcess(string fileName, string arguments)
        {
            // TODO: Initial directory is currently location of current EXE. Consider tracking user's last selected Repo.
            _workingDirectory = Directory.GetCurrentDirectory();
            _processInterface.StartProcess(fileName, arguments, _workingDirectory);
        }

        public void WriteInput(string input, out string stderr_str, out string stdout_str)
        {
            TaskCompletionSource<bool> IsProcessOutput = new TaskCompletionSource<bool>();

            bool? error = null;
            _processInterface.ProcessOutput += delegate (object sender, ProcessEventArgs args)
            {
                error = false;
                //IsProcessOutput.SetResult(false);
            };
            _processInterface.ProcessError += delegate (object sender, ProcessEventArgs args)
            {
                error = true;
                //IsProcessOutput.SetResult(true);
            };

            _processInterface.WriteInput(input);

            stderr_str = null;  // pick up STDERR
            stdout_str = null; // pick up STDOUT
        }

        public async Task<bool> WriteInputAsync(string input)
        {
            TaskCompletionSource<bool> IsProcessOutput = new TaskCompletionSource<bool>();

            string stderr_str;
            string stdout_str;

            bool? error = null;

            await Task.Run(() =>
            {
                _processInterface.ProcessError += delegate (object sender, ProcessEventArgs args)
                {
                    error = true;
                    stderr_str = args.Content;
                    //IsProcessOutput.SetResult(true);
                };
                _processInterface.ProcessOutput += delegate (object sender, ProcessEventArgs args)
                {
                    error = false;
                    stdout_str = args.Content;
                    //IsProcessOutput.SetResult(false);
                };
            })
            .ConfigureAwait(false);

            /* Call service asynchronously */
            await Task.Run(() =>
            {
                _processInterface.WriteInput(input);
            })
            .ConfigureAwait(false);

            //await IsProcessOutput.Task;
            //while (error == null)
            //{
            //    await Task.Yield();
            //}

            return true;
        }
    }
}
