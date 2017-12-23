using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Properties;
using Bahkat.Service;
using Bahkat.UI.Main;
using Bahkat.UI.Updater;
using Bahkat.Util;
using Hardcodet.Wpf.TaskbarNotification;
using SharpRaven;
using SharpRaven.Data;
using Trustsoft.SingleInstanceApp;

namespace Bahkat
{
    public static class Shared {
        public static String BytesToString(Int64 bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (bytes == 0)
            {
                return "0 " + suf[0];
            }
            Int32 place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            Double num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return num.ToString(CultureInfo.CurrentCulture) + " " + suf[place];
        }
    }
    
    public interface IBahkatApp
    {
        AppConfigStore ConfigStore { get; }
        RepositoryService RepositoryService { get; }
        PackageService PackageService { get; }
        UpdaterService UpdaterService { get; }
        PackageStore PackageStore { get; }
        IRavenClient RavenClient { get; }

        void ShowUpdaterWindow();
    }

    public abstract class AbstractBahkatApp : Application, IBahkatApp, ISingleInstanceApp
    {
        public abstract AppConfigStore ConfigStore { get; }
        public abstract RepositoryService RepositoryService { get; protected set; }
        public abstract PackageService PackageService { get; }
        public abstract UpdaterService UpdaterService { get; protected set; }
        public abstract PackageStore PackageStore { get; }
        public abstract IRavenClient RavenClient { get; protected set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // main window turn on, who set up us the bomb
            ConfigStore.State
                .Select(s => s.RepositoryUrl)
                .Subscribe(RepositoryService.SetRepositoryUri);
        }

        public abstract bool OnActivate(IList<string> args);
        public abstract void ShowUpdaterWindow();
    }

    static class DI
    {
        internal static AppConfigStore CreateAppConfigStore()
        {
#if DEBUG
            return new AppConfigStore(new MockRegistry());
#else
            return new AppConfigStore(new WindowsRegistry());
#endif
        }

        internal static PackageService CreatePackageService()
        {
#if DEBUG
            return new PackageService(new MockRegistry());
#else
            return new PackageService(new WindowsRegistry());
#endif
        }

        internal static RavenClient CreateRavenClient()
        {
            return new RavenClient(Constants.SentryDsn);
        }
    }

    public class BahkatApp : AbstractBahkatApp
    {
        public override AppConfigStore ConfigStore { get; } = DI.CreateAppConfigStore();
        public override PackageService PackageService { get; } = DI.CreatePackageService();
        public override PackageStore PackageStore { get; } = new PackageStore();
        public override UpdaterService UpdaterService { get; protected set; }
        public override RepositoryService RepositoryService { get; protected set; }
        public override IRavenClient RavenClient { get; protected set; }

        private IMainWindowView _mainWindow;
        private IUpdateWindowView _updaterWindow;

        private TaskbarIcon _icon;

        private void MainClosingHandler(object sender, CancelEventArgs args)
        {
            args.Cancel = true;
            var w = (Window) _mainWindow;
            w.Hide();
            w.Closing -= MainClosingHandler;
            _mainWindow = null;
        }
        
        private void UpdaterClosingHandler(object sender, CancelEventArgs args)
        {
            args.Cancel = true;
            var w = (Window) _updaterWindow;
            w.Hide();
            w.Closing -= UpdaterClosingHandler;
            _updaterWindow = null;
        }

        private IMainWindowView CreateMainWindow()
        {
            var window = new MainWindow();
            window.Closing += MainClosingHandler;
            return window;
        }

        private IUpdateWindowView CreateUpdaterWindow()
        {
            var window = new UpdateWindow();
            window.Closing += UpdaterClosingHandler;
            return window;
        }

        public override void ShowUpdaterWindow()
        {
            if (_updaterWindow == null)
            {
                _updaterWindow = CreateUpdaterWindow();
            }

            _updaterWindow.Show();
        }

        private void ShowMainWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow = CreateMainWindow();
            }

            _mainWindow.Show();
        }

        private void CreateNotifyIcon()
        {
            _icon = new TaskbarIcon();
            var uri = new Uri("pack://application:,,,/UI/TaskbarIcon.ico");
            _icon.IconSource = new BitmapImage(uri);
            _icon.ContextMenu = new ContextMenu();
            var openPkgMgrItem = new MenuItem()
            {
                Header = Strings.OpenPackageManager
            };
            openPkgMgrItem.Click += (sender, args) => ShowMainWindow();
            _icon.ContextMenu.Items.Add(openPkgMgrItem);
            _icon.ContextMenu.Items.Add(new Separator());
            var exitItem = new MenuItem()
            {
                Header = Strings.Exit
            };
            exitItem.Click += (sender, args) => Application.Current.Shutdown();
            _icon.ContextMenu.Items.Add(exitItem);
            _icon.TrayMouseDoubleClick += (sender, args) => ShowMainWindow();
        }

        private void InitRepositoryService()
        {
            RepositoryService = new RepositoryService(RepositoryApi.Create, DispatcherScheduler.Current);
            RepositoryService.System
                .Where(x => x.RepoResult?.Error != null)
                .Select(x => x.RepoResult.Error)
                .DistinctUntilChanged()
                .Subscribe(error => MessageBox.Show(
                    error.Message,
                    Strings.RepositoryError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error));
        }

        private void InitUpdaterService()
        {
            UpdaterService = new UpdaterService(ConfigStore, RepositoryService, PackageService);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            InitRepositoryService();
            InitUpdaterService();
            CreateNotifyIcon();
            
            base.OnStartup(e);
            OnActivate(Environment.GetCommandLineArgs());
        }

        public override bool OnActivate(IList<string> args)
        {
            // If -s, run silently. Used for start-up service.
            if (!args.Contains("-s"))
            {
                ShowMainWindow();
            }
            
            // This return value has no purpose.
            return true;
        }

        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlAppDomain)]
        public static void Main()
        {
            var key = Assembly.GetExecutingAssembly().GetGuid();

            if (!SingleInstance<BahkatApp>.InitializeAsFirstInstance(key))
            {
                return;
            }
            
            var raven = DI.CreateRavenClient();
            
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += (sender, args) =>
            {
                raven.Capture(new SentryEvent((Exception) args.ExceptionObject));
            };

            var application = new BahkatApp { RavenClient = raven };
            application.Run();
                
            SingleInstance<BahkatApp>.Cleanup();
        }
    }
}