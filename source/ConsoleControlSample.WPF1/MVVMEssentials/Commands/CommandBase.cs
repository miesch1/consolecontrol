//https://github.com/SingletonSean/wpf-tutorials/blob/master/MVVMEssentials/Commands/CommandBase.cs
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace MVVMEssentials.Commands
{
    public abstract class CommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        public abstract void Execute(object parameter);

        protected virtual void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }
}
