using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PhotoLibrary;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected void Set<T>(ref T field, T value, [CallerMemberName] string fieldName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(fieldName));
        }
    }
}
