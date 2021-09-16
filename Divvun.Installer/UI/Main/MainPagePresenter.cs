using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using Castle.Core.Internal;
using Divvun.Installer.Extensions;
using Divvun.Installer.Models;
using Divvun.Installer.UI.Shared;
using Divvun.Installer.Util;
using Iterable;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Models;
using Serilog;
using Iter = Iterable.Iterable;

namespace Divvun.Installer.UI.Main {

public class MainPagePresenter {
    private readonly UserPackageSelectionStore _store;

    private readonly ObservableCollection<RepoTreeItem> _tree =
        new ObservableCollection<RepoTreeItem>();

    private readonly IMainPageView _view;

    public MainPagePresenter(IMainPageView view, UserPackageSelectionStore store) {
        _view = view;
        _store = store;
    }

    private IDisposable BindPackageToggled(IMainPageView view, UserPackageSelectionStore store) {
        return view.OnPackageToggled().Subscribe(item => {
            PahkatApp.Current.Dispatcher.InvokeAsync(async () => {
                Log.Verbose("Toggle package");
                await store.TogglePackageWithDefaultAction(item.Key, !item.IsSelected);
            });
        });
    }

    private IDisposable BindGroupToggled(IMainPageView view, UserPackageSelectionStore store) {
        return view.OnGroupToggled()
            .Subscribe(tree => {
                PahkatApp.Current.Dispatcher.InvokeAsync(async () => {
                    Log.Verbose("Toggle group");
                    var list = new List<PackageKey>();
                    foreach (var task in tree.Items.Map(x => x.Key.DefaultPackageAction())) {
                        var v = await task;
                        list.Add(v.PackageKey);
                    }

                    await store.ToggleGroupWithDefaultAction(Iter.ToArray(list), !tree.IsGroupSelected);
                });
            });
    }

    private IDisposable BindPrimaryButton(IMainPageView view) {
        return view.OnPrimaryButtonPressed()
            .Subscribe(_ => {
                Log.Verbose("Primary button pressed");
                var app = PahkatApp.Current;
                var actions = Iter.ToArray(_store.State.SelectedPackages.Values);

                Task.Run(async () => {
                    try {
                        await app.PackageStore.ProcessTransaction(actions, value => {
                            Log.Debug("-- New event: " + value);
                            var newState = app.CurrentTransaction.Value.Reduce(value);
                            app.CurrentTransaction.OnNext(newState);
                        });
                    }
                    catch (Exception e) {
                        _view.HandleError(e);
                    }
                });
            });
    }

    private RepoTreeItem FilterByTagPrefix(ILoadedRepository repo, string prefix, Func<string, string> convertor) {
        // var nodes = new ObservableCollection<PackageCategoryTreeItem>();

        var map = new Dictionary<string, List<PackageMenuItem>>();

        foreach (var package in repo.Packages.Packages) {
            if (package.Value == null) {
                continue;
            }

            IDescriptor descriptor = package.Value!;

            var pkgKey = PackageKey.Create(repo.Index.Url, descriptor.Id);
            var release = repo.Release(pkgKey);
            var payload = release?.WindowsExecutable();

            if (release == null || payload == null) {
                continue;
            }

            var tags = Iter.ToArray(descriptor.Tags.Where(x => x.StartsWith(prefix)));

            if (tags.IsNullOrEmpty()) {
                tags = new[] { prefix };
            }

            foreach (var tag in tags) {
                if (!map.ContainsKey(tag)) {
                    map[tag] = new List<PackageMenuItem>();
                }

                map[tag].Add(new PackageMenuItem(_store,
                    pkgKey, payload, descriptor.NativeName() ?? "<>", release.Version));
            }
        }

        var categoriesSet = new SortedSet<PackageCategoryTreeItem>(map.OrderBy(x => x.Key).Map(x => {
            x.Value.Sort();
            var items = new ObservableCollection<PackageMenuItem>(x.Value);
            return new PackageCategoryTreeItem(_store, convertor(x.Key), items);
        }));

        var categories = new ObservableCollection<PackageCategoryTreeItem>(categoriesSet);

        return new RepoTreeItem(repo.Index.NativeName(), categories);
    }

    private RepoTreeItem FilterByLanguage(ILoadedRepository repo) {
        return FilterByTagPrefix(repo, "lang:", tag => {
            var langTag = tag.Substring(5);
            var r = Iso639.GetTag(langTag);
            return $"{Util.Util.GetCultureDisplayName(langTag)} [{langTag}]";
        });
    }

    private async Task<RepoTreeItem> FilterByCategory(ILoadedRepository repo) {
        var app = (PahkatApp)Application.Current;
        Dictionary<Uri, LocalizationStrings> strings = await app.PackageStore.Strings(app.Settings.GetLanguage() ?? "en");

        return FilterByTagPrefix(repo, "cat:", tag => {
            if (tag == "cat:") {
                return "Uncategorized";
            }

            if (strings.TryGetValue(repo.Index.Url, out var s)) {
                if (s.Tags.TryGetValue(tag, out var t)) {
                    return t;
                }
            }

            return tag;
        });
    }

    internal async Task BindNewRepositories(SortBy sortBy) {
        var app = PahkatApp.Current;
        var repos = (await app.PackageStore.RepoIndexes()).Values;
        var records = await app.PackageStore.GetRepoRecords();

        _tree.Clear();

        var sorted = new SortedSet<RepoTreeItem>();

        if (repos.IsNullOrEmpty()) {
            Log.Debug("Repository empty.");
            _view.UpdateTitle(Strings.AppName);
            return;
        }

        foreach (var repo in repos) {
            if (!records.ContainsKey(repo.Index.Url)) {
                continue;
            }

            switch (sortBy) {
            case SortBy.Category:
                sorted.Add(await FilterByCategory(repo));
                break;
            case SortBy.Language:
                sorted.Add(FilterByLanguage(repo));
                break;
            }
        }
        
        foreach (var repoTreeItem in sorted) {
            _tree.Add(repoTreeItem);
        }

        _view.UpdateTitle(Strings.AppName);
        Log.Debug("Added packages.");
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

    private IDisposable BindPrimaryButtonLabel(IMainPageView view, UserPackageSelectionStore store) {
        // Can't use distinct until changed here because HashSet is never reset
        return store.SelectedPackages()
            .Subscribe(GeneratePrimaryButtonLabel);
    }

    public IDisposable Start() {
        _view.SetPackagesModel(_tree);

        return new CompositeDisposable(
            BindPrimaryButtonLabel(_view, _store),
            BindPackageToggled(_view, _store),
            BindGroupToggled(_view, _store),
            BindPrimaryButton(_view)
        );
    }
}

}