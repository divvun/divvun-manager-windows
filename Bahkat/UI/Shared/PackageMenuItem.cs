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
    public class PackageMenuItem : INotifyPropertyChanged, IDisposable, IEquatable<PackageMenuItem>, IComparable<PackageMenuItem>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Package Model { get; private set; }
        private PackageService _pkgServ;
        private PackageStore _store;

        private CompositeDisposable _bag = new CompositeDisposable();
        
        // TODO: add a subscriber to the registry to stop this from firing so often
        private PackageInstallStatus _status => _pkgServ.GetInstallStatus(Model);
        private bool _isSelected = false;

        public PackageMenuItem(Package model, PackageService pkgServ, PackageStore store)
        {
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
        
        public int CompareTo(PackageMenuItem other)
        {
            return String.Compare(Model.NativeName, other.Model.NativeName, StringComparison.CurrentCulture);
        }
    }
}