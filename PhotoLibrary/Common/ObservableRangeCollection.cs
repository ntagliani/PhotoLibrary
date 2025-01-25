using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PhotoLibrary.Common;

public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public void SetRange(IEnumerable<T> source)
    {
        // working on the internal Items prevents the events to be spammed around when using .Add
        Items.Clear();
        foreach (var s in source)
            Items.Add(s);     
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}


