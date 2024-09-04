using ConsoleControlSample.WPF1.Commands;
using ConsoleControlSample.WPF1.Utility;
using MVVMEssentials.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ConsoleControlSample.WPF1.ViewModel
{
    /// <summary>
    /// The Main ViewModel.
    /// </summary>
    public class MainWindowViewModel : ErrorsViewModel
    {
        private string _processState;

        private string _errorContent;

        public string ErrorContent
        {
            get { return _errorContent; }
            set
            {
                if (_errorContent != value)
                {
                    _errorContent = value;
                    OnPropertyChanged(nameof(ErrorContent));
                }
            }
        }

        // TODO I don't need this property?
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

        public RepoBrowserViewModel RepoBrowserViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel(RepoBrowserViewModel repoBrowserComponentViewModel, ConsoleControlViewModel consoleControlViewModel)
        {
            RepoBrowserViewModel = repoBrowserComponentViewModel;
            ConsoleControlViewModel = consoleControlViewModel;

            RepoBrowserViewModel.ErrorsChanged += ErrorsViewModel_ErrorsChanged;
        }

        private void ErrorsViewModel_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            ErrorsViewModel errorsViewModel = sender as ErrorsViewModel;

            if (errorsViewModel != null)
            {
                if (errorsViewModel.HasErrors)
                {
                    IEnumerable errors = errorsViewModel.GetErrors(e.PropertyName);
                    if (errors != null)
                    {
                        // Just take the last error.
                        foreach (string error in errors)
                        {
                            ErrorContent = error;
                        }
                    }
                }
                else
                {
                    ErrorContent = null;
                }
            }
        }
    }
}
