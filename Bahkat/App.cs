using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Properties;
using Bahkat.Service;
using Bahkat.UI.Main;
using Bahkat.UI.Settings;
using Bahkat.UI.Updater;
using Bahkat.Util;
using Hardcodet.Wpf.TaskbarNotification;
using SharpRaven;
using SharpRaven.Data;
using Trustsoft.SingleInstanceApp;

namespace Bahkat
{
    public interface IBahkatApp
    {
        AppConfigStore ConfigStore { get; }
        RepositoryService RepositoryService { get; }
        PackageService PackageService { get; }
        UpdaterService UpdaterService { get; }
        IWindowService WindowService { get; }
        PackageStore PackageStore { get; }
        IRavenClient RavenClient { get; }
    }

    public abstract class AbstractBahkatApp : Application, IBahkatApp, ISingleInstanceApp
    {
        public abstract AppConfigStore ConfigStore { get; }
        public abstract RepositoryService RepositoryService { get; protected set; }
        public abstract PackageService PackageService { get; }
        public abstract UpdaterService UpdaterService { get; protected set; }
        public abstract IWindowService WindowService { get; }
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
        public override IWindowService WindowService { get; } = Service.WindowService.Create(
            CloseHandlingWindowConfig.Create<MainWindow>(),
            CloseHandlingWindowConfig.Create<UpdateWindow>(),
            CloseHandlingWindowConfig.Create<SettingsWindow>()
        );
        public override RepositoryService RepositoryService { get; protected set; }
        public override IRavenClient RavenClient { get; protected set; }
        
        private TaskbarIcon _icon;

        private void CreateNotifyIcon()
        {
            _icon = new TaskbarIcon();
            var uri = new Uri("pack://application:,,,/UI/TaskbarIcon.ico");
            _icon.IconSource = new BitmapImage(uri);
            _icon.ContextMenu = new ContextMenu();
            
            var openPkgMgrItem = new MenuItem { Header = Strings.OpenPackageManager };
            openPkgMgrItem.Click += (sender, args) => WindowService.Show<MainWindow>();
            _icon.ContextMenu.Items.Add(openPkgMgrItem);

            var updateItem = new MenuItem { Header = Strings.CheckForUpdates };
            updateItem.Click += (sender, args) => UpdaterService
                .CheckForUpdatesImmediately()
                .Where(x => x == false)
                .SubscribeOn(DispatcherScheduler.Current)
                .Subscribe(_ => MessageBox.Show(MainWindow,
                    Strings.NoUpdatesBody,
                    Strings.NoUpdatesTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information));
            _icon.ContextMenu.Items.Add(updateItem);
            
            _icon.ContextMenu.Items.Add(new Separator());
            
            var exitItem = new MenuItem { Header = Strings.Exit };
            exitItem.Click += (sender, args) => Current.Shutdown();
            _icon.ContextMenu.Items.Add(exitItem);
            
            _icon.TrayMouseDoubleClick += (sender, args) => WindowService.Show<MainWindow>();
        }

        private void InitRepositoryService()
        {
            RepositoryService = new RepositoryService(RepositoryApi.Create, DispatcherScheduler.Current);
            RepositoryService.System
                .Where(x => x.RepoResult?.Error != null)
                .Select(x => x.RepoResult.Error)
                .DistinctUntilChanged()
                .Subscribe(error => MessageBox.Show(MainWindow,
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
                WindowService.Show<MainWindow>();
            }
            
            // This return value has no purpose.
            return true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            UpdaterService.Dispose();
            base.OnExit(e);
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
            
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                raven.Capture(new SentryEvent((Exception) args.ExceptionObject));
            };

            var application = new BahkatApp { RavenClient = raven };
            application.Run();
                
            SingleInstance<BahkatApp>.Cleanup();
        }
    }
}