using System;
using System.ComponentModel;
using System.Data.Odbc;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Service;

namespace Bahkat.UI.Shared
{
    public class PackageMenuItem : INotifyPropertyChanged, IDisposable, IEquatable<PackageMenuItem>, IComparable<PackageMenuItem>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Package Model { get; private set; }
        private IPackageService _pkgServ;
        private PackageStore _store;

        private CompositeDisposable _bag = new CompositeDisposable();
        
        // TODO: add a subscriber to the registry to stop this from firing so often
        private PackageInstallStatus _status => _pkgServ.InstallStatus(Model);
        private PackageActionInfo _actionInfo;

        public PackageMenuItem(Package model, IPackageService pkgServ, PackageStore store)
        {
            Model = model;
            _pkgServ = pkgServ;
            _store = store;

            _bag.Add(_store.State
                .Select(x => x.SelectedPackages.Get(Model, null))
                .DistinctUntilChanged()
                .Subscribe(x =>
                {
                    _actionInfo = x;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
                }));
        }

        public string Title => Model.NativeName;
        public string Version => Model.Version;
        public string Status
        {
            get
            {
                switch (_actionInfo?.Action)
                {
                    case PackageAction.Install:
                        return Strings.Install;
                    case PackageAction.Uninstall:
                        return Strings.Uninstall;
                    default:
                        return _status.Description();
                }
            }
        }

        public string FileSize
        {
            get
            {
                if (Model.Installer != null)
                {
                    return "(" + Util.Util.BytesToString(Model.Installer.Size) + ")";
                }
                return Strings.NotApplicable;
            }
        }

        public bool IsSelected
        {
            get => _actionInfo != null;
            set => _store.Dispatch(PackageStoreAction.TogglePackageWithDefaultAction(Model, value));
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