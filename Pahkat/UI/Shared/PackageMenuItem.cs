using System;
using System.ComponentModel;
using System.Data.Odbc;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.Service;
using Pahkat.Sdk;
using System.Windows;

namespace Pahkat.UI.Shared
{
    public class PackageMenuItem : INotifyPropertyChanged, IDisposable, IEquatable<PackageMenuItem>,
        IComparable<PackageMenuItem>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public PackageKey Key { get; private set; }
        public Package Model { get; private set; }
        private UserPackageSelectionStore _store;

        private CompositeDisposable _bag = new CompositeDisposable();

        // TODO: add a subscriber to the registry to stop this from firing so often
        private PackageStatus _status => ((PahkatApp) Application.Current).PackageStore.Status(Key).Item1;
        private PackageActionInfo _actionInfo;

        public PackageMenuItem(PackageKey key, Package model, UserPackageSelectionStore store) {
            Key = key;
            Model = model;
            _store = store;

            _bag.Add(_store.State
                .Select(x => x.SelectedPackages.Get(Key, null))
                .DistinctUntilChanged()
                .Subscribe(x => {
                    _actionInfo = x;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
                }));
        }

        public string Title => Model.NativeName;
        public string Version => Model.Version;

        public string Status {
            get {
                switch (_actionInfo?.Action) {
                    case PackageAction.Install:
                        return Strings.Install;
                    case PackageAction.Uninstall:
                        return Strings.Uninstall;
                    default:
                        return _status.Description() ?? _status.ToString();
                }
            }
        }

        public string FileSize {
            get {
                if (Model.WindowsInstaller != null) {
                    return "(" + Util.Util.BytesToString(Model.WindowsInstaller.Size) + ")";
                }

                return Strings.NotApplicable;
            }
        }

        public bool IsSelected {
            get => _actionInfo != null;
            set => _store.Dispatch(UserSelectionAction.TogglePackageWithDefaultAction(Key, value));
        }

        public void Dispose() {
            _bag.Dispose();
        }

        public bool Equals(PackageMenuItem other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Key, other.Key) && Equals(Model, other.Model);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackageMenuItem) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((Key != null ? Key.GetHashCode() : 0) * 397) ^ (Model != null ? Model.GetHashCode() : 0);
            }
        }

        public int CompareTo(PackageMenuItem other) {
            return string.Compare(Model.NativeName, other.Model.NativeName, StringComparison.CurrentCulture);
        }
    }
}