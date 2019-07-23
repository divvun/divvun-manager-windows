using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.Sdk;
using Pahkat.UI.Shared;

namespace Pahkat.UI.Updater
{
    public class UpdateWindowPresenter
    {
        private readonly IUpdateWindowView _view;
        private readonly UserPackageSelectionStore _store;
        private ObservableCollection<PackageMenuItem> _listItems =
            new ObservableCollection<PackageMenuItem>();
        
        public UpdateWindowPresenter(IUpdateWindowView view, UserPackageSelectionStore store)
        {
            _view = view;
            _store = store;
        }
        
        private void RefreshPackageList(RepositoryIndex[] repos)
        {
            _listItems.Clear();
            _store.Dispatch(UserSelectionAction.ResetSelection);

            foreach (var repo in repos)
            {
                var it = repo.Packages.Values
                    .Select(repo.AbsoluteKeyFor)
                    .Where(x => x.RequiresUpdate())
                    .Select(x => new PackageMenuItem(x, repo.Package(x), _store))
                    .ToArray();
                foreach (var item in it)
                {
                    _store.Dispatch(UserSelectionAction.AddSelectedPackage(item.Key, PackageAction.Install));
                    _listItems.Add(item);
                }
            }
            
            _view.UpdateTitle($"{Strings.AppName} - {string.Format(Strings.NUpdatesAvailable, _listItems.Count)}");
            _view.CloseMainWindow();

            Console.WriteLine("Added packages.");
        }
        
        private IDisposable BindPrimaryButton(IUpdateWindowView view, IUserPackageSelectionStore store)
        {
            return store.State
                .Select(state => state.SelectedPackages)
                .Subscribe(packages =>
                {
                    if (packages.Count > 0)
                    {
                        string s;

                        if (packages.All(x => x.Value.Action == PackageAction.Install))
                        {
                            s = string.Format(Strings.InstallNPackages, packages.Count);
                        }
                        else if (packages.All(x => x.Value.Action == PackageAction.Uninstall))
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
            var app = (PahkatApp) Application.Current;
            
            return Observable.Return(app.PackageStore.RepoIndexes())
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
                .Select(item => UserSelectionAction.TogglePackage(item.Key, PackageAction.Install, !item.IsSelected))
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
                    var app = (PahkatApp)Application.Current;
                    var config = app.PackageStore.Config();

                    foreach (var record in pkgs)
                    {
                        var package = app.PackageStore.ResolvePackage(record.Key);
                        config.AddSkippedVersion(record.Key, package.Version);
                    }

                    var packages = pkgs.Keys.Select(x => new PackageActionInfo(x, PackageAction.Uninstall)).ToArray();
                    _store.Dispatch(UserSelectionAction.ToggleGroup(packages, false));
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