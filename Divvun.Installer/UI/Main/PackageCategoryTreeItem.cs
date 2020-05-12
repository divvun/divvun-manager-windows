using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Divvun.Installer.Models;

namespace Divvun.Installer.UI.Shared
{
    public class RepoTreeItem
    {
        public string Name { get; }
        public ObservableCollection<PackageCategoryTreeItem> Items { get; }

        public RepoTreeItem(String name, ObservableCollection<PackageCategoryTreeItem> items) {
            Name = name;
            Items = items;
        }
    }

    public class PackageCategoryTreeItem : IComparable<PackageCategoryTreeItem>, IEquatable<PackageCategoryTreeItem>,
        INotifyPropertyChanged
    {
        private readonly IUserPackageSelectionStore _store;
        private CompositeDisposable _bag = new CompositeDisposable();
        private bool _isGroupSelected;

        public string Name { get; }
        public ObservableCollection<PackageMenuItem> Items { get; }

        public bool IsGroupSelected {
            get => _isGroupSelected;
            set => _store.Dispatch(
                UserSelectionAction.ToggleGroupWithDefaultAction(Items.Select(x => x.Key).ToArray(), value));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public PackageCategoryTreeItem(IUserPackageSelectionStore store, string name,
            ObservableCollection<PackageMenuItem> items) {
            _store = store;
            Name = name;
            Items = items;

            _bag.Add(_store.State
                .Select(x => x.SelectedPackages)
                .Select(pkgs => Items.All(x => pkgs.ContainsKey(x.Key)))
                .DistinctUntilChanged()
                .Subscribe(x => {
                    _isGroupSelected = x;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsGroupSelected"));
                }));
        }

        public int CompareTo(PackageCategoryTreeItem other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return string.Compare(Name, other.Name, StringComparison.CurrentCulture);
        }

        public bool Equals(PackageCategoryTreeItem other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Equals(Items, other.Items);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackageCategoryTreeItem) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Items != null ? Items.GetHashCode() : 0);
            }
        }
    }
}