using ConsoleControlAPI;
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

        private bool _isInputEnabled;

        private string _processState;

        /// <summary>
        /// Gets the clear output command.
        /// </summary>
        public ICommand ClearOutputCommand
        {
            get;
        }

        public GitInterface GitInterface
        {
            get { return _gitInterface; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has input enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has input enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsInputEnabled
        {
            get { return _isInputEnabled; }
            set
            {
                if (_isInputEnabled != value)
                {
                    _isInputEnabled = value;
                    OnPropertyChanged(nameof(IsInputEnabled));
                }
            }
        }

        /// <summary>
        /// Gets or sets the state of the process.
        /// </summary>
        /// <value>
        /// The state of the process.
        /// </value>
        public ProcessInterface ProcessInterface
        {
            get { return _gitInterface.ProcessInterface; }
        }

        /// <summary>
        /// Gets or sets the state of the process.
        /// </summary>
        /// <value>
        /// The state of the process.
        /// </value>
        public string ProcessState
        {
            get { return _processState; }
            set
            {
                if (_processState != value)
                {
                    _processState = value;
                    OnPropertyChanged(nameof(ProcessState));
                }
            }
        }

        /// <summary>
        /// Gets the start command prompt command.
        /// </summary>
        public ICommand StartCommandPromptCommand
        {
            get;
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleControlViewModel"/> class.
        /// </summary>
        public ConsoleControlViewModel(GitInterface gitInterface)
        {
            StartCommandPromptCommand = new StartCommandPromptCommand(this);
            StartNewProcessCommand = new StartNewProcessCommand();
            StopProcessCommand = new StopProcessCommand();
            ClearOutputCommand = new ClearOutputCommand();

            _gitInterface = gitInterface;
            _isInputEnabled = false;
        }
    }
}
