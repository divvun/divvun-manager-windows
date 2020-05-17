using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Divvun.Installer.Models;
using Divvun.Installer.UI.Shared;
using Divvun.Installer.Extensions;
using Divvun.Installer.Util;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;

namespace Divvun.Installer.UI.Main
{
    public class MainPagePresenter
    {
        private ObservableCollection<RepoTreeItem> _tree =
            new ObservableCollection<RepoTreeItem>();

        private IMainPageView _view;
        private UserPackageSelectionStore _store;

        private IDisposable BindPackageToggled(IMainPageView view, IUserPackageSelectionStore store) {
            return view.OnPackageToggled()
                .Select(item => UserSelectionAction.TogglePackage(
                    item.Key,
                    item.Key.DefaultPackageAction(),
                    !item.IsSelected))
                .Subscribe(store.Dispatch);
        }

        private IDisposable BindGroupToggled(IMainPageView view, IUserPackageSelectionStore store) {
            return view.OnGroupToggled()
                .Select(item => {
                    return UserSelectionAction.ToggleGroup(
                        item.Items.Select(x => x.Key.DefaultPackageAction()).ToArray(),
                        !item.IsGroupSelected);
                })
                .Subscribe(store.Dispatch);
        }

        private IDisposable BindPrimaryButton(IMainPageView view) {
            return view.OnPrimaryButtonPressed()
                .ObserveOn(NewThreadScheduler.Default)
                .SubscribeOn(NewThreadScheduler.Default)
                .Subscribe(_ => {
                    Console.WriteLine("Button pressed");
                    var app = (PahkatApp) Application.Current;
                    var actions = _store.Value.SelectedPackages.Values.ToArray();
                    
                    using var guard = app.PackageStore.Lock();
                    guard.Value.ProcessTransaction(actions, value => {
                        Console.WriteLine("-- New event: " + value);
                        var newState = app.CurrentTransaction.Value.Reduce(value);
                        app.CurrentTransaction.OnNext(newState);
                    });
                });
        }

        // private IEnumerable<PackageCategoryTreeItem> FilterByCategory(Dictionary<Descriptor, PackageKey> keyMap) {
        //     var map = new Dictionary<string, List<PackageMenuItem>>();
        //     
        //     foreach (var package in keyMap.Keys) {
        //         foreach (var cat in package.Categories()) {
        //             
        //             if (!map.ContainsKey(cat)) {
        //                 map[cat] = new List<PackageMenuItem>();
        //             }
        //             
        //             
        //             map[cat].Add(new PackageMenuItem(keyMap[package], package, _store));
        //         }
        //     }
        //     
        //     var categories = new ObservableCollection<PackageCategoryTreeItem>(map.OrderBy(x => x.Key).Select(x => {
        //         x.Value.Sort();
        //         var items = new ObservableCollection<PackageMenuItem>(x.Value);
        //         return new PackageCategoryTreeItem(_store, x.Key, items);
        //     }));
        //     
        //     return categories;
        // }

        private RepoTreeItem FilterByTagPrefix(LoadedRepository repo, string prefix, Func<string, string> convertor) {
            // var nodes = new ObservableCollection<PackageCategoryTreeItem>();

            var map = new Dictionary<string, List<PackageMenuItem>>();

            foreach (var package in repo.Packages.Packages()) {
                if (!package.Value.HasValue) {
                    continue;
                }

                Descriptor descriptor = package.Value.Value;

                var pkgKey = PackageKey.Create(repo.Index.Url, descriptor.Id);
                var release = repo.Release(pkgKey);
                var payload = release?.WindowsTarget()?.WindowsExecutable();

                if (!payload.HasValue || !release.HasValue) {
                    continue;
                }

                var tags = descriptor.Tags().Where(x => x.StartsWith(prefix));

                foreach (var tag in tags) {
                    if (!map.ContainsKey(tag)) {
                        map[tag] = new List<PackageMenuItem>();
                    }

                    map[tag].Add(new PackageMenuItem(_store,
                        pkgKey, payload.Value, descriptor.NativeName() ?? "<>", release.Value.Version));
                }
            }

            var categories = new ObservableCollection<PackageCategoryTreeItem>(map.OrderBy(x => x.Key).Select(x => {
                x.Value.Sort();
                var items = new ObservableCollection<PackageMenuItem>(x.Value);
                return new PackageCategoryTreeItem(_store, convertor(x.Key), items);
            }));

            return new RepoTreeItem(repo.Index.NativeName(), categories);
        }

        private RepoTreeItem FilterByLanguage(LoadedRepository repo) {
            return FilterByTagPrefix(repo, "lang:", (tag) => {
                var langTag = tag.Substring(5);
                var r = Iso639.GetTag(langTag);
                return r.Autonym ?? r.Name ?? langTag;
            });
        }
        private RepoTreeItem FilterByCategory(LoadedRepository repo) {
            var app = (PahkatApp) Application.Current;
            Dictionary<Uri, LocalizationStrings> strings;
            
            using (var guard = app.PackageStore.Lock()) {
                strings = guard.Value.Strings("en");
            }
            
            return FilterByTagPrefix(repo, "cat:", (tag) => {
                if (strings.TryGetValue(repo.Index.Url, out var s)) {
                    if (s.Tags.TryGetValue(tag, out var t)) {
                        return t;
                    }
                }

                return tag;
            });
        }

        internal void BindNewRepositories(SortBy sortBy) {
            var app = (PahkatApp) Application.Current;
            using var x = ((PahkatApp) Application.Current).PackageStore.Lock();
            var repos = x.Value.RepoIndexes().Values;
            var records = x.Value.GetRepoRecords();
        
            _tree.Clear();

            if (repos == null) {
                Console.WriteLine("Repository empty.");
                _view.UpdateTitle(Strings.AppName);
                return;
            }

            foreach (var repo in repos) {
                if (!records.ContainsKey(repo.Index.Url)) {
                    continue;
                }

                switch (sortBy) {
                    case SortBy.Category:
                        _tree.Add(FilterByCategory(repo));
                        break;
                    case SortBy.Language:
                        _tree.Add(FilterByLanguage(repo));
                        break;
                }
            }

            _view.UpdateTitle(Strings.AppName);
            Console.WriteLine("Added packages.");
        }

        private void GeneratePrimaryButtonLabel(Dictionary<PackageKey, PackageAction> packages) {
            if (packages.Count > 0) {
                string s;

                if (packages.All(x => x.Value.Action == InstallAction.Install)) {
                    s = string.Format(Strings.InstallNPackages, packages.Count);
                }
                else if (packages.All(x => x.Value.Action == InstallAction.Uninstall)) {
                    s = string.Format(Strings.UninstallNPackages, packages.Count);
                }
                else {
                    s = string.Format(Strings.InstallUninstallNPackages, packages.Count);
                }

                _view.UpdatePrimaryButton(true, s);
            }
            else {
                _view.UpdatePrimaryButton(false, Strings.NoPackagesSelected);
            }
        }

        private IDisposable BindPrimaryButtonLabel(IMainPageView view, IUserPackageSelectionStore store) {
            // Can't use distinct until changed here because HashSet is never reset
            return store.State
                .Select(state => state.SelectedPackages)
                .Subscribe(GeneratePrimaryButtonLabel);
        }

        public MainPagePresenter(IMainPageView view, UserPackageSelectionStore store) {
            _view = view;
            _store = store;
        }

        public IDisposable Start() {
            _view.UpdateTitle($"{Strings.AppName} - {Strings.Loading}");
            _view.SetPackagesModel(_tree);

            return new CompositeDisposable(
                BindPrimaryButtonLabel(_view, _store),
                //BindUpdatePackageList(_repoServ, _pkgServ, _store),
                BindPackageToggled(_view, _store),
                BindGroupToggled(_view, _store),
                BindPrimaryButton(_view)
                // BindNewRepositories(_view)
            );
        }
    }
}