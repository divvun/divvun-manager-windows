using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using Bahkat.Models;
using Bahkat.Service;

namespace Bahkat.UI.Shared
{
    public class PackageMenuItem : INotifyPropertyChanged, IDisposable, IEquatable<PackageMenuItem>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Package Model { get; private set; }
        private PackageService _pkgServ;
        private PackageStore _store;

        private CompositeDisposable _bag = new CompositeDisposable();
        
        // TODO: add a subscriber to the registry to stop this from firing so often
        private PackageInstallStatus _status => _pkgServ.GetInstallStatus(Model);
        private bool _isSelected = false;
        private ObservableCollection<PackageMenuItem> _itemSource;

        private PackageMenuItem[] _selectedGroupItems
        {
            get
            {
                var cvs = (CollectionView) CollectionViewSource.GetDefaultView(_itemSource);
                var vg = cvs.Groups.Select(x => (CollectionViewGroup) x).First(x => x.Items.Contains(this));
                return vg.Items.Select(x => (PackageMenuItem)x).ToArray();
            }
        }

        public PackageMenuItem(ObservableCollection<PackageMenuItem> itemSource, Package model, PackageService pkgServ, PackageStore store)
        {
            _itemSource = itemSource;
            Model = model;
            _pkgServ = pkgServ;
            _store = store;

            _bag.Add(_store.State.Select(x => x.SelectedPackages.Contains(model))
                .DistinctUntilChanged()
                .Subscribe(x =>
                {
                    _isSelected = x;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
                }));
        }

        public string Title => Model.NativeName;
        public string Version => Model.Version;
        public string Status => _status.Description();

        public string FileSize
        {
            get
            {
                if (Model.Installer != null)
                {
                    return "(" + Bahkat.Shared.BytesToString(Model.Installer.InstalledSize) + ")";
                }
                return Strings.NotApplicable;
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => _store.Dispatch(PackageAction.TogglePackage(Model, value));
        }

        public bool IsGroupSelected
        {
            get =>  _selectedGroupItems.All(x => x.IsSelected);
            set => _store.Dispatch(PackageAction.ToggleGroup(_selectedGroupItems.Select(x => x.Model).ToArray(), value));
        }

        public void Dispose()
        {
            _bag.Dispose();
        }
        
        public bool Equals(PackageMenuItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Model, other.Model);
        }
        
        public override int GetHashCode()
        {
            return (Model != null ? Model.GetHashCode() : 0);
        }
    }
}