using ConsoleControlAPI;
using ConsoleControlSample.WPF1.Utility;
using ConsoleControlSample.WPF1.View;
using ConsoleControlSample.WPF1.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ConsoleControlSample.WPF1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly GitInterface _gitInterface;

        public App()
        {
            ProcessInterface processInterface = new ProcessInterface();
            _gitInterface = new GitInterface(processInterface);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ConsoleControlViewModel consoleControlViewModel = new ConsoleControlViewModel(_gitInterface);

            RepoBrowserViewModel repoBrowserViewModel = new RepoBrowserViewModel(consoleControlViewModel);

            MainWindow = new MainWindow()
            {
                DataContext = new MainWindowViewModel(repoBrowserViewModel, consoleControlViewModel)
            };
            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}
