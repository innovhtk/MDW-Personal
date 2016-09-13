using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HTK__GPI_Listener
{
    public class Blackboard : INotifyPropertyChanged, INotifyPropertyChanging
    {
        Dictionary<string, object> _dict = new Dictionary<string, object>();

        public T Get<T>(BlackboardProperty<T> property)
        {
            if (!_dict.ContainsKey(property.Name))
                _dict[property.Name] = property.GetDefault();
            return (T)_dict[property.Name];
        }

        public void Set<T>(BlackboardProperty<T> property, T value)
        {
            OnPropertyChanging(property.Name);
            _dict[property.Name] = value;
            OnPropertyChanged(property.Name);
        }

        #region property change notification

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
