using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.Properties;
using Pahkat.Service;
using Pahkat.UI.Main;
using Pahkat.UI.Settings;
using Pahkat.UI.Updater;
using Pahkat.Util;
using Hardcodet.Wpf.TaskbarNotification;
using SharpRaven;
using SharpRaven.Data;
using Trustsoft.SingleInstanceApp;

namespace Pahkat
{
    public interface IBahkatApp
    {
        AppConfigStore ConfigStore { get; }
        RepositoryService RepositoryService { get; }
        IPackageService PackageService { get; }
        UpdaterService UpdaterService { get; }
        IWindowService WindowService { get; }
        IPackageStore PackageStore { get; }
        IRavenClient RavenClient { get; }
        SelfUpdaterService SelfUpdateService { get; }
    }

    public abstract class AbstractBahkatApp : Application, IBahkatApp, ISingleInstanceApp
    {
        public abstract AppConfigStore ConfigStore { get; }
        public abstract RepositoryService RepositoryService { get; protected set; }
        public abstract IPackageService PackageService { get; }
        public abstract UpdaterService UpdaterService { get; protected set; }
        public abstract IWindowService WindowService { get; }
        public abstract IPackageStore PackageStore { get; protected set; }
        public abstract IRavenClient RavenClient { get; protected set; }
        public abstract SelfUpdaterService SelfUpdateService { get; protected set; }

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

        internal static IPackageService CreatePackageService()
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
        public const string ArgsSilent = "-s";

        public override AppConfigStore ConfigStore { get; } = DI.CreateAppConfigStore();
        public override IPackageService PackageService { get; } = DI.CreatePackageService();
        public override IPackageStore PackageStore { get; protected set; }
        public override UpdaterService UpdaterService { get; protected set; }
        public override IWindowService WindowService { get; } = Service.WindowService.Create(
            CloseHandlingWindowConfig.Create<MainWindow>(),
            CloseHandlingWindowConfig.Create<UpdateWindow>(),
            CloseHandlingWindowConfig.Create<SettingsWindow>()
        );
        public override RepositoryService RepositoryService { get; protected set; }
        public override IRavenClient RavenClient { get; protected set; }
        public override SelfUpdaterService SelfUpdateService { get; protected set; }
        
        private TaskbarIcon _icon;

        private void CreateNotifyIcon()
        {
            _icon = new TaskbarIcon
            {
                IconSource = new BitmapImage(Constants.TaskbarIcon)
            };
            var menu = new ContextMenu();
            
            var openPkgMgrItem = new MenuItem { Header = Strings.OpenPackageManager };
            openPkgMgrItem.Click += (sender, args) => WindowService.Show<MainWindow>();
            menu.Items.Add(openPkgMgrItem);

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
            menu.Items.Add(updateItem);

            menu.Items.Add(new Separator());
            
            var exitItem = new MenuItem { Header = Strings.Exit };
            exitItem.Click += (sender, args) => Current.Shutdown();
            menu.Items.Add(exitItem);

            _icon.ContextMenu = menu;
            _icon.TrayMouseDoubleClick += (sender, args) => WindowService.Show<MainWindow>();
        }

        private void InitPackageStore()
        {
            PackageStore = new PackageStore(PackageService);
        }

        private void InitRepositoryService()
        {
            RepositoryService = new RepositoryService(RepositoryApi.Create, DispatcherScheduler.Current);
            RepositoryService.System
                .Where(x => x.RepoResult?.Error != null)
                .Select(x => x.RepoResult.Error)
                .DistinctUntilChanged()
                .Subscribe(error => MessageBox.Show(MainWindow,
                    string.Format(Strings.RepositoryErrorBody, error.Message),
                    Strings.RepositoryError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error));
        }

        private void InitUpdaterService()
        {
            UpdaterService = new UpdaterService(ConfigStore, RepositoryService, PackageService);
        }

        private void InitSelfUpdateService()
        {
            SelfUpdateService = new SelfUpdaterService(ConfigStore, PackageService, DispatcherScheduler.Current);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // The order of these initialisers is important.
            InitPackageStore();
            InitRepositoryService();
            InitSelfUpdateService();
            InitUpdaterService();
            
            CreateNotifyIcon();
            
            base.OnStartup(e);
            Console.WriteLine(Iso639.GetTag("kpv").Autonym);

            OnActivate(Environment.GetCommandLineArgs());
        }

        public override bool OnActivate(IList<string> args)
        {
            // If -s, run silently. Used for start-up service.
            if (!args.Contains(ArgsSilent))
            {
                WindowService.Show<MainWindow>();
            }
            
            // This return value has no purpose.
            return true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Stop Quartz from holding the app open for all time
            UpdaterService.Dispose();
            SelfUpdateService.Dispose();

            base.OnExit(e);
        }

        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void Main()
        {
            var key = Assembly.GetExecutingAssembly().GetGuid();

            if (!SingleInstance<BahkatApp>.InitializeAsFirstInstance(key))
            {
                return;
            }

            Native.RegisterApplicationRestart(ArgsSilent, 0);
            var raven = DI.CreateRavenClient();

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                raven.Capture(new SentryEvent((Exception) args.ExceptionObject));
            };

            var application = new BahkatApp {RavenClient = raven};
            application.Run();

            SingleInstance<BahkatApp>.Cleanup();
        }
    }
}
