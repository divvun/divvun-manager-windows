using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Pahkat.Service;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Windows;
using Pahkat.Models;
using Pahkat.Extensions;
using Pahkat.Service.CoreLib;
using PackageActionType = Pahkat.Models.PackageActionType;

namespace Pahkat.UI.Main
{
    public class DownloadPagePresenter
    {
        public static DownloadPagePresenter Default(IDownloadPageView view)
        {
            var app = (IPahkatApp) Application.Current;
            return new DownloadPagePresenter(view, app.PackageStore);
        }
        
        private ObservableCollection<DownloadListItem> _listItems =
            new ObservableCollection<DownloadListItem>();
        
        private readonly IDownloadPageView _view;
        private readonly IPackageStore _pkgStore;
        private readonly CancellationTokenSource _cancelSource;

        private void UpdateProgress(PackageProgress package, uint cur, uint total)
        {
            _listItems
                .First(x => Equals(package, x.Model))
                .Downloaded = cur;
        }
        
        public DownloadPagePresenter(IDownloadPageView view, IPackageStore pkgStore)
        {
            _view = view;
            _pkgStore = pkgStore;
            
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

            var app = (IPahkatApp) Application.Current;

            var transaction = _pkgStore.State
                .Select(x =>
                {
                    var actions = x.SelectedPackages.Select((p) =>
                    {
                        return new TransactionAction(p.Value.Action, p.Key, InstallerTarget.System);
                    });
                    return app.Client.Transaction(actions.ToArray());
                })
                .Take(1)
                .Replay(1)
                .RefCount();

            var downloadablePackages = transaction.SelectMany(tx =>
            {
                return tx.Actions
                    .Where(x => x.Action == PackageActionType.Install)
                    .Select((action =>
                    {
                        Package package = null;
                        foreach (var repo in app.Client.Repos())
                        {
                            package = repo.Package(action.Id);
                            if (package != null)
                            {
                                break;
                            }
                        }

                        var item = new DownloadListItem(action.Id, package);
                        _listItems.Add(item);

                        return new Tuple<TransactionAction, DownloadListItem>(action, item);
                    }))
                    .ToArray();
            });

            var downloading = downloadablePackages.Select((tuple) =>
                {
                    return app.Client.Download(tuple.Item1.Id, tuple.Item1.Target)
//                        .SubscribeOn(DispatcherScheduler.Current)
                        .ObserveOn(DispatcherScheduler.Current)
                        .Do((status) =>
                        {
                            _view.SetStatus(tuple.Item2, status);
                        });
                })
                .Merge(3)
                .Where((p) => p.Status == PackageDownloadStatus.Error)
                .ToArray()
                .Select((_) => transaction)
                .Switch()
                .SubscribeOn(DispatcherScheduler.Current)
//                .ObserveOn(DispatcherScheduler.Current)
                .Subscribe((t) =>
                {
//                    if (t.Item2)
//                    {
//                        // TODO: far better error handling for failed downloads
//                        _view.HandleError(new Exception(Strings.DownloadError));
//                    }
//                    else
//                    {
                        _view.StartInstallation(t);
//                    }
                }, _view.HandleError);

            return new CompositeDisposable(downloading, cancel);
        }
    }
}