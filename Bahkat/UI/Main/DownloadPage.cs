using System;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using Bahkat.Service;

namespace Bahkat.UI.Main
{
    public interface IDownloadPageView : IPageView
    {
        void StartInstallation(PackagePath[] packages);
    }
    
    public class DownloadPage : Page, IDownloadPageView
    {
        public void StartInstallation(PackagePath[] packages)
        {
            throw new NotImplementedException();
        }
    }

    public class DownloadPagePresenter
    {
        private readonly IDownloadPageView _view;
        private readonly PackageProgress[] _packages;
        private readonly IPackageService _pkgServ;
        
        public DownloadPagePresenter(IDownloadPageView view, PackageProgress[] packages, IPackageService pkgServ)
        {
            _view = view;
            _packages = packages;
            _pkgServ = pkgServ;
        }

        public IDisposable Start()
        {
            // TODO implement a system and state machine for this dance of mess
            return _pkgServ.Download(_packages, 3)
                .ToArray()
                .Subscribe(_view.StartInstallation);
        }
    }
}