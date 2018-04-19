using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.Service;
using Pahkat.UI.Shared;

namespace Pahkat.UI.Main
{
    public class MainPagePresenter
    {
        private ObservableCollection<RepoTreeItem> _tree =
            new ObservableCollection<RepoTreeItem>();
        
        private IMainPageView _view;
        //private RepositoryService _repoServ;
        private IPackageService _pkgServ;
        private IPackageStore _store;

        private Repository[] _currentRepos;

        private IDisposable BindPackageToggled(IMainPageView view, IPackageStore store)
        {
            
            return view.OnPackageToggled()
                .Select(item => PackageStoreAction.TogglePackage(
                    item.Model,
                    _pkgServ.DefaultPackageAction(item.Model),
                    !item.IsSelected))
                .Subscribe(store.Dispatch);
        }
        
        private IDisposable BindGroupToggled(IMainPageView view, IPackageStore store)
        {
            return view.OnGroupToggled()
                .Select(item =>
                {
                    return PackageStoreAction.ToggleGroup(
                        item.Items.Select(x => new PackageActionInfo(x.Model, _pkgServ.DefaultPackageAction(x.Model))).ToArray(),
                        !item.IsGroupSelected);
                })
                .Subscribe(store.Dispatch);
        }

        private IDisposable BindPrimaryButton(IMainPageView view)
        {
            return view.OnPrimaryButtonPressed()
                .Subscribe(_ => view.ShowDownloadPage());
        }

        private IEnumerable<PackageCategoryTreeItem> FilterByCategory(Repository repo)
        {
            var map = new Dictionary<string, List<PackageMenuItem>>();

            foreach (var package in repo.Packages.Values)
            {
                if (!map.ContainsKey(package.Category))
                {
                    map[package.Category] = new List<PackageMenuItem>();
                }

                map[package.Category].Add(new PackageMenuItem(package, _pkgServ, _store));
            }

            var categories = new ObservableCollection<PackageCategoryTreeItem>(map.OrderBy(x => x.Key).Select(x =>
            {
                x.Value.Sort();
                var items = new ObservableCollection<PackageMenuItem>(x.Value);
                return new PackageCategoryTreeItem(_store, x.Key, items);
            }));

            return categories;
        }

        private IEnumerable<PackageCategoryTreeItem> FilterByLanguage(Repository repo)
        {
            var map = new Dictionary<string, List<PackageMenuItem>>();

            foreach (var package in repo.Packages.Values)
            {
                foreach (var bcp47 in package.Languages)
                {
                    if (!map.ContainsKey(bcp47))
                    {
                        map[bcp47] = new List<PackageMenuItem>();
                    }

                    map[bcp47].Add(new PackageMenuItem(package, _pkgServ, _store));
                }
            }

            var languages = new ObservableCollection<PackageCategoryTreeItem>(map.OrderBy(x => x.Key).Select(x =>
            {
                x.Value.Sort();
                var items = new ObservableCollection<PackageMenuItem>(x.Value);
                return new PackageCategoryTreeItem(_store, Util.Util.GetCultureDisplayName(x.Key), items);
            }));

            return languages;
        }
        
        private void RefreshPackageList()
        {
            _tree.Clear();
            
            if (_currentRepos == null)
            {
                Console.WriteLine("Repository empty.");
                _view.UpdateTitle(Strings.AppName);
                return;
            }

            foreach (var repo in _currentRepos)
            {
                IEnumerable<PackageCategoryTreeItem> items;
                switch (repo.Meta.PrimaryFilter)
                {
                    case RepositoryMeta.Filter.Language:
                        items = FilterByLanguage(repo);
                        break;
                    case RepositoryMeta.Filter.Category:
                    default:
                        items = FilterByCategory(repo);
                        break;
                }

                var item = new RepoTreeItem(repo.Meta.NativeName,
                    new ObservableCollection<PackageCategoryTreeItem>(items));
                _tree.Add(item);
            }

            _view.UpdateTitle($"{Strings.AppName}");
            Console.WriteLine("Added packages.");
        }

        //private IDisposable BindUpdatePackageList(RpcService rpc, IPackageService pkgServ, IPackageStore store)
        //{
        //    return rpc.Repository()
        //    return repoServ.System
        //        .Select(x => x.RepoResult?.Repository)
        //        .NotNull()
        //        .DistinctUntilChanged()
        //        .Subscribe(repo =>
        //        {
        //            _currentRepo = repo;
        //            RefreshPackageList();
        //        }, _view.HandleError);
        //}

        private void GeneratePrimaryButtonLabel(Dictionary<Package, PackageActionInfo> packages)
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

                _view.UpdatePrimaryButton(true, s);
            }
            else
            {
                _view.UpdatePrimaryButton(false, Strings.NoPackagesSelected);
            }
        }

        private IDisposable BindPrimaryButtonLabel(IMainPageView view, IPackageStore store)
        {
            // Can't use distinct until changed here because HashSet is never reset
            return store.State
                .Select(state => state.SelectedPackages)
                .Subscribe(GeneratePrimaryButtonLabel);
        }

        public void SetRepos(Repository[] repos)
        {
            _currentRepos = repos;
            RefreshPackageList();
        }

        public MainPagePresenter(IMainPageView view, IPackageService pkgServ, IPackageStore store)
        {
            //_repoServ = repoServ;
            _pkgServ = pkgServ;
            _view = view;
            _store = store;
        }

        public IDisposable Start()
        {
            _view.UpdateTitle($"{Strings.AppName} - {Strings.Loading}");
            _view.SetPackagesModel(_tree);

            return new CompositeDisposable(
                BindPrimaryButtonLabel(_view, _store),
                //BindUpdatePackageList(_repoServ, _pkgServ, _store),
                BindPackageToggled(_view, _store),
                BindGroupToggled(_view, _store),
                BindPrimaryButton(_view)
            );
        }
    }
}