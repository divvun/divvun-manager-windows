using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Pahkat.Service;

namespace Pahkat.UI.Main
{
    public class InstallPagePresenter
    {
        private readonly IInstallPageView _view;
        private readonly IInstallService _instServ;
        private readonly PackageProcessInfo _pkgInfo;
        private readonly IScheduler _scheduler;
        private readonly CancellationTokenSource _cancelSource;
        
        public InstallPagePresenter(IInstallPageView view, PackageProcessInfo pkgInfo, IInstallService instServ, IScheduler scheduler)
        {
            _view = view;
            _pkgInfo = pkgInfo;
            _instServ = instServ;
            _scheduler = scheduler;
            
            _cancelSource = new CancellationTokenSource();
        }

        public IDisposable Start()
        {
            var onStartPackageSubject = new Subject<OnStartPackageInfo>();
            _view.SetTotalPackages(_pkgInfo.ToInstall.LongLength + _pkgInfo.ToUninstall.LongLength);

            return new CompositeDisposable(
                // Handles forwarding progress status to the UI
                onStartPackageSubject
                    .ObserveOn(_scheduler)
                    .SubscribeOn(_scheduler)
                    .Subscribe(_view.SetCurrentPackage, _view.HandleError),
                // Processes the packages (install and uninstall)
                _instServ.Process(_pkgInfo, onStartPackageSubject, _cancelSource.Token)
                    .ToArray()
                    .SubscribeOn(_scheduler)
                    .ObserveOn(_scheduler)
                    .Subscribe(results =>
                    {
                        _view.ShowCompletion(_cancelSource.IsCancellationRequested, results);
                    }, _view.HandleError),
                // Cancel button binding
                _view.OnCancelClicked().Subscribe(_ =>
                {
                    _cancelSource.Cancel();
                    _view.ProcessCancelled();
                }),
                // Dispose the subject itself
                onStartPackageSubject
            );
        }
    }
}