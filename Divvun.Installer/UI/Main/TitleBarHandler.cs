using System;
using System.Collections.Generic;
using Iterable;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Divvun.Installer.Extensions;
using Divvun.Installer.UI.About;
using Divvun.Installer.UI.Settings;
using Divvun.Installer.Util;
using ModernWpf.Controls;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Models;

namespace Divvun.Installer.UI.Main
{
    public static class TitleBarHandler
    {
        private static IObservable<Notification> OnReposChanged() {
            var app = PahkatApp.Current;
            return app.PackageStore.Notifications()
                .Filter(x => x == Notification.RepositoriesChanged)
                .ObserveOn(app.Dispatcher)
                .SubscribeOn(app.Dispatcher)
                .StartWith(Notification.RepositoriesChanged);
        }

        internal static void BindRepoDropdown(CompositeDisposable bag, Action<Uri?> setRepository) {
            var app = PahkatApp.Current;
            OnReposChanged()
                .CombineLatest(app.Settings.SelectedRepository, (a, b) => b)
                .Subscribe(url => {
                    Task.Run(() => setRepository(url));
                })
                .DisposedBy(bag);
        }
        
        public static void OnClickAboutMenuItem(object sender, RoutedEventArgs e) {
            var app = PahkatApp.Current;
            app.WindowService.Show<AboutWindow>();
        }

        public static void OnClickSettingsMenuItem(object sender, RoutedEventArgs e) {
            var app = PahkatApp.Current;
            app.WindowService.Show<SettingsWindow>();
        }

        public static void OnClickExitMenuItem(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }
        
        public static void RefreshFlyoutItems(DropDownButton titleBarReposButton,
            MenuFlyout titleBarReposFlyout,
            ILoadedRepository[] repos,
            Dictionary<Uri, RepoRecord> records)
        {
            var app = PahkatApp.Current;
            app.Dispatcher.Invoke(() =>
            {
                var app = PahkatApp.Current;

                titleBarReposFlyout.Items.Clear();

                foreach (var repo in repos)
                {
                    if (!records.ContainsKey(repo.Index.Url))
                    {
                        continue;
                    }

                    if (repo.Index.LandingUrl == null)
                    {
                        continue;
                    }

                    var item = new MenuItem();
                    var name = repo.Index.NativeName();
                    item.Header = name;
                    item.Click += (sender, args) =>
                    {
                        var r = repo;
                        app.Settings.Mutate(x =>
                        {
                            x.SelectedRepository = r.Index.Url;
                        });
                    };
                    titleBarReposFlyout.Items.Add(item);
                }

                titleBarReposFlyout.Items.Add(new Separator());
                var menu = new MenuItem();
                menu.Header = "All Repositories";
                menu.Click += (sender, args) =>
                {
                    app.Settings.Mutate(x =>
                    {
                        x.SelectedRepository = new Uri("divvun-installer:detailed");
                    });
                };
                titleBarReposFlyout.Items.Add(menu);
            });
        }
    }
}