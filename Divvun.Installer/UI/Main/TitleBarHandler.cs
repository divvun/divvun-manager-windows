using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Divvun.Installer.Extensions;
using Divvun.Installer.UI.About;
using Divvun.Installer.UI.Settings;
using ModernWpf.Controls;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.UI.Main
{
    public static class TitleBarHandler
    {
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
        
        public static void RefreshFlyoutItems(MenuFlyout titleBarReposFlyout) {
            var app = (PahkatApp) Application.Current;
            LoadedRepository[] repos;
            using (var guard = app.PackageStore.Lock()) {
                repos = guard.Value.RepoIndexes().Values.ToArray();
            }
            titleBarReposFlyout.Items.Clear();
            
            foreach (var repo in repos) {
                var item = new MenuItem();
                var name = repo.Index.NativeName();
                item.Header = name;
                item.Click += (sender, args) => {
                    app.Settings.Mutate(x => {
                        x.SelectedRepository = repo.Index.Url;
                    });
                };
                titleBarReposFlyout.Items.Add(item);
            }

            titleBarReposFlyout.Items.Add(new Separator());
            var menu = new MenuItem();
            menu.Header = "Show detailed view...";
            menu.Click += (sender, args) => {
                app.Settings.Mutate(x => {
                    x.SelectedRepository = new Uri("divvun-installer:detailed");
                });
            }; 
            titleBarReposFlyout.Items.Add(menu);
        }
    }
}