using ConsoleControlSample.WPF1.Utility;
using ConsoleControlSample.WPF1.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ConsoleControlSample.WPF1.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConsoleControl_Loaded(object sender, RoutedEventArgs e)
        {
            // HACK: I didn't develop this ConsoleControl, so without an overhaul I need easy access to its ProcessInterface for my ViewModel.
            var viewModel = this.DataContext as MainWindowViewModel;
            if (viewModel != null)
            {
                //viewModel.ConsoleControlViewModel.SetGitInterface(new GitInterface(consoleControl.ProcessInterface));
                //viewModel.ConsoleControlViewModel.GetGitInterface().StartProcess("C:\\Program Files\\Git\\git-cmd.exe", string.Empty);

                viewModel.ConsoleControlViewModel.GitInterface.StartProcessAsync("cmd.exe", string.Empty).GetAwaiter().GetResult();
            }
        }
    }
}
