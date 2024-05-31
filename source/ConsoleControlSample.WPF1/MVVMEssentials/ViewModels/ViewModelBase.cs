//https://github.com/SingletonSean/wpf-tutorials/blob/master/MVVMEssentials/ViewModels/ViewModelBase.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MVVMEssentials.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void Dispose() { }
    }
}
