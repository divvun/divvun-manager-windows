using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Divvun.Installer.Extensions;
using Divvun.Installer.Models;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Models;

namespace Divvun.Installer.UI.Shared {

public class PackageMenuItem : INotifyPropertyChanged, IDisposable, IEquatable<PackageMenuItem>,
    IComparable<PackageMenuItem> {
    private PackageAction? _actionInfo;

    private readonly CompositeDisposable _bag = new CompositeDisposable();

    private PackageStatus _status = PackageStatus.Unknown;

    private readonly UserPackageSelectionStore _store;

    public PackageMenuItem(UserPackageSelectionStore store, PackageKey key, IWindowsExecutable payload, string name,
        string version) {
        _store = store;

        Key = key;
        Name = name;
        Version = version;
        Payload = payload;

        _store.Observe()
            .SubscribeOn(PahkatApp.Current.Dispatcher)
            .Subscribe(state => {
                PahkatApp.Current.Dispatcher.InvokeAsync(async () => {
                    var lastActionInfo = _actionInfo;

                    if (state.SelectedPackages.TryGetValue(key, out var value)) {
                        _actionInfo = value;
                    }
                    else {
                        _actionInfo = null;
                    }

                    // Log.Verbose("Updating item state");
                    _status = await PahkatApp.Current.PackageStore.Status(Key);
                    if (PropertyChanged != null) {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Status"));
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
                    }
                });
            }).DisposedBy(_bag);
    }

    public PackageKey Key { get; }
    public IWindowsExecutable Payload { get; }
    public string Name { get; }
    public string Version { get; }

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
        set {
            PahkatApp.Current.Dispatcher.InvokeAsync(async () => {
                // Log.Verbose("CLICKEROO");
                await _store.TogglePackageWithDefaultAction(Key, value);
                _status = await PahkatApp.Current.PackageStore.Status(Key);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
            });
        }
    }

    public int CompareTo(PackageMenuItem other) {
        return string.Compare(Name, other.Name, StringComparison.CurrentCulture);
    }

    public void Dispose() {
        _bag.Dispose();
    }

    public bool Equals(PackageMenuItem? other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return _store.Equals(other._store) && _bag.Equals(other._bag) &&
            Key.Equals(other.Key) && Payload.Equals(other.Payload) && Name == other.Name &&
            Version == other.Version;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) {
            return false;
        }

        if (ReferenceEquals(this, obj)) {
            return true;
        }

        if (obj.GetType() != GetType()) {
            return false;
        }

        return Equals((PackageMenuItem)obj);
    }
}

}