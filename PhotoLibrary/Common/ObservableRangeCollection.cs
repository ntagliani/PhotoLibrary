using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoLibrary.Common;

public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public void SetRange(IEnumerable<T> source)
    {
        Items.Clear();
        foreach (var s in source)
            Items.Add(s);     
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}


