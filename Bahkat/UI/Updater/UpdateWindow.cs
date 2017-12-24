using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Service;
using Bahkat.UI.Main;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Updater
{
    public interface IUpdateWindowView : IWindowView
    {
        IObservable<EventArgs> OnInstallClicked();
        IObservable<EventArgs> OnRemindMeLaterClicked();
        IObservable<EventArgs> OnSkipClicked();
        void StartDownloading();
        void UpdatePrimaryButton(bool isEnabled, string label);
        void SetPackagesModel(ObservableCollection<PackageMenuItem> items);
        void HandleError(Exception error);
        void CloseMainWindow();
    }
    
    public class UpdateWindowPresenter
    {
        private readonly IUpdateWindowView _view;
        private readonly RepositoryService _repoServ;
        private readonly PackageService _pkgServ;
        private readonly PackageStore _store;
        private ObservableItemList<PackageMenuItem> _listItems =
            new ObservableItemList<PackageMenuItem>();
        
        public UpdateWindowPresenter(IUpdateWindowView view, RepositoryService repoServ, PackageService pkgServ, PackageStore store)
        {
            _view = view;
            _repoServ = repoServ;
            _pkgServ = pkgServ;
            _store = store;
        }
        
        private void RefreshPackageList(Repository repo)
        {
            _view.CloseMainWindow();
            
            _listItems.Clear();
            _store.Dispatch(PackageAction.ResetSelection);
           
            var items = repo.PackagesIndex.Values
                .Where(_pkgServ.RequiresUpdate)
                .Select(x => new PackageMenuItem(x, _pkgServ, _store));
                
            foreach (var item in items)
            {
                _store.Dispatch(PackageAction.AddSelectedPackage(item.Model));
                _listItems.Add(item);
            }
            
            Console.WriteLine("Added packages.");
        }
        
        private IDisposable BindPrimaryButton(IUpdateWindowView view, PackageStore store)
        {
            return store.State
                .Select(state => state.SelectedPackages)
                .Subscribe(packages =>
                {
                    if (packages.Count > 0)
                    {
                        view.UpdatePrimaryButton(true, string.Format(Strings.InstallNPackages, packages.Count));
                    }
                    else
                    {
                        view.UpdatePrimaryButton(false, Strings.NoPackagesSelected);
                    }
                });
        }

        private IDisposable BindRefreshPackageList()
        {
            return _repoServ.System
                .Select(x => x.RepoResult?.Repository)
                .NotNull()
                .DistinctUntilChanged()
                .Subscribe(RefreshPackageList, _view.HandleError);
        }

        private IDisposable BindPrimaryButtonPress()
        {
            return _view.OnInstallClicked().Subscribe(_ => _view.StartDownloading());
        }

        public IDisposable Start()
        {
            _view.SetPackagesModel(_listItems);

            return new CompositeDisposable(
                BindPrimaryButton(_view, _store),
                BindRefreshPackageList(),
                BindPrimaryButtonPress());
        }
    }
}