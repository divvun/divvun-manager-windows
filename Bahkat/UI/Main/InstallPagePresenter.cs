using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Bahkat.Service;

namespace Bahkat.UI.Main
{
    public class InstallPagePresenter
    {
        private readonly IInstallPageView _view;
        private readonly IInstallService _instServ;
        private readonly PackageProcessInfo _pkgInfo;
        private readonly IScheduler _scheduler;
        
        public InstallPagePresenter(IInstallPageView view, PackageProcessInfo pkgInfo, IInstallService instServ, IScheduler scheduler)
        {
            _view = view;
            _pkgInfo = pkgInfo;
            _instServ = instServ;
            _scheduler = scheduler;
        }
        
        public IDisposable Start()
        {
            var onStartPackageSubject = new Subject<OnStartPackageInfo>();
            _view.SetTotalPackages(_pkgInfo.ToInstall.LongLength + _pkgInfo.ToUninstall.LongLength);

            return new CompositeDisposable(
                _instServ.Process(_pkgInfo, onStartPackageSubject)
                    .ToArray()
                    .SubscribeOn(_scheduler)
                    .ObserveOn(_scheduler)
                    .Subscribe(_view.ShowCompletion, _view.HandleError),
                onStartPackageSubject
                    .ObserveOn(_scheduler)
                    .SubscribeOn(_scheduler)
                    .Subscribe(_view.SetCurrentPackage, _view.HandleError),
                onStartPackageSubject
            );
        }
    }
}