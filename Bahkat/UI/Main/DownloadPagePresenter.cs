using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Bahkat.Service;
using System.Collections.ObjectModel;
using Bahkat.Models;

namespace Bahkat.UI.Main
{
    public class DownloadPagePresenter
    {
        private ObservableCollection<DownloadListItem> _listItems =
            new ObservableCollection<DownloadListItem>();
        
        private readonly IDownloadPageView _view;
        private readonly PackageStore _pkgStore;
        private readonly IPackageService _pkgServ;
        private readonly CancellationTokenSource _cancelSource;

        private void UpdateProgress(object sender, DownloadProgressChangedEventArgs args)
        {
            var package = (PackageProgress) sender;

            _listItems
                .First(x => Equals(package, x.Model))
                .Downloaded = args.BytesReceived;
        }

        private PackageProgress CreatePackageProgress(Package package)
        {
            var prog = new PackageProgress()
            {
                Package = package
            };
            prog.Progress = (sender, e) => UpdateProgress(prog, e);
            return prog;
        }
        
        public DownloadPagePresenter(IDownloadPageView view, PackageStore pkgStore, IPackageService pkgServ)
        {
            _view = view;
            _pkgStore = pkgStore;
            _pkgServ = pkgServ;
            
            _cancelSource = new CancellationTokenSource();
        }

        public IDisposable Start()
        {
            _view.InitProgressList(_listItems);

            var cancel = _view.OnCancelClicked()
                .Subscribe(_ =>
                {
                    _cancelSource.Cancel();
                    _view.DownloadCancelled();
                });

            var justSelected = _pkgStore.State
                .Select(x => x.SelectedPackages)
                .Take(1)
                .Replay(1)
                .RefCount();

            var downloader = justSelected.Select(selected =>
                {
                    var packages = selected.Values
                        .Where(x => x.Action == PackageAction.Install)
                        .Select(x => x.Package)
                        .Select(CreatePackageProgress)
                        .ToArray();

                    foreach (var item in packages)
                    {
                        _listItems.Add(new DownloadListItem(item));
                    }

                    return packages;
                })
                .DefaultIfEmpty(Array.Empty<PackageProgress>())
                .Select(packages => _pkgServ.Download(packages, 3, _cancelSource.Token))
                .Switch()
                .ToArray()
                .DefaultIfEmpty(Array.Empty<PackageInstallInfo>());

            var uninstaller = justSelected.Select(selected => selected.Values
                    .Where(x => x.Action == PackageAction.Uninstall))
                .Select(packages => packages
                    .Select(x => _pkgServ.UninstallInfo(x.Package))
                    .Where(x => x != null)
                    .ToArray())
                .DefaultIfEmpty(Array.Empty<PackageUninstallInfo>());

            var everything = Observable.Zip(
                downloader,
                uninstaller,
                (downloaded, uninstalls) => new PackageProcessInfo
                {
                    ToInstall = downloaded,
                    ToUninstall = uninstalls
                })
                .Do(x =>
                {
                    Console.WriteLine($"Installs: {x.ToInstall.Length}, Uninsts: {x.ToUninstall.Length}");
                })
                .SingleAsync()
                .Subscribe(_view.StartInstallation, _view.HandleError);

            return new CompositeDisposable(everything, cancel);
        }
    }
}