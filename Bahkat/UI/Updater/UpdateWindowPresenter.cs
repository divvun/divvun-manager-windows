using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Service;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Updater
{
    public class UpdateWindowPresenter
    {
        private readonly IUpdateWindowView _view;
        private readonly RepositoryService _repoServ;
        private readonly PackageService _pkgServ;
        private readonly PackageStore _store;
        private ObservableCollection<PackageMenuItem> _listItems =
            new ObservableCollection<PackageMenuItem>();
        
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
                .Select(x => new PackageMenuItem(x, _pkgServ, _store))
                .ToArray();
                
            foreach (var item in items)
            {
                _store.Dispatch(PackageAction.AddSelectedPackage(item.Model));
                _listItems.Add(item);
            }
            
            _view.UpdateTitle($"{Strings.AppName} - {repo.Meta.NativeName} - {string.Format(Strings.NUpdatesAvailable, items.Count())}");
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

        private IDisposable BindLaterButtonPress()
        {
            return _view.OnRemindMeLaterClicked().Subscribe(x => _view.Close());
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

        private IDisposable BindPackageToggled()
        {
            return _view.OnPackageToggled()
                .Select(item => PackageAction.TogglePackage(item.Model, !item.IsSelected))
                .Subscribe(_store.Dispatch);
        }
        
        private IDisposable BindSkipButtonPress()
        {
            return _view.OnSkipClicked()
                .Select(_ => _store.State.Select(x => x.SelectedPackages))
                .Switch()
                .Take(1)
                .Subscribe(pkgs =>
                {
                    foreach (var pkg in pkgs)
                    {
                        _pkgServ.SkipVersion(pkg);
                    }
                    
                    _store.Dispatch(PackageAction.ToggleGroup(pkgs.ToArray(), false));
                    _view.RefreshList();
                });
        }

        public IDisposable Start()
        {
            _view.UpdateTitle($"{Strings.AppName} - {Strings.Loading}");
            _view.SetPackagesModel(_listItems);

            return new CompositeDisposable(
                BindPrimaryButton(_view, _store),
                BindRefreshPackageList(),
                BindSkipButtonPress(),
                BindPrimaryButtonPress(),
                BindPackageToggled(),
                BindLaterButtonPress());
        }  
    }
}