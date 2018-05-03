using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Linq;
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
using Newtonsoft.Json;

namespace Pahkat
{
    public interface IPahkatApp
    {
        AppConfigStore ConfigStore { get; }
        //RepositoryService RepositoryService { get; }
        IPackageService PackageService { get; }
        //UpdaterService UpdaterService { get; }
        IWindowService WindowService { get; }
        IPackageStore PackageStore { get; }
        IRavenClient RavenClient { get; }
        //SelfUpdaterService SelfUpdateService { get; }
    }

    public abstract class AbstractPahkatApp : Application, IPahkatApp, ISingleInstanceApp
    {
        public abstract AppConfigStore ConfigStore { get; }
        //public abstract RepositoryService RepositoryService { get; protected set; }
        public abstract IPackageService PackageService { get; }
        //public abstract UpdaterService UpdaterService { get; protected set; }
        public abstract IWindowService WindowService { get; }
        public abstract IPackageStore PackageStore { get; protected set; }
        public abstract IRavenClient RavenClient { get; protected set; }
        //public abstract SelfUpdaterService SelfUpdateService { get; protected set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // main window turn on, who set up us the bomb
            //ConfigStore.State
            //    .Select(s => s.RepositoryUrl)
            //    .Subscribe(RepositoryService.SetRepositoryUri);
        }

        public abstract bool OnActivate(IList<string> args);
    }

    static class DI
    {
        internal static AppConfigStore CreateAppConfigStore()
        {
#if DEBUG
            return new AppConfigStore();
#else
            return new AppConfigStore();
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

    public enum AppMode
    {
        Default,
        Install
    }

    public class PahkatApp : AbstractPahkatApp
    {
        public AppMode Mode { get; private set; } = AppMode.Default;

        public const string ArgsSilent = "-s";
        public const string ArgsInstall = "-i";
        public const string ArgsWindow = "-w";

        public override AppConfigStore ConfigStore { get; } = DI.CreateAppConfigStore();
        public override IPackageService PackageService { get; } = DI.CreatePackageService();
        public override IPackageStore PackageStore { get; protected set; }
        //public override UpdaterService UpdaterService { get; protected set; }
        public override IWindowService WindowService { get; } = Service.WindowService.Create(
            CloseHandlingWindowConfig.Create<MainWindow>(),
            CloseHandlingWindowConfig.Create<UpdateWindow>(),
            CloseHandlingWindowConfig.Create<SettingsWindow>()
        );
        //public override RepositoryService RepositoryService { get; protected set; }
        public override IRavenClient RavenClient { get; protected set; }
        //public override SelfUpdaterService SelfUpdateService { get; protected set; }

        private CompositeDisposable _bag = new CompositeDisposable();
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

            //var updateItem = new MenuItem { Header = Strings.CheckForUpdates };
            //updateItem.Click += (sender, args) => UpdaterService
            //    .CheckForUpdatesImmediately()
            //    .Where(x => x == false)
            //    .SubscribeOn(DispatcherScheduler.Current)
            //    .Subscribe(_ => MessageBox.Show(MainWindow,
            //        Strings.NoUpdatesBody,
            //        Strings.NoUpdatesTitle,
            //        MessageBoxButton.OK,
            //        MessageBoxImage.Information));
            //menu.Items.Add(updateItem);

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
            //RepositoryService = new RepositoryService(RepositoryApi.Create, DispatcherScheduler.Current);
            //RepositoryService.System
            //    .Where(x => x.RepoResult?.Error != null)
            //    .Select(x => x.RepoResult.Error)
            //    .DistinctUntilChanged()
            //    .Subscribe(error => MessageBox.Show(MainWindow,
            //        string.Format(Strings.RepositoryErrorBody, error.Message),
            //        Strings.RepositoryError,
            //        MessageBoxButton.OK,
            //        MessageBoxImage.Error));
        }

        private void InitUpdaterService()
        {
            //UpdaterService = new UpdaterService(ConfigStore, RepositoryService, PackageService);
        }

        private void InitSelfUpdateService()
        {
            //SelfUpdateService = new SelfUpdaterService(ConfigStore, PackageService, DispatcherScheduler.Current);
        }

        private void InitStrings()
        {
            ConfigStore.State.Select(x => x.InterfaceLanguage)
                .DistinctUntilChanged()
                .Subscribe(lang =>
                {
                    if (lang == null)
                    {
                        Strings.Culture = CultureInfo.CurrentCulture;
                    }
                    else
                    {
                        Strings.Culture = new CultureInfo(lang);
                    }
                }).DisposedBy(_bag);
        }

        public RpcService Rpc;

        protected override void OnStartup(StartupEventArgs e)
        {
            Rpc = new RpcService(RavenClient);

            // The order of these initialisers is important.
            InitPackageStore();
            InitStrings();

            if (Mode == AppMode.Default)
            {
                //InitRepositoryService();
                InitSelfUpdateService();
                InitUpdaterService();

                CreateNotifyIcon();
            }
            
            base.OnStartup(e);

            OnActivate(Environment.GetCommandLineArgs());
        }

        public override bool OnActivate(IList<string> args)
        {
            WindowSaveState? windowState = null;

            if (args.Contains(ArgsWindow))
            {
                var index = args.IndexOf(ArgsWindow) + 1;
                if (index >= args.Count)
                {
                    throw new Exception("Invalid command line arguments provided");
                }

                windowState = JsonConvert.DeserializeObject<WindowSaveState>(args[index]);
            }

            if (args.Contains(ArgsInstall))
            {
                var index = args.IndexOf(ArgsInstall) + 1;
                if (index >= args.Count)
                {
                    throw new Exception("Invalid command line arguments provided");
                }

                WindowService.Show<MainWindow>(InstallPage.Create(args[index]), windowState);
            }

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
            //UpdaterService.Dispose();
            //SelfUpdateService.Dispose();
            Rpc.Dispose();

            base.OnExit(e);
        }

        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void Main(string[] args)
        {
            var mode = args.Any((x) => x == ArgsInstall)
                ? AppMode.Install
                : AppMode.Default;
            
            if (mode == AppMode.Default) {
                var key = Assembly.GetExecutingAssembly().GetGuid();

                if (!SingleInstance<PahkatApp>.InitializeAsFirstInstance(key))
                {
                    return;
                }
            }

            Native.RegisterApplicationRestart(ArgsSilent, 0);
            var raven = DI.CreateRavenClient();

            AppDomain.CurrentDomain.UnhandledException += (sender, sargs) =>
            {
                raven.Capture(new SentryEvent((Exception) sargs.ExceptionObject));
            };

            var application = new PahkatApp {RavenClient = raven, Mode = mode};
            application.Run();

            if (mode == AppMode.Default)
            {
                SingleInstance<PahkatApp>.Cleanup();
            }
        }
    }
}
