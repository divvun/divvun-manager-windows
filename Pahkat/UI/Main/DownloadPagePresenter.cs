using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Pahkat.Service;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Windows;
using Pahkat.Models;
using Pahkat.Sdk;

namespace Pahkat.UI.Main
{
    public class DownloadPagePresenter
    {
        public static DownloadPagePresenter Default(IDownloadPageView view)
        {
            var app = (PahkatApp) Application.Current;
            return new DownloadPagePresenter(view, app.UserSelection);
        }
        
        private ObservableCollection<DownloadListItem> _listItems =
            new ObservableCollection<DownloadListItem>();
        
        private readonly IDownloadPageView _view;
        private readonly IUserPackageSelectionStore _pkgStore;
        private readonly CancellationTokenSource _cancelSource;
        private Transaction _downloadedTransaction;
        private bool _waitingForCancelDialog;

        //private void UpdateProgress(PackageProgress package, uint cur, uint total)
        //{
        //    _listItems
        //        .First(x => Equals(package, x.Model))
        //        .Downloaded = cur;
        //}
        
        public DownloadPagePresenter(IDownloadPageView view, IUserPackageSelectionStore pkgStore)
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
                    _waitingForCancelDialog = false;
                    _downloadedTransaction = null;
                    _cancelSource.Cancel();
                    _view.DownloadCancelled();
                });

            var resume = _view.OnResumeClicked()
                .Subscribe(_ =>
                {
                    _waitingForCancelDialog = false;
                    if (_downloadedTransaction != null)
                    {
                        _view.StartInstallation(_downloadedTransaction);
                    }
                });

            var cancelDialogOpen = _view.OnCancelDialogOpen()
                .Subscribe(_ =>
                {
                    _waitingForCancelDialog = true;
                });

            var app = (PahkatApp) Application.Current;

            var transaction = _pkgStore.State
                .Select(x =>
                {
                    var actions = x.SelectedPackages.Select((p) =>
                    {
                        if (p.Value.Action == PackageAction.Install)
                        {
                            return TransactionAction.Install(p.Key, PackageTarget.System);
                        }
                        else
                        {
                            return TransactionAction.Uninstall(p.Key, PackageTarget.System);
                        }
                    });

                    return Transaction.New(app.PackageStore, actions.ToList());
                })
                .Take(1)
                .Replay(1)
                .RefCount();

            var downloadablePackages = transaction.SelectMany(tx =>
            {
                return tx.Actions()
                    .Where(x => x.Action == PackageAction.Install)
                    .Select((action =>
                    {
                        Package package = null;
                        foreach (var repo in app.PackageStore.RepoIndexes())
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
                    return app.PackageStore.Download(tuple.Item1.Id, tuple.Item1.Target)
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
                    if (!_waitingForCancelDialog)
                    {
                        _view.StartInstallation(t);
                    }
                    else
                    {
                        _downloadedTransaction = t;
                    }
                    //                    }
                }, _view.HandleError);

            return new CompositeDisposable(downloading, cancel);
        }
    }
}