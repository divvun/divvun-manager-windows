using System;
using System.Collections.Generic;
using Iterable;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Divvun.Installer.Extensions;
using Divvun.Installer.UI.About;
using Divvun.Installer.UI.Settings;
using Divvun.Installer.Util;
using ModernWpf.Controls;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.UI.Main
{
    public static class TitleBarHandler
    {
        private static IObservable<Notification> OnReposChanged() {
            var app = (PahkatApp) Application.Current;
            using var guard = app.PackageStore.Lock();
            return guard.Value.Notifications()
                .Filter(x => x == Notification.RepositoriesChanged)
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                .StartWith(Notification.RepositoriesChanged);
        }

        internal static void BindRepoDropdown(CompositeDisposable bag, Action<Uri?> setRepository) {
            var app = (PahkatApp) Application.Current;
            OnReposChanged()
                .CombineLatest(app.Settings.SelectedRepository, (a, b) => b)
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                .Subscribe(setRepository)
                .DisposedBy(bag);
        }
        
        public static void OnClickAboutMenuItem(object sender, RoutedEventArgs e) {
            var app = (PahkatApp) Application.Current;
            app.WindowService.Show<AboutWindow>();
        }

        public static void OnClickSettingsMenuItem(object sender, RoutedEventArgs e) {
            var app = (PahkatApp) Application.Current;
            app.WindowService.Show<SettingsWindow>();
        }

        public static void OnClickExitMenuItem(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }
        
        public static void RefreshFlyoutItems(DropDownButton titleBarReposButton,
            MenuFlyout titleBarReposFlyout,
            LoadedRepository[] repos,
            Dictionary<Uri, RepoRecord> records)
        {
            var app = (PahkatApp) Application.Current;

            titleBarReposFlyout.Items.Clear();
            
            foreach (var repo in repos) {
                if (!records.ContainsKey(repo.Index.Url)) {
                    continue;
                }
                
                var item = new MenuItem();
                var name = repo.Index.NativeName();
                item.Header = name;
                item.Click += (sender, args) => {
                    var r = repo;
                    app.Settings.Mutate(x => {
                        x.SelectedRepository = r.Index.Url;
                    });
                };
                titleBarReposFlyout.Items.Add(item);
            }

            titleBarReposFlyout.Items.Add(new Separator());
            var menu = new MenuItem();
            menu.Header = "All Repositories";
            menu.Click += (sender, args) => {
                app.Settings.Mutate(x => {
                    x.SelectedRepository = new Uri("divvun-installer:detailed");
                });
            }; 
            titleBarReposFlyout.Items.Add(menu);
        }
    }
}