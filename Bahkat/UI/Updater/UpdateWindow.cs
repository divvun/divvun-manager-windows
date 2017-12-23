using System;
using Bahkat.Models;
using Bahkat.Service;
using Bahkat.UI.Main;

namespace Bahkat.UI.Updater
{
    public interface IUpdateWindowView : IWindowView
    {
        IObservable<EventArgs> OnInstallClicked();
        IObservable<EventArgs> OnRemindMeLaterClicked();
        void BeginDownloading();
    }
    
    public class UpdateWindowPresenter
    {
        private readonly IUpdateWindowView _view;
        private readonly RepositoryService _repoServ;
        private readonly PackageService _pkgServ;
        private readonly PackageStore _store;
        
        public UpdateWindowPresenter(IUpdateWindowView view, RepositoryService repoServ, PackageService pkgServ, PackageStore store)
        {
            _view = view;
            _repoServ = repoServ;
            _pkgServ = pkgServ;
            _store = store;
        }

        public void Start()
        {
            
        }
    }
}