using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
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
using System.Security;
using Castle.Core.Internal;
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
using Pahkat.Sdk;
using Pahkat.UI.About;
using Pahkat.UI.SelfUpdate;
using SharpRaven.Data.Context;

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
        PahkatClient Client { get; }
    }

    public abstract class AbstractPahkatApp : Application, IPahkatApp, ISingleInstanceApp
    {
        public abstract AppConfigStore ConfigStore { get; protected set; }
        //public abstract RepositoryService RepositoryService { get; protected set; }
        public abstract IPackageService PackageService { get; protected set;  }
        public abstract UpdaterService UpdaterService { get; protected set; }
        public abstract IWindowService WindowService { get; protected set;  }
        public abstract IPackageStore PackageStore { get; protected set; }
        public abstract IRavenClient RavenClient { get; protected set; }
        public abstract PahkatClient Client { get; protected set; }
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
        internal static AppConfigStore CreateAppConfigStore(IPahkatApp app)
        {
#if DEBUG
            return new AppConfigStore(app);
#else
            return new AppConfigStore(app);
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
        public const string ArgsSkipSelfUpdate = "-n";
        public const string ArgsSelfUpdate = "-u";

        public override PahkatClient Client { protected set; get; }
        public override AppConfigStore ConfigStore { protected set; get; } 
        public override IPackageStore PackageStore { get; protected set; }
        public override UpdaterService UpdaterService { get; protected set; }
        public override IWindowService WindowService { get; protected set; } = Service.WindowService.Create(
            CloseHandlingWindowConfig.Create<MainWindow>(),
            CloseHandlingWindowConfig.Create<UpdateWindow>(),
            CloseHandlingWindowConfig.Create<SettingsWindow>(),
            CloseHandlingWindowConfig.Create<SelfUpdateWindow>(),
            CloseHandlingWindowConfig.Create<AboutWindow>()
        );
        public override IPackageService PackageService { get; protected set; } = DI.CreatePackageService();
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

        private void InitUpdaterService()
        {
            UpdaterService = new UpdaterService(ConfigStore);
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

        private void InitConfigStore()
        {
            ConfigStore = DI.CreateAppConfigStore(this);
        }

        public string CurrentVersion => Assembly.GetEntryAssembly()
            .GetSemanticVersion()
            .ToString();

        private void EnsureValidRepoConfig()
        {
            ConfigStore.State.Select((x) => x.Repositories)
                .Where((x) => x.IsNullOrEmpty())
                .Subscribe((_) =>
                {
                    var repos = new[]
                        {new RepoConfig(new Uri("https://pahkat.uit.no/repo/windows/"), RepositoryMeta.Channel.Stable)};
                    ConfigStore.Dispatch(AppConfigAction.SetRepositories(repos));
                    Client.Config.SetRepos(repos);
                    Client.RefreshRepos();
                }).DisposedBy(_bag);
        }

        public bool RunSelfUpdate()
        {
            var pipeServer = new NamedPipeServerStream(Constants.PackageId);
                        
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var workPath = Path.Combine(Path.GetTempPath(), $"pahkat-{Path.GetRandomFileName()}");
            Directory.CreateDirectory(workPath);
            var updaterPath = Path.Combine(workPath, "updater.exe");
            File.Copy(Path.Combine(basePath, "updater.exe"), updaterPath);
            File.Copy(Path.Combine(basePath, "pahkat_client.dll"), Path.Combine(workPath, "pahkat_client.dll"));
                        
            Process.Start(updaterPath, $"-c \"{basePath}\"");
            pipeServer.WaitForConnection();
            var reader = new StreamReader(pipeServer);
            var line = reader.ReadLine();
            pipeServer.Disconnect();
    
            if (line == "error")
            {
                MessageBox.Show("The updater had an error; loading Divvun Installer anyway.");
                return false;
            }
            else // if (line == "ready")
            {
                // Early return.
                return true;
            }
        }

        public bool SkipSelfUpdateOnLaunch { private set; get; }= false;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            //Rpc = new RpcService(RavenClient);

            // The order of these initialisers is important.
            InitConfigStore();
            InitPackageStore();
            InitStrings();
            
            EnsureValidRepoConfig();

            var args = Environment.GetCommandLineArgs();

            SkipSelfUpdateOnLaunch = args.Contains(ArgsSkipSelfUpdate);

            if (Mode == AppMode.Default)
            {
                var shouldSelfUpdateCheck = !SkipSelfUpdateOnLaunch &&
                                            Client.Config.GetUiSetting("LastVersion") == CurrentVersion;
                
                // Ensures first run doesn't lead to random updates.
                Client.Config.SetUiSetting("LastVersion", CurrentVersion);

                if (shouldSelfUpdateCheck)
                {
                    var selfUpdateClient = CheckForSelfUpdate();
                    if (selfUpdateClient != null)
                    {
                        if (RunSelfUpdate())
                        {
                            return;
                        }
                    }
                }
                
                InitUpdaterService();
                CreateNotifyIcon();
            }
            
            base.OnStartup(e);

            OnActivate(args);
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
            if (!args.Contains(ArgsSilent) && !WindowService.Get<SelfUpdateWindow>().Instance.IsVisible)
            {
                if (Mode == AppMode.Default && UpdaterService != null && UpdaterService.HasUpdates())
                {
                    return true;
                }
                WindowService.Show<MainWindow>();
            }
            
            // This return value has no purpose.
            return true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Stop Quartz from holding the app open for all time
            if (UpdaterService != null)
            {
                UpdaterService.Dispose();
                UpdaterService = null;
            }

            base.OnExit(e);
        }

        public PahkatClient CheckForSelfUpdate()
        {
//            SessionEnding += (sender, args) =>
//            {
//                UpdaterService.Dispose();
//                UpdaterService = null;
//            };
            
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var selfUpdateJsonPath = Path.Combine(basePath, "selfupdate.json");
            var overrideUpdateChannel = false;

            PahkatClient client;
            try
            {
                client = new PahkatClient(selfUpdateJsonPath, false);

                // determine if the user has overridden the the update
                // channel to use for self updates. If so we need to
                // reconfigure the pahkat client to use that channel instead
                overrideUpdateChannel = client.TrySwitchChannel(client.Config.GetUiSetting("selfUpdateChannel"));
            }
            catch (Exception e)
            {
                return null;
            }

            var repo = client.Repos().FirstOrDefault();
            if (repo == null)
            {
                return null;
            }

            var package = repo.Packages.Get(Constants.PackageId, null);
            if (package == null)
            {
                return null;
            }

            var status = repo.PackageStatus(repo.AbsoluteKeyFor(package)).Status;
            switch (status)
            {
                #if DEBUG
                case PackageStatus.NotInstalled:
                #endif
                case PackageStatus.RequiresUpdate:
                    if (overrideUpdateChannel)
                    {
                        var result = MessageBox.Show(
                            Strings.BetaUpdateQuestion,
                            Strings.BetaUpdateAvailable,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning,
                            MessageBoxResult.No
                        );
                        
                        if (result != MessageBoxResult.Yes)
                        {
                            return null;
                        }
                    }
                    return client;
                default:
                    return null;
            }
        }

        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void Main(string[] args)
        {
            var mode = args.Any((x) => x == ArgsInstall)
                ? AppMode.Install
                : AppMode.Default;
            
            if (mode == AppMode.Default)
            {
                const string key = "DivvunInstaller";

                if (!SingleInstance<PahkatApp>.InitializeAsFirstInstance(key))
                {
                    return;
                }
            }

            var raven = DI.CreateRavenClient();

            AppDomain.CurrentDomain.UnhandledException += (sender, sargs) =>
            {
                raven.Capture(new SentryEvent((Exception) sargs.ExceptionObject));
            };
            var client = new PahkatClient();
            var application = new PahkatApp {Client = client, RavenClient = raven, Mode = mode};
            application.Run();

            if (mode == AppMode.Default)
            {
                SingleInstance<PahkatApp>.Cleanup();
            }
        }
    }
}
