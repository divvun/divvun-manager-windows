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

namespace Pahkat
{
    public abstract class AbstractPahkatApp : Application, ISingleInstanceApp
    {
        public abstract AppConfigStore ConfigStore { get; protected set; }
        public abstract UpdaterService UpdaterService { get; protected set; }
        public abstract IWindowService WindowService { get; protected set;  }
        public abstract PackageStore PackageStore { get; protected set; }
        public abstract IRavenClient SentryClient { get; protected set; }
        public abstract UserPackageSelectionStore UserSelection { get; protected set; }

        protected override void OnStartup(StartupEventArgs e)
        {
        }

        public abstract bool OnActivate(IList<string> args);
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

        public override AppConfigStore ConfigStore { protected set; get; }
        public override PackageStore PackageStore { get; protected set; }
        public override UpdaterService UpdaterService { get; protected set; }
        public override IWindowService WindowService { get; protected set; } = Service.WindowService.Create(
            CloseHandlingWindowConfig.Create<MainWindow>(),
            CloseHandlingWindowConfig.Create<UpdateWindow>(),
            CloseHandlingWindowConfig.Create<SettingsWindow>(),
            CloseHandlingWindowConfig.Create<AboutWindow>()
        );
        public override IRavenClient SentryClient { get; protected set; }
        public override UserPackageSelectionStore UserSelection { get; protected set; }

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
                        try
                        {
                            Strings.Culture = new CultureInfo(lang);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show($"Failed to set language to {lang}; falling back to {CultureInfo.CurrentCulture.Name}.");
                            Strings.Culture = CultureInfo.CurrentCulture;
                        }
                    }
                }).DisposedBy(_bag);
        }

        private void InitConfigStore()
        {
            ConfigStore = new AppConfigStore(this);
        }

        public string CurrentVersion => ThisAssembly.AssemblyInformationalVersion;

        private void EnsureValidRepoConfig()
        {
            ConfigStore.State.Select((x) => x.Repositories)
                .Where((x) => x.IsNullOrEmpty())
                .Subscribe((_) =>
                {
                    var repos = new List<RepoRecord>()
                    {
                        new RepoRecord(new Uri("https://pahkat.uit.no/repo/windows/"), RepositoryMeta.Channel.Stable)
                    };
                    ConfigStore.Dispatch(AppConfigAction.SetRepositories(repos));
                    PackageStore.Config().SetRepos(repos.ToList());
                    PackageStore.RefreshRepos();
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
    
            if (line == "error" || line == null)
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

        public bool SkipSelfUpdateOnLaunch { private set; get; } = false;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            // The order of these initialisers is important.
            PackageStore = PackageStore.Default();
            Pahkat.Sdk.Settings.EnableLogging();
            InitConfigStore();
            UserSelection = new UserPackageSelectionStore();
            InitStrings();
            EnsureValidRepoConfig();

            var args = Environment.GetCommandLineArgs();

            SkipSelfUpdateOnLaunch = args.Contains(ArgsSkipSelfUpdate);

            if (Mode == AppMode.Default)
            {
                var shouldSelfUpdateCheck = !SkipSelfUpdateOnLaunch &&
                                            PackageStore.Config().GetUiValueRaw("LastVersion") == CurrentVersion;
                
                // Ensures first run doesn't lead to random updates.
                PackageStore.Config().SetUiValue("LastVersion", CurrentVersion);

                if (shouldSelfUpdateCheck)
                {
                    var selfUpdateClient = CheckForSelfUpdate();
                    if (selfUpdateClient != null)
                    {
                        if (RunSelfUpdate())
                        {
                            selfUpdateClient.Config().SetUiValue("no.divvun.Pahkat.updatingTo", null);
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
            // TODO: review
            if (!args.Contains(ArgsSilent)) //  && !WindowService.Get<SelfUpdateWindow>().Instance.IsVisible)
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
        private bool TrySwitchChannel(PackageStore selfUpdateStore)
        {
            var appConfig = PackageStore.Config();
            var selfUpdateChannel = appConfig.GetUiValueRaw("selfUpdateChannel");
            if (Enum.TryParse<RepositoryMeta.Channel>(selfUpdateChannel, true, out var channel))
            {
                var config = selfUpdateStore.Config();
                config.SetRepos(new List<RepoRecord> { new RepoRecord(config.Repos().First().Url, channel) });
                selfUpdateStore.ForceRefreshRepos();
                return true;
            }
            return false;
        }

        public PackageStore CheckForSelfUpdate()
        {
            //            SessionEnding += (sender, args) =>
            //            {
            //                UpdaterService.Dispose();
            //                UpdaterService = null;
            //            };
            return null;
            
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var selfUpdateJsonPath = Path.Combine(basePath, "selfupdate.json");
            var overrideUpdateChannel = false;

            PackageStore selfUpdateStore;
            try
            {
                selfUpdateStore = PackageStore.NewForSelfUpdate(selfUpdateJsonPath);

                // determine if the user has overridden the the update
                // channel to use for self updates. If so we need to
                // reconfigure the pahkat client to use that channel instead
                overrideUpdateChannel = TrySwitchChannel(selfUpdateStore);
            }
            catch (Exception e)
            {
                return null;
            }

            var repo = selfUpdateStore.RepoIndexes().FirstOrDefault();
            if (repo == null)
            {
                return null;
            }

            Package package = repo.Packages.Get(Constants.PackageId, null);
            if (package == null)
            {
                return null;
            }

            if (!overrideUpdateChannel && !AssertSuccessfulUpdate(package.Version))
            {
                return null;
            }

            var key = repo.AbsoluteKeyFor(package);
            var status = selfUpdateStore.Status(key).Item1;
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
                    PackageStore.Config().SetUiValue("no.divvun.Pahkat.updatingTo", package.Version);
                    return selfUpdateStore;
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
            
            // Check if multiple instances of Divvun Installer are running before creating new app
            if (mode == AppMode.Default)
            {
                const string key = "DivvunInstaller";

                if (!SingleInstance<PahkatApp>.InitializeAsFirstInstance(key))
                {
                    return;
                }
            }

            // Set up Sentry exception capturing
            var sentry = new RavenClient(Constants.SentryDsn);
            AppDomain.CurrentDomain.UnhandledException += (sender, sargs) =>
            {
                sentry.Capture(new SentryEvent((Exception) sargs.ExceptionObject));
            };


            // Start app
            var application = new PahkatApp {SentryClient = sentry, Mode = mode};
            application.Run();

            if (mode == AppMode.Default)
            {
                SingleInstance<PahkatApp>.Cleanup();
            }
        }

        private bool AssertSuccessfulUpdate(string packageVersion)
        {
            var updatingTo = PackageStore.Config().GetUiValueRaw("no.divvun.Pahkat.updatingTo");

            if (!string.IsNullOrEmpty(updatingTo) && updatingTo == packageVersion)
            {
                var result = MessageBox.Show(
                    "It seems that the previous update attempt failed. If problems persist, please download the installer directly from the website. Would you like to go there now?",
                    "Update Failed",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("http://divvun.no/korrektur/oswide.html");
                }
            }

            PackageStore.Config().SetUiValue("no.divvun.Pahkat.updatingTo", null);
            return true;
        }
    }
}
