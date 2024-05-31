//https://github.com/SingletonSean/wpf-tutorials/blob/master/MVVMEssentials/ViewModels/ErrorsViewModel.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Documents;

namespace MVVMEssentials.ViewModels
{
    public class ErrorsViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _propertyErrors = new Dictionary<string, List<string>>();

        public bool HasErrors => _propertyErrors.Any();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return null;
            }

            return _propertyErrors.GetValueOrDefault(propertyName, null);
        }

        public void AddError(string propertyName, string errorMessage)
        {
            if (!_propertyErrors.ContainsKey(propertyName))
            {
                _propertyErrors.Add(propertyName, new List<string>());
            }

            _propertyErrors[propertyName].Add(errorMessage);
            OnErrorsChanged(propertyName);
        }

        public void ClearErrors(string propertyName)
        {
            if (_propertyErrors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
            }
        }

        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public void SetErrors(string propertyName, IEnumerable propertyErrors)
        {
            if (_propertyErrors.ContainsKey(propertyName))
            {
                _propertyErrors.Remove(propertyName);
            }

            List<string> list = new List<string>();
            if (propertyErrors != null)
            {
                foreach (var item in propertyErrors)
                {
                    list.Add(item.ToString());
                }
            }

            _propertyErrors.Add(propertyName, list);

            /* Raise event to tell WPF to execute the GetErrors method */
            OnErrorsChanged(propertyName);
        }
    }
}
