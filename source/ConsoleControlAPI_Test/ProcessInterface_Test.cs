using ConsoleControlAPI;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Diagnostics;

namespace ConsoleControlAPI_Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        private string _workingDirectory;

        [Test]
        public void Test1()
        {
            ProcessInterface processInterface = new ProcessInterface();
            processInterface.ProcessEvent += ProcessInterface_ProcessEvent;
            _workingDirectory = Directory.GetCurrentDirectory();
            processInterface.StartProcess(@"C:\Program Files\Git\git-cmd.exe", string.Empty, _workingDirectory);

            bool isValid;
            isValid = IsPathValidRepo(processInterface, @"C:\Dir1");
        }

        private void ProcessInterface_ProcessEvent(object sender, ProcessEventArgs args)
        {
            switch (args.ProcessType)
            {
                case ProcessType.Start:
                    break;
                case ProcessType.StartOutput:
                case ProcessType.CommandPrompt:
                    ProcessInterface_ProcessOutput(sender, args);
                    break;
                case ProcessType.Input:
                    ProcessInterface_ProcessInput(sender, args);
                    break;
                case ProcessType.CommandOutput:
                    ProcessInterface_ProcessCommandOutput(sender, args);
                    break;
                case ProcessType.Error:
                    ProcessInterface_ProcessError(sender, args);
                    break;
                case ProcessType.Exit:
                    break;
            }
        }

        private bool IsPathValidRepo(ProcessInterface processInterface, string folderPath)
        {
            string stderr_str;
            string stdout_str;

            string newDirectoryRoot = Directory.GetDirectoryRoot(folderPath);
            if (newDirectoryRoot != Directory.GetDirectoryRoot(_workingDirectory))
            {
                // To change logical drives in cmd line, can't use 'cd'.
                processInterface.WriteInput($"{newDirectoryRoot.TrimEnd(Path.DirectorySeparatorChar)}");
            }

            processInterface.WriteInput($"git status");

            //WorkingDirectory = folderPath;
            //WriteInput($"cd {folderPath}", out stderr_str, out stdout_str);
            //System.Threading.Thread.Sleep(200);
            ////while (error == null) { }
            ////error = null;
            //WriteInput($"git status", out stderr_str, out stdout_str);
            //System.Threading.Thread.Sleep(200);
            ////while (error == null) { }
            ////error = null;

            //if (stderr_str != null)
            //{
            //    validationErrors.Add("The supplied path is not a valid Git repo.");
            //}

            return true;
        }

        private void ProcessInterface_ProcessError(object sender, ProcessEventArgs args)
        {
            Debug.WriteLine($"ProcessError: {args.Content}");
        }
        private void ProcessInterface_ProcessCommandOutput(object sender, ProcessEventArgs args)
        {
            Debug.WriteLine($"ProcessCommandOutput: {args.Content}");
        }

        private void ProcessInterface_ProcessOutput(object sender, ProcessEventArgs args)
        {
            Debug.WriteLine($"ProcessOutput: {args.Content}");
        }

        private void ProcessInterface_ProcessInput(object sender, ProcessEventArgs args)
        {
            Debug.WriteLine($"ProcessInput: {args.Content}");
        }
    }
}