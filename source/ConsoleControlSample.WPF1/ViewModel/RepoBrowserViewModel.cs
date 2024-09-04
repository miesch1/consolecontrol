using ConsoleControlSample.WPF1.Commands;
using ConsoleControlSample.WPF1.Utility;
using ConsoleControlSample.WPF1.ViewModel;
using MVVMEssentials.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Design;
using System.Windows.Input;

namespace ConsoleControlSample.WPF1.ViewModel
{
    public class RepoBrowserViewModel : ErrorsViewModel
    {
        private ConsoleControlViewModel _consoleControlViewModel;

        private bool _iSDirectoryValidRepo;

        private bool _isUpdating;

        private string _placeholderText;

        private readonly ObservableCollection<string> _repos;

        private string _selectedDirectory;

        private string _selectedRepo;

        //public GitInterface GitInterface
        //{
        //    get { return _gitInterface; }
        //}

        public bool IsDirectoryValidRepo
        {
            get { return _iSDirectoryValidRepo; }
            set
            {
                _iSDirectoryValidRepo = value;
                OnPropertyChanged(nameof(IsDirectoryValidRepo));
            }
        }

        public string PlaceholderText
        {
            get { return _placeholderText; }
            set
            {
                if (_placeholderText != value)
                {
                    _placeholderText = value;
                    OnPropertyChanged(nameof(PlaceholderText));
                }
            }
        }

        public IEnumerable<string> Repos => _repos;

        /// <summary>
        /// Represents the text in the combo box. Note that it does not have to be in <see cref="Repos"/>.
        /// </summary>
        /// <remarks>Note that logic in this propety allows trailing backslash to be equivalent.</remarks>
        public string SelectedDirectory
        {
            get { return _selectedDirectory; }
            set
            {
                // NOTE: This logic is very finicky. It is very difficult to allow IsEditable and support validation.
                if (_selectedDirectory != value && !_isUpdating)
                {
                    _selectedDirectory = value;

                    // From here, need to ignore any trailing backslash
                    string trimmedDirectory = value?.TrimEnd(Path.DirectorySeparatorChar);
                    if (ValidateSelectedDirectory(trimmedDirectory).GetAwaiter().GetResult())
                    {
                        if (!_repos.Contains(trimmedDirectory))
                        {
                            _repos.Add(trimmedDirectory);
                        }

                        SelectedRepo = trimmedDirectory;
                        IsDirectoryValidRepo = true;
                    }
                    else
                    {
                        // If directory is no longer valid, remove it from list but keep text.
                        if (_repos.Contains(trimmedDirectory))
                        {
                            _isUpdating = true;
                            _repos.Remove(trimmedDirectory);
                            SelectedDirectory = trimmedDirectory;
                            _isUpdating = false;
                        }
                        // HACK: Because IsEditable is true, if the user starts deleting the text of a SelectedItem,
                        // the ComboBox keeps that selection until they type something that is not in the Repos.
                        // I need to know when the text does not reflect an actual SelectedItem. Updating it here.
                        else
                        {
                            SelectedRepo = null;
                        }

                        IsDirectoryValidRepo = false;
                    }

                    OnPropertyChanged(nameof(SelectedDirectory));
                }
            }
        }

        /// <summary>
        /// Represents the selected item in the combo box. Note that it will be in <see cref="Repos"/>, otherwise null.
        /// </summary>
        public string SelectedRepo
        {
            get { return _selectedRepo; }
            set
            {
                if (_selectedRepo != value)
                {
                    _selectedRepo = value;
                    OnPropertyChanged(nameof(SelectedRepo));
                }
            }
        }

        public RepoBrowserViewModel(ConsoleControlViewModel consoleControlViewModel)
        {
            _repos = new ObservableCollection<string>();
            _repos.Add(@"C:\Dir1");
            _repos.Add(@"C:\Dir2");
            _repos.Add(@"C:\Dir3\Dir2\Dir1\git");

            _consoleControlViewModel = consoleControlViewModel;

            PlaceholderText = "Select a previous Git repo or browse to new one...";

            // Was not validating on initial load: https://stackoverflow.com/a/46972866
            //ValidateSelectedDirectory(SelectedDirectory).ConfigureAwait(false);
            //OpenGitRepoCommand.CanExecute(SelectedDirectory); TODO, verify this isn't necessary...
        }

        internal async Task<bool> ValidateSelectedDirectory(string folderPath)
        {
            const string propertyKey = nameof(SelectedDirectory);

            /* Call service asynchronously */
            ICollection<string> validationErrors = await _consoleControlViewModel.GitInterface.ValidateSelectedDirectoryAsync(folderPath);

            bool isValid = validationErrors.Count == 0;
            if (!isValid)
            {
                /* Update the collection in the dictionary returned by the GetErrors method */
                SetErrors(propertyKey, validationErrors);
            }
            else
            {
                ClearErrors(propertyKey);
            }

            return isValid;
        }
    }
}
