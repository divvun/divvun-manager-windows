using System;
using System.ComponentModel;
using System.Windows.Threading;
using Pahkat.Sdk;
using Serilog;

namespace Divvun.Installer.UI.Main
{
    public class DownloadListItem : INotifyPropertyChanged, IEquatable<DownloadListItem>
    {
        public bool Equals(DownloadListItem other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Key, other.Key);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DownloadListItem) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (Key.GetHashCode() * 397);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public readonly PackageKey Key;
        // public readonly string Name;
        // public readonly string Version;
        private long _downloaded;

        public DownloadListItem(PackageKey key, string name, string version) {
            Key = key;
            Title = name;
            Version = version;
        }

        public string Title { get; } = "-";
        public string Version { get; } = "-";

        private long _fileSize = -1;
        public long FileSize {
            get => _fileSize;
            set {
                _fileSize = value;
                PropertyChanged?.Invoke(this,
                    new PropertyChangedEventArgs("FileSize"));
            }
        }

        public long Downloaded {
            get => _downloaded;
            set {
                _downloaded = value;

                // Workaround for WPF bug where only one property change event can be
                // fired per setter being used... :|
                Dispatcher.CurrentDispatcher.Invoke(() => {
                    Log.Verbose("Dispatching Status and Downloaded events");
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs("Status"));
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs("Downloaded"));
                });
            }
        }

        public string Status {
            get {
                if (_downloaded < 0) {
                    return Strings.DownloadError;
                }

                if (_downloaded == 0) {
                    return Strings.Downloading;
                }

                if (_downloaded < FileSize) {
                    return Util.Util.BytesToString(Downloaded);
                }

                return Strings.Downloaded;
            }
        }
    }
}