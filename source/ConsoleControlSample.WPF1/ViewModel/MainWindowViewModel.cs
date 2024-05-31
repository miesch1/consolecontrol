using ConsoleControlSample.WPF1.Commands;
using ConsoleControlSample.WPF1.Utility;
using MVVMEssentials.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ConsoleControlSample.WPF1.ViewModel
{
    /// <summary>
    /// The Main ViewModel.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private string _processState;

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

        public ConsoleControlViewModel ConsoleControlViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel(ConsoleControlViewModel consoleControlViewModel)
        {
            ConsoleControlViewModel = consoleControlViewModel;

            //  Handle certain commands.
            //viewModel.StartCommandPromptCommand.Executed += new Apex.MVVM.CommandEventHandler(StartCommandPromptCommand_Executed);
            //viewModel.StartNewProcessCommand.Executed += new Apex.MVVM.CommandEventHandler(StartNewProcessCommand_Executed);
            //viewModel.StopProcessCommand.Executed += new Apex.MVVM.CommandEventHandler(StopProcessCommand_Executed);
            //viewModel.ClearOutputCommand.Executed += new Apex.MVVM.CommandEventHandler(ClearOutputCommand_Executed);
        }
    }
}
