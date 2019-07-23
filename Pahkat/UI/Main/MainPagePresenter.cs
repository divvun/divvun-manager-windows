using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Pahkat.Models;
using Pahkat.Sdk;
using Pahkat.UI.Shared;
using DuoVia.FuzzyStrings;
using Pahkat.Extensions;

namespace Pahkat.UI.Main
{
    public class MainPagePresenter
    {
        private ObservableCollection<RepoTreeItem> _tree =
            new ObservableCollection<RepoTreeItem>();
        
        private IMainPageView _view;
        private UserPackageSelectionStore _store;

        private IDisposable BindPackageToggled(IMainPageView view, IUserPackageSelectionStore store)
        {
            return view.OnPackageToggled()
                .Select(item => UserSelectionAction.TogglePackage(
                    item.Key,
                    item.Key.DefaultPackageAction(),
                    !item.IsSelected))
                .Subscribe(store.Dispatch);
        }
        
        private IDisposable BindGroupToggled(IMainPageView view, IUserPackageSelectionStore store)
        {
            return view.OnGroupToggled()
                .Select(item =>
                {
                    return UserSelectionAction.ToggleGroup(
                        item.Items.Select(x => new PackageActionInfo(x.Key)).ToArray(),
                        !item.IsGroupSelected);
                })
                .Subscribe(store.Dispatch);
        }

        private IDisposable BindPrimaryButton(IMainPageView view)
        {
            return view.OnPrimaryButtonPressed()
                .Subscribe(_ => view.ShowDownloadPage());
        }

        private IEnumerable<PackageCategoryTreeItem> FilterByCategory(Dictionary<Package, AbsolutePackageKey> keyMap)
        {
            var map = new Dictionary<string, List<PackageMenuItem>>();

            foreach (var package in keyMap.Keys)
            {
                if (!map.ContainsKey(package.Category))
                {
                    map[package.Category] = new List<PackageMenuItem>();
                }

                map[package.Category].Add(new PackageMenuItem(keyMap[package], package, _store));
            }

            var categories = new ObservableCollection<PackageCategoryTreeItem>(map.OrderBy(x => x.Key).Select(x =>
            {
                x.Value.Sort();
                var items = new ObservableCollection<PackageMenuItem>(x.Value);
                return new PackageCategoryTreeItem(_store, x.Key, items);
            }));

            return categories;
        }

        private IEnumerable<PackageCategoryTreeItem> FilterByLanguage(Dictionary<Package, AbsolutePackageKey> keyMap)
        {
            var map = new Dictionary<string, List<PackageMenuItem>>();

            foreach (var package in keyMap.Keys)
            {
                foreach (var bcp47 in package.Languages)
                {
                    if (!map.ContainsKey(bcp47))
                    {
                        map[bcp47] = new List<PackageMenuItem>();
                    }

                    map[bcp47].Add(new PackageMenuItem(keyMap[package], package, _store));
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

        private bool IsFoundBySearchQuery(string query, Package package)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            const double matchCoefficient = 0.2;

            return package.NativeName
                .Split(new[] { ' ' })
                .Where(word => word.FuzzyMatch(query) >= matchCoefficient)
                .Count() >= 1;
        }

        private IDisposable BindNewRepositories(IMainPageView view)
        {
            return Observable.CombineLatest(_view.OnNewRepositories(), _view.OnSearchTextChanged(), (repos, query) =>
            {
                return new Tuple<RepositoryIndex[], string>(repos, query);
            })
            .Subscribe((t) =>
            {
                var repos = t.Item1;
                var query = t.Item2;

                _tree.Clear();

                if (repos == null)
                {
                    Console.WriteLine("Repository empty.");
                    _view.UpdateTitle(Strings.AppName);
                    return;
                }

                foreach (var repo in repos)
                {
                    IEnumerable<PackageCategoryTreeItem> items;


                    var keyMap = new Dictionary<Package, AbsolutePackageKey>();
                    foreach (var pkg in repo.Packages.Values.Where((pkg) => IsFoundBySearchQuery(query, pkg))) {
                        keyMap[pkg] = repo.AbsoluteKeyFor(pkg);
                    };
    
                    switch (repo.Meta.PrimaryFilter)
                    {
                        case RepositoryMeta.Filter.Language:
                            items = FilterByLanguage(keyMap);
                            break;
                        case RepositoryMeta.Filter.Category:
                        default:
                            items = FilterByCategory(keyMap);
                            break;
                    }

                    var item = new RepoTreeItem(
                        repo.Meta.NativeName,
                        new ObservableCollection<PackageCategoryTreeItem>(items)
                    );

                    _tree.Add(item);
                    _view.UpdateTitle(Strings.AppName);
                    Console.WriteLine("Added packages.");
                }

            }, _view.HandleError);
        }

        private void GeneratePrimaryButtonLabel(Dictionary<AbsolutePackageKey, PackageActionInfo> packages)
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

        private IDisposable BindPrimaryButtonLabel(IMainPageView view, IUserPackageSelectionStore store)
        {
            // Can't use distinct until changed here because HashSet is never reset
            return store.State
                .Select(state => state.SelectedPackages)
                .Subscribe(GeneratePrimaryButtonLabel);
        }

        public MainPagePresenter(IMainPageView view, UserPackageSelectionStore store)
        {
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
                BindPrimaryButton(_view),
                BindNewRepositories(_view)
            );
        }
    }
}