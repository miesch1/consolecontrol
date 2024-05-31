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
        protected override void OnStartup(StartupEventArgs e)
        {
            ConsoleControlViewModel consoleControlViewModel = new ConsoleControlViewModel();

            MainWindow = new MainWindow()
            {
                DataContext = new MainWindowViewModel(consoleControlViewModel)
            };
            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}
