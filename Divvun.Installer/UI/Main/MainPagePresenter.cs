using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
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
            // var app = (PahkatApp) Application.Current;
            // var actions = _store.Value.SelectedPackages.Values.ToArray();

            // return view.OnPrimaryButtonPressed().Subscribe(_ => {
            //     app.PackageStore.ProcessTransaction(actions, value => {
            //         Console.WriteLine("-- New event: " + value);
            //         var newState = app.CurrentTransaction.Value.Reduce(value);
            //         app.CurrentTransaction.OnNext(newState);
            //     });
            // });
            //
            // return Disposable.Empty;
            return view.OnPrimaryButtonPressed()
                .ObserveOn(NewThreadScheduler.Default)
                .SubscribeOn(NewThreadScheduler.Default)
                .Subscribe(_ => {
                Console.WriteLine("Button pressed");
                var app = (PahkatApp) Application.Current;
                var actions = _store.Value.SelectedPackages.Values.ToArray();
            
                // var responses = Observable.Create<TransactionResponseValue>(emitter => {
                //     using var x = ((PahkatApp) Application.Current).PackageStore.Lock();
                //     
                //     var token = x.Value.ProcessTransaction(actions, value => {
                //         Console.WriteLine("New event: " + value);
                //         emitter.OnNext(value);
                //
                //         if (value.IsCompletionState || value.IsErrorState) {
                //             emitter.OnCompleted();
                //         }
                //     });
                //
                //     return new CancellationDisposable(token);
                // });
                // view.OnPrimaryButtonPressed().Subscribe(_ => {
                    using var guard = app.PackageStore.Lock();
                    guard.Value.ProcessTransaction(actions, value => {
                        Console.WriteLine("-- New event: " + value);
                        var newState = app.CurrentTransaction.Value.Reduce(value);
                        app.CurrentTransaction.OnNext(newState);
                    });
                // });
            
                // var runningTransaction = Observable.CombineLatest(
                //         app.CurrentTransaction,
                //         responses.DistinctUntilChanged().NotNull(),
                //         (state, value) => (State: state, Event: value))
                //     .SubscribeOn(NewThreadScheduler.Default)
                //     .Do(
                //         next => { Console.WriteLine($"next: {next.State} {next.Event}"); }, 
                //         () => Console.WriteLine("completed"))
                //     .Subscribe(tuple => {
                //         Console.WriteLine(tuple);
                //         app.CurrentTransaction.OnNext(tuple.State.Reduce(tuple.Event));
                //     });
            
                // var runningTransaction = Observable.CombineLatest(
                //         app.CurrentTransaction,
                //         responses,
                //         (state, value) => (State: state, Event: value))
                //     .ObserveOn(NewThreadScheduler.Default)
                //     .SubscribeOn(NewThreadScheduler.Default)
                //     .Select(x => x.State.Reduce(x.Event))
                //     .DistinctUntilChanged()
                //     .Subscribe(state => {
                //         app.CurrentTransaction.OnNext(state);
                //     });
                //
                // app.SetRunningTransaction(runningTransaction);
            });

            // return view.OnPrimaryButtonPressed()
            //     .Subscribe(_ => view.ShowDownloadPage());
            // throw new NotImplementedException();
            // return Disposable.Empty;
        }

        private IEnumerable<PackageCategoryTreeItem> FilterByCategory(Dictionary<Descriptor, PackageKey> keyMap) {
            // var map = new Dictionary<string, List<PackageMenuItem>>();
            //
            // foreach (var package in keyMap.Keys) {
            //     foreach (var cat in package.Categories()) {
            //         
            //         if (!map.ContainsKey(cat)) {
            //             map[cat] = new List<PackageMenuItem>();
            //         }
            //         
            //         
            //         map[cat].Add(new PackageMenuItem(keyMap[package], package, _store));
            //     }
            // }
            //
            // var categories = new ObservableCollection<PackageCategoryTreeItem>(map.OrderBy(x => x.Key).Select(x => {
            //     x.Value.Sort();
            //     var items = new ObservableCollection<PackageMenuItem>(x.Value);
            //     return new PackageCategoryTreeItem(_store, x.Key, items);
            // }));
            //
            // return categories;
            throw new NotImplementedException();
        }

        private IEnumerable<PackageCategoryTreeItem> FilterByLanguage(Dictionary<Package, PackageKey> keyMap) {
            // var map = new Dictionary<string, List<PackageMenuItem>>();
            //
            // foreach (var package in keyMap.Keys) {
            //     foreach (var bcp47 in package.Languages()) {
            //         if (!map.ContainsKey(bcp47)) {
            //             map[bcp47] = new List<PackageMenuItem>();
            //         }
            //
            //         map[bcp47].Add(new PackageMenuItem(keyMap[package], package, _store));
            //     }
            // }
            //
            // var languages = new ObservableCollection<PackageCategoryTreeItem>(map.OrderBy(x => x.Key).Select(x => {
            //     x.Value.Sort();
            //     var items = new ObservableCollection<PackageMenuItem>(x.Value);
            //     return new PackageCategoryTreeItem(_store, Util.Util.GetCultureDisplayName(x.Key), items);
            // }));
            //
            // return languages;
            
            throw new NotImplementedException();
        }

        // private bool IsFoundBySearchQuery(string query, Package package) {
        //     if (string.IsNullOrWhiteSpace(query)) {
        //         return true;
        //     }
        //
        //     const double matchCoefficient = 0.2;
        //
        //     return package.NativeName
        //         .Split(new[] {' '})
        //         .Where(word => word.FuzzyMatch(query) >= matchCoefficient)
        //         .Count() >= 1;
        // }

        private void BindNewRepositories(IMainPageView view) {
            var app = (PahkatApp) Application.Current;
            using var x = ((PahkatApp) Application.Current).PackageStore.Lock();
            var repos = x.Value.RepoIndexes().Values;
        
            _tree.Clear();

            if (repos == null) {
                Console.WriteLine("Repository empty.");
                _view.UpdateTitle(Strings.AppName);
                return;
            }

            foreach (var repo in repos) {
                IEnumerable<PackageCategoryTreeItem> items;



                var packages = repo.Packages.Packages().Select(x => {
                    Descriptor descriptor = x.Value.Value;
                    var pkgKey = PackageKey.Create(repo.Index.Url, descriptor.Id);
                    var release = repo.Release(pkgKey);
                    var payload = release?.WindowsTarget()?.WindowsExecutable();

                    if (payload.HasValue && release.HasValue) {
                        return new PackageMenuItem(_store,
                            pkgKey, payload.Value, descriptor.NativeName() ?? "<>", release.Value.Version);
                    }

                    return null;
                }).Where(x => x != null).Select(x => x!);
                
                var categoryTree = new PackageCategoryTreeItem(_store, "Test", 
                    new ObservableCollection<PackageMenuItem>(packages));
                // switch (repo.Meta.PrimaryFilter) {
                //     case RepositoryMeta.Filter.Language:
                //         items = FilterByLanguage(keyMap);
                //         break;
                //     case RepositoryMeta.Filter.Category:
                //     default:
                //         items = FilterByCategory(keyMap);
                //         break;
                // }

                var item = new RepoTreeItem(
                    repo.Index.NativeName() ?? "Unknown repo name",
                    new ObservableCollection<PackageCategoryTreeItem>(new [] { categoryTree })
                );

                _tree.Add(item);
                _view.UpdateTitle(Strings.AppName);
                Console.WriteLine("Added packages.");
            }
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
            BindNewRepositories(_view);

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