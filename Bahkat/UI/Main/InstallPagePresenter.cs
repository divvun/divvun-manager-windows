using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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
            return _instServ.Process(_packages, _view.SetCurrentPackage)
                .ObserveOn(_scheduler)
                .ToArray()
                .SubscribeOn(_scheduler)
                .ObserveOn(_scheduler)
                .Subscribe(_view.ShowCompletion, _view.HandleError);
        }
    }
}