using ConsoleControlSample.WPF1.Commands;
using ConsoleControlSample.WPF1.Utility;
using MVVMEssentials.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ConsoleControlSample.WPF1.ViewModel
{
    public class ConsoleControlViewModel : ViewModelBase
    {
        private GitInterface _gitInterface;

        /// <summary>
        /// Gets the start command prompt command.
        /// </summary>
        public ICommand StartCommandPromptCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the start new process command.
        /// </summary>
        /// <value>
        /// The start new process command.
        /// </value>
        public ICommand StartNewProcessCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the stop process command.
        /// </summary>
        /// <value>
        /// The stop process command.
        /// </value>
        public ICommand StopProcessCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the clear output command.
        /// </summary>
        public ICommand ClearOutputCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleControlViewModel"/> class.
        /// </summary>
        public ConsoleControlViewModel()
        {
            StartCommandPromptCommand = new StartCommandPromptCommand(this);
            StartNewProcessCommand = new StartNewProcessCommand();
            StopProcessCommand = new StopProcessCommand();
            ClearOutputCommand = new ClearOutputCommand();
        }

        public GitInterface GetGitInterface()
        {
            return _gitInterface;
        }

        public void SetGitInterface(GitInterface gitInterface)
        {
            if (_gitInterface != gitInterface)
            {
                _gitInterface = gitInterface;
            }
        }
    }
}
