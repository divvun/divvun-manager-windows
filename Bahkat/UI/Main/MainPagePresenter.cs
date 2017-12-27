using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Service;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Main
{
    public class MainPagePresenter
    {
        private ObservableCollection<PackageCategoryTreeItem> _tree =
            new ObservableCollection<PackageCategoryTreeItem>();
        
        private IMainPageView _view;
        private RepositoryService _repoServ;
        private IPackageService _pkgServ;
        private PackageStore _store;

        private Repository _currentRepo;

        private IDisposable BindPackageToggled(IMainPageView view, PackageStore store)
        {
            
            return view.OnPackageToggled()
                .Select(item => PackageStoreAction.TogglePackage(
                    item.Model,
                    _pkgServ.DefaultPackageAction(item.Model),
                    !item.IsSelected))
                .Subscribe(store.Dispatch);
        }
        
        private IDisposable BindGroupToggled(IMainPageView view, PackageStore store)
        {
            return view.OnGroupToggled()
                .Select(item =>
                {
                    return PackageStoreAction.ToggleGroup(
                        item.Items.Select(x => new PackageActionInfo {
                            Package = x.Model,
                            Action = _pkgServ.DefaultPackageAction(x.Model)
                        }).ToArray(),
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

            return map.OrderBy(x => x.Key).Select(x =>
            {
                x.Value.Sort();
                var items = new ObservableCollection<PackageMenuItem>(x.Value);
                return new PackageCategoryTreeItem(_store, x.Key, items);
            });
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

            return map.OrderBy(x => x.Key).Select(x =>
            {
                x.Value.Sort();
                var items = new ObservableCollection<PackageMenuItem>(x.Value);
                return new PackageCategoryTreeItem(_store, Util.Util.GetCultureDisplayName(x.Key), items);
            });
            
        }
        
        private void RefreshPackageList()
        {
            _tree.Clear();
            
            if (_currentRepo == null)
            {
                Console.WriteLine("Repository empty.");
                _view.UpdateTitle(Strings.AppName);
                return;
            }

            IEnumerable<PackageCategoryTreeItem> categoryItems;
            switch (_currentRepo.Meta.PrimaryFilter)
            {
                case "language":
                    categoryItems = FilterByLanguage(_currentRepo);
                    break;
                default:
                    categoryItems = FilterByCategory(_currentRepo);
                    break;
            }

            foreach (var item in categoryItems)
            {
                _tree.Add(item);
            }
            
            _view.UpdateTitle($"{Strings.AppName} - {_currentRepo.Meta.NativeName}");
            Console.WriteLine("Added packages.");
        }

        private IDisposable BindUpdatePackageList(RepositoryService repoServ, IPackageService pkgServ, PackageStore store)
        {
            return repoServ.System
                .Select(x => x.RepoResult?.Repository)
                .NotNull()
                .DistinctUntilChanged()
                .Subscribe(repo =>
                {
                    _currentRepo = repo;
                    RefreshPackageList();
                }, _view.HandleError);
        }

        private void GeneratePrimaryButtonLabel(Dictionary<Package, PackageActionInfo> infos)
        {
            if (infos.Count == 0)
            {
                _view.UpdatePrimaryButton(false, Strings.NoPackagesSelected);
                return;
            }

            _view.UpdatePrimaryButton(true, string.Format(Strings.ProcessNPackages, infos.Count));
        }

        private IDisposable BindPrimaryButtonLabel(IMainPageView view, PackageStore store)
        {
            // Can't use distinct until changed here because HashSet is never reset
            return store.State
                .Select(state => state.SelectedPackages)
                .Subscribe(GeneratePrimaryButtonLabel);
        }

        public MainPagePresenter(IMainPageView view, RepositoryService repoServ, IPackageService pkgServ, PackageStore store)
        {
            _repoServ = repoServ;
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
                BindUpdatePackageList(_repoServ, _pkgServ, _store),
                BindPackageToggled(_view, _store),
                BindGroupToggled(_view, _store),
                BindPrimaryButton(_view)
            );
        }
    }
}