using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DungeonTest.ViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected Boolean ValidateProperty { get; private set; }

        protected ViewModelBase(Boolean validateProperty = false)
        {
            ValidateProperty = validateProperty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (ValidateProperty && TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                throw new Exception("Invalid property name: " + propertyName);
            }

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
