using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Bahkat.Models;
using Bahkat.Service;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Main
{
    public interface IMainPageView : IPageView
    {
        IObservable<PackageMenuItem> OnPackageToggled();
        IObservable<PackageCategoryTreeItem> OnGroupToggled();
        IObservable<EventArgs> OnPrimaryButtonPressed();
        void UpdateTitle(string title);
        void SetPackagesModel(ObservableCollection<PackageCategoryTreeItem> tree);
        void ShowDownloadPage();
        void UpdatePrimaryButton(bool isEnabled, string label);
        void HandleError(Exception error);
    }

    public class MainPagePresenter
    {
        private ObservableCollection<PackageCategoryTreeItem> _tree =
            new ObservableCollection<PackageCategoryTreeItem>();
        
        private IMainPageView _view;
        private RepositoryService _repoServ;
        private PackageService _pkgServ;
        private PackageStore _store;

        private Repository _currentRepo;

        private IDisposable BindPackageToggled(IMainPageView view, PackageStore store)
        {
            return view.OnPackageToggled()
                .Select(item => PackageAction.TogglePackage(item.Model, !item.IsSelected))
                .Subscribe(store.Dispatch);
        }
        
        private IDisposable BindGroupToggled(IMainPageView view, PackageStore store)
        {
            return view.OnGroupToggled()
                .Select(item => PackageAction.ToggleGroup(item.Items.Select(x => x.Model).ToArray(), !item.IsGroupSelected))
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
            
            foreach (var package in repo.PackagesIndex.Values)
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
            
            foreach (var package in repo.PackagesIndex.Values)
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
                return new PackageCategoryTreeItem(_store, new CultureInfo(x.Key).DisplayName, items);
            });
            
        }
        
        private void RefreshPackageList()
        {
            _tree.Clear();
            
            if (_currentRepo == null)
            {
                _tree.Add(new PackageCategoryTreeItem(_store, "The repo failed to download.", null));
                Console.WriteLine("Repository empty.");
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
            
            Console.WriteLine("Added packages.");
        }

        private IDisposable BindUpdatePackageList(RepositoryService repoServ, PackageService pkgServ, PackageStore store)
        {
            return repoServ.System
                .Where(x => x.RepoResult?.Repository != null)
                .Select(x => x.RepoResult.Repository)
                .DistinctUntilChanged()
                .Subscribe(repo =>
                {
                    _currentRepo = repo;
                    RefreshPackageList();
                }, _view.HandleError);
        }

        private IDisposable BindPrimaryButtonLabel(IMainPageView view, PackageStore store)
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

        public MainPagePresenter(IMainPageView view, RepositoryService repoServ, PackageService pkgServ, PackageStore store)
        {
            _repoServ = repoServ;
            _pkgServ = pkgServ;
            _view = view;
            _store = store;
        }

        public IDisposable Start()
        {
            _view.SetPackagesModel(_tree);

            return new CompositeDisposable(
                BindPrimaryButtonLabel(_view, _store),
                BindUpdatePackageList(_repoServ, _pkgServ, _store),
                BindPackageToggled(_view, _store),
                BindGroupToggled(_view, _store),
                BindPrimaryButton(_view)
//                BindFilter(_view, _repoServ)
            );
        }
    }
}