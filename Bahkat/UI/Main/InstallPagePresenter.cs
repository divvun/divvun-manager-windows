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
        private readonly PackagePath[] _packages;
        private readonly IScheduler _scheduler;
        
        public InstallPagePresenter(IInstallPageView view, PackagePath[] packages, IInstallService instServ, IScheduler scheduler)
        {
            _view = view;
            _packages = packages;
            _instServ = instServ;
            _scheduler = scheduler;
        }
        
        public IDisposable Start()
        {
            var _onStartPackageSubject = new Subject<OnStartPackageInfo>();
            _view.SetTotalPackages(_packages.LongLength);

            return new CompositeDisposable(
                _instServ.Process(_packages, _onStartPackageSubject)
                    .ToArray()
                    .SubscribeOn(_scheduler)
                    .ObserveOn(_scheduler)
                    .Subscribe(_view.ShowCompletion, _view.HandleError),
                _onStartPackageSubject
                    .ObserveOn(_scheduler)
                    .SubscribeOn(_scheduler)
                    .Subscribe(_view.SetCurrentPackage),
                _onStartPackageSubject
            );
        }
    }
}