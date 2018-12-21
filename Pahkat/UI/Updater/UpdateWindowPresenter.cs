using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.Service;
using Pahkat.UI.Shared;

namespace Pahkat.UI.Updater
{
    public class UpdateWindowPresenter
    {
        private readonly IUpdateWindowView _view;
        //private readonly RepositoryService _repoServ;
        private readonly IPackageService _pkgServ;
        private readonly IPackageStore _store;
        private ObservableCollection<PackageMenuItem> _listItems =
            new ObservableCollection<PackageMenuItem>();
        
        public UpdateWindowPresenter(IUpdateWindowView view, IPackageService pkgServ, IPackageStore store)
        {
            _view = view;
            //_repoServ = repoServ;
            _pkgServ = pkgServ;
            _store = store;
        }
        
        private void RefreshPackageList(RepositoryIndex[] repos)
        {
            _listItems.Clear();
            _store.Dispatch(PackageStoreAction.ResetSelection);

            foreach (var repo in repos)
            {
                var it = repo.Packages.Values
                    .Select(repo.AbsoluteKeyFor)
                    .Where(_pkgServ.RequiresUpdate)
                    .Select(x => new PackageMenuItem(x, repo.Package(x), _pkgServ, _store))
                    .ToArray();
                foreach (var item in it)
                {
                    _store.Dispatch(PackageStoreAction.AddSelectedPackage(item.Key, PackageActionType.Install));
                    _listItems.Add(item);
                }
            }
            
            _view.UpdateTitle($"{Strings.AppName} - {string.Format(Strings.NUpdatesAvailable, _listItems.Count)}");
            _view.CloseMainWindow();

            Console.WriteLine("Added packages.");
        }
        
        private IDisposable BindPrimaryButton(IUpdateWindowView view, IPackageStore store)
        {
            return store.State
                .Select(state => state.SelectedPackages)
                .Subscribe(packages =>
                {
                    if (packages.Count > 0)
                    {
                        string s;

                        if (packages.All(x => x.Value.Action == PackageActionType.Install))
                        {
                            s = string.Format(Strings.InstallNPackages, packages.Count);
                        }
                        else if (packages.All(x => x.Value.Action == PackageActionType.Uninstall))
                        {
                            s = string.Format(Strings.UninstallNPackages, packages.Count);
                        }
                        else
                        {
                            s = string.Format(Strings.InstallUninstallNPackages, packages.Count);
                        }

                        view.UpdatePrimaryButton(true, s);
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
            var app = (IPahkatApp) Application.Current;
            
            return Observable.Return(app.Client.Repos())
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
                .Select(item => PackageStoreAction.TogglePackage(item.Key, PackageActionType.Install, !item.IsSelected))
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
                    var packages = pkgs.Keys.Select(x => new PackageActionInfo(x, PackageActionType.Uninstall)).ToArray();
                    
                    foreach (var pkg in packages.Select(x => x.PackageKey))
                    {
                        _pkgServ.SkipVersion(pkg);
                    }

                    _store.Dispatch(PackageStoreAction.ToggleGroup(packages, false));
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