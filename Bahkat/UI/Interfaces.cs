
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Bahkat.UI
{
    public class ObservableItemList<T> : ObservableCollection<T> where T : IEquatable<T>, INotifyPropertyChanged
    {
        public ObservableItemList()
        {
        }

        public ObservableItemList(List<T> list) : base(list)
        {
        }

        public ObservableItemList(IEnumerable<T> collection) : base(collection)
        {
        }

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