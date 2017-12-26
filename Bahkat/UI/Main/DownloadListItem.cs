using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Threading;
using Bahkat.Service;

namespace Bahkat.UI.Main
{
    public class DownloadListItem : INotifyPropertyChanged, IEquatable<DownloadListItem>
    {
        public bool Equals(DownloadListItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Model.Package.Equals(other.Model.Package);
        }

        public override int GetHashCode()
        {
            return Model.GetHashCode();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public readonly PackageProgress Model;
        private long _downloaded;
        
        public DownloadListItem(PackageProgress package)
        {
            Model = package;
        }

        public string Title => Model.Package.NativeName;
        public string Version => Model.Package.Version;
        public long FileSize => Model.Package.Installer.Size;
        public long Downloaded
        {
            get => _downloaded;
            set
            {
                _downloaded = value;
                
                // Workaround for WPF bug where only one property change event can be
                // fired per setter being used... :|
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs("Status"));
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs("Downloaded"));
                });
            }
        }

        public string Status
        {
            get
            {
                if (_downloaded < 0)
                {
                    return Strings.DownloadError;
                }
                
                if (_downloaded == 0)
                {
                    return Strings.Downloading;
                }
                
                if (_downloaded < FileSize)
                {
                    return Util.Util.BytesToString(Downloaded);
                }

                return Strings.Downloaded;
            }
        }
    }
}