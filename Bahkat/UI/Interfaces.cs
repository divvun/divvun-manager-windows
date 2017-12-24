
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Bahkat.UI
{
    public class ObservableItemList<T> : ObservableCollection<T> where T : IEquatable<T>, INotifyPropertyChanged
    {
        private void DelegatePropertyChange(object sender, PropertyChangedEventArgs args)
        {
            var item = (T) sender;
            var index = IndexOf(item);
            MoveItem(index, index);
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            item.PropertyChanged += DelegatePropertyChange;
        }

        protected override void RemoveItem(int index)
        {
            this[index].PropertyChanged -= DelegatePropertyChange;
            base.RemoveItem(index);
        }
    }

    public interface IPageView
    {
        // void ShowPage(IPageView page);
    }
}