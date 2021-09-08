using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Divvun.Installer.Extensions;
using Divvun.Installer.Service;
using Divvun.Installer.UI.About;
using Divvun.Installer.UI.Main.Dialog;
using Divvun.Installer.UI.Settings;
using Microsoft.Win32;
using ModernWpf.Controls;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Models;

namespace Divvun.Installer.UI.Main {

public static class TitleBarHandler {
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
            .Subscribe(url => { Task.Run(() => setRepository(url)); })
            .DisposedBy(bag);
    }

    [DllImport("shell32.dll", SetLastError = true)]
    public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl,
        [In] [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

    [DllImport("shell32.dll", SetLastError = true)]
    public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext,
        [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);

    public static void OpenFolderAndSelectItem(string folderPath, string file) {
        IntPtr nativeFolder;
        uint psfgaoOut;
        SHParseDisplayName(folderPath, IntPtr.Zero, out nativeFolder, 0, out psfgaoOut);

        if (nativeFolder == IntPtr.Zero) {
            // Log error, can't find folder
            return;
        }

        IntPtr nativeFile;
        SHParseDisplayName(Path.Combine(folderPath, file), IntPtr.Zero, out nativeFile, 0, out psfgaoOut);

        IntPtr[] fileArray;
        if (nativeFile == IntPtr.Zero) {
            // Open the folder without the file selected if we can't find the file
            fileArray = new IntPtr[0];
        }
        else {
            fileArray = new[] { nativeFile };
        }

        SHOpenFolderAndSelectItems(nativeFolder, (uint)fileArray.Length, fileArray, 0);

        Marshal.FreeCoTaskMem(nativeFolder);
        if (nativeFile != IntPtr.Zero) {
            Marshal.FreeCoTaskMem(nativeFile);
        }
    }

    private static async Task ShowCollationFinish(string zipPath) {
        var fileName = Path.GetFileName(zipPath)!;
        var dirName = Path.GetDirectoryName(zipPath)!;

        Clipboard.SetText("feedback@divvun.no");

        var dialog = new ConfirmationDialog(
            "Debug Data Zipped!",
            $"A zip file named {fileName} has been created.\n\n" +
            "Please attach this to an email to feedback@divvun.no.",
            "(The email address has been automatically copied to your clipboard " +
            "for your convenience. You can paste this into your email program or web-based " +
            "email tool)",
            "Go to file",
            null);

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary) {
            OpenFolderAndSelectItem(dirName, fileName);
        }
    }

    public static void OnClickBundleLogsItem(object sender, RoutedEventArgs e) {
        var mainWindowCfg = PahkatApp.Current.WindowService.Get<MainWindow>();
        var mainWindow = (MainWindow)mainWindowCfg.Instance;

        mainWindow.HideContent();

        PahkatApp.Current.Dispatcher.InvokeAsync(async () => {
            var confirmDialog = new ConfirmationDialog(
                "Create debugging zip file",
                "This function creates a zip file containing logging information useful " +
                "for assisting debugging issues with Divvun Manager and its packages.\n\n" +
                "This tool should only be used when requested by your IT administrator or Divvun personnel.",
                null,
                "Save Debug Zip");

            if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary) {
                mainWindow.ShowContent();
                return;
            }

            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".zip";
            dialog.Filter = "Zip file|*.zip";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            dialog.FileName = "divvun-installer-debug.zip";

            if (dialog.ShowDialog() == true) {
                await LogCollator.Run(dialog.FileName);
                await ShowCollationFinish(dialog.FileName);
            }

            mainWindow.ShowContent();
        });
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
        Dictionary<Uri, RepoRecord> records) {
        var app = PahkatApp.Current;
        app.Dispatcher.Invoke(() => {
            var app = PahkatApp.Current;

            titleBarReposFlyout.Items.Clear();

            foreach (var repo in repos) {
                if (!records.ContainsKey(repo.Index.Url)) {
                    continue;
                }

                if (repo.Index.LandingUrl == null) {
                    continue;
                }

                var item = new MenuItem();
                var name = repo.Index.NativeName();
                item.Header = name;
                item.Click += (sender, args) => {
                    var r = repo;
                    app.Settings.Mutate(x => { x.SelectedRepository = r.Index.Url; });
                };
                titleBarReposFlyout.Items.Add(item);
            }

            titleBarReposFlyout.Items.Add(new Separator());
            var menu = new MenuItem();
            menu.Header = "All Repositories";
            menu.Click += (sender, args) => {
                app.Settings.Mutate(x => { x.SelectedRepository = new Uri("divvun-installer:detailed"); });
            };
            titleBarReposFlyout.Items.Add(menu);
        });
    }
}

}