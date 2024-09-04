using ConsoleControlSample.WPF1.ViewModel;
using MVVMEssentials.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleControlSample.WPF1.Commands
{
    public class StartCommandPromptCommand : CommandBase
    {
        private ConsoleControlViewModel _consoleControlViewModel;

        public StartCommandPromptCommand(ConsoleControlViewModel consoleControlViewModel)
        {
            _consoleControlViewModel = consoleControlViewModel;
        }

        public override void Execute(object parameter)
        {
            _consoleControlViewModel.GitInterface.StartProcessAsync(parameter as string, string.Empty);
        }
    }
}
