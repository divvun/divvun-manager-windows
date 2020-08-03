using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Divvun.Installer.Extensions;
using System.Windows;
using Divvun.Installer.Models;
using Divvun.Installer.Util;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;
using Pahkat.Sdk.Rpc.Models;

namespace Divvun.Installer.UI.Shared
{
    public class PackageMenuItem : INotifyPropertyChanged, IDisposable, IEquatable<PackageMenuItem>,
        IComparable<PackageMenuItem>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public PackageKey Key { get; private set; }
        public IWindowsExecutable Payload { get; private set; }
        public string Name { get; private set; }
        public string Version { get; private set; }

        private UserPackageSelectionStore _store;

        private CompositeDisposable _bag = new CompositeDisposable();

        private PackageStatus _status {
            get {
                using var x = ((PahkatApp) Application.Current).PackageStore.Lock();
                return x.Value.Status(Key);
            }
        }

        private PackageAction? _actionInfo;

        public PackageMenuItem(UserPackageSelectionStore store, PackageKey key, IWindowsExecutable payload, string name, string version) {
            _store = store;
            
            Key = key;
            Name = name;
            Version = version;
            Payload = payload;
            
            _store.State
                .Subscribe(state => {
                    if (state.SelectedPackages.TryGetValue(key, out var value)) {
                        _actionInfo = value;
                    } else {
                        _actionInfo = null;
                    }
                    
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
                }).DisposedBy(_bag);
        }

        public string Title => Name;

        public string Status {
            get {
                switch (_actionInfo?.Action) {
                    case InstallAction.Install:
                        return Strings.Install;
                    case InstallAction.Uninstall:
                        return Strings.Uninstall;
                    default:
                        return _status.Description() ?? _status.ToString();
                }
            }
        }

        public string FileSize => $"({Util.Util.BytesToString(Payload.Size)})";

        public bool IsSelected {
            get => _actionInfo != null;
            set => _store.Dispatch(UserSelectionAction.TogglePackageWithDefaultAction(Key, value));
        }

        public void Dispose() {
            _bag.Dispose();
        }

        public int CompareTo(PackageMenuItem other) {
            return string.Compare(Name, other.Name, StringComparison.CurrentCulture);
        }

        public bool Equals(PackageMenuItem? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _store.Equals(other._store) && _bag.Equals(other._bag) &&
                   Key.Equals(other.Key) && Payload.Equals(other.Payload) && Name == other.Name &&
                   Version == other.Version;
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackageMenuItem) obj);
        }
    }
}