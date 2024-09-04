using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPF = System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using ConsoleControlSample.WPF1.ViewModel;

namespace ConsoleControlSample.WPF1.Components
{
    /// <summary>
    /// Interaction logic for RepoBrowserComponent.xaml
    /// </summary>
    public partial class RepoBrowser : WPF.UserControl
    {
        public RepoBrowser()
        {
            InitializeComponent();
        }

        // A command is not used here as this is View-specific to launch a WinForms FolderBrowserDialog.
        //https://stackoverflow.com/a/54009102
        private void FolderBrowser_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as RepoBrowserViewModel;
            FolderBrowserDialog openFolderDialog = new FolderBrowserDialog();
            if (openFolderDialog.ShowDialog() == DialogResult.OK)
            {
                // Update the view with User's seleced path.
                viewModel.SelectedDirectory = openFolderDialog.SelectedPath;
            }
        }
    }
}
