using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Linq;
using Divvun.Installer.Models;
using Divvun.Installer.Properties;
using Divvun.Installer.Service;
using Divvun.Installer.UI.About;
using Divvun.Installer.UI.Main;
using Divvun.Installer.UI.Settings;
using Hardcodet.Wpf.TaskbarNotification;
using SharpRaven;
using SharpRaven.Data;
using Trustsoft.SingleInstanceApp;
using Newtonsoft.Json;
using Serilog;

namespace Divvun.Installer
{
    public abstract class AbstractPahkatApp : Application, ISingleInstanceApp
    {
        public abstract IWindowService WindowService { get; protected set; }

        // public abstract PackageStore PackageStore { get; protected set; }
        public abstract IRavenClient SentryClient { get; protected set; }
        public abstract UserPackageSelectionStore UserSelection { get; protected set; }

        protected override void OnStartup(StartupEventArgs e) { }

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

        // public override PackageStore PackageStore { get; protected set; }
        public override IWindowService WindowService { get; protected set; } = Service.WindowService.Create(
            CloseHandlingWindowConfig.Create<MainWindow>(),
            CloseHandlingWindowConfig.Create<SettingsWindow>(),
            CloseHandlingWindowConfig.Create<AboutWindow>()
        );

        public override IRavenClient SentryClient { get; protected set; }
        public override UserPackageSelectionStore UserSelection { get; protected set; }

        private CompositeDisposable _bag = new CompositeDisposable();
        private TaskbarIcon _icon;

        private void CreateNotifyIcon() {
            _icon = new TaskbarIcon {
                IconSource = new BitmapImage(Constants.TaskbarIcon)
            };
            var menu = new ContextMenu();

            var openPkgMgrItem = new MenuItem {Header = Strings.OpenPackageManager};
            openPkgMgrItem.Click += (sender, args) => WindowService.Show<MainWindow>();
            menu.Items.Add(openPkgMgrItem);
            menu.Items.Add(new Separator());

            var exitItem = new MenuItem {Header = Strings.Exit};
            exitItem.Click += (sender, args) => Current.Shutdown();
            menu.Items.Add(exitItem);

            _icon.ContextMenu = menu;
            _icon.TrayMouseDoubleClick += (sender, args) => WindowService.Show<MainWindow>();
        }

        private void InitStrings() {
            // ConfigStore.State.Select(x => x.InterfaceLanguage)
            //     .DistinctUntilChanged()
            //     .Subscribe(lang =>
            //     {
            //         if (lang == null)
            //         {
            //             Strings.Culture = CultureInfo.CurrentCulture;
            //         }
            //         else
            //         {
            //             try
            //             {
            //                 Strings.Culture = new CultureInfo(lang);
            //             }
            //             catch (Exception e)
            //             {
            //                 MessageBox.Show($"Failed to set language to {lang}; falling back to {CultureInfo.CurrentCulture.Name}.");
            //                 Strings.Culture = CultureInfo.CurrentCulture;
            //             }
            //         }
            //     }).DisposedBy(_bag);
        }

        private void InitConfigStore() {
            // ConfigStore = new AppConfigStore(this);
        }

        public string CurrentVersion => ThisAssembly.AssemblyInformationalVersion;

        // private void EnsureValidRepoConfig()
        // {
        //     ConfigStore.State.Select((x) => x.Repositories)
        //         .Where((x) => x.IsNullOrEmpty())
        //         .Subscribe((_) =>
        //         {
        //             var repos = new List<RepoRecord>()
        //             {
        //                 new RepoRecord(new Uri("https://pahkat.uit.no/repo/windows/"), RepositoryMeta.Channel.Stable)
        //             };
        //             ConfigStore.Dispatch(AppConfigAction.SetRepositories(repos));
        //             PackageStore.Config().SetRepos(repos.ToList());
        //             PackageStore.RefreshRepos();
        //         }).DisposedBy(_bag);
        // }

        protected override void OnStartup(StartupEventArgs e) {
            // The order of these initialisers is important.
            // PackageStore = PackageStore.Default();
            // Divvun.Installer.Sdk.Settings.EnableLogging();
            InitConfigStore();
            UserSelection = new UserPackageSelectionStore();
            InitStrings();

            var args = Environment.GetCommandLineArgs();

            if (Mode == AppMode.Default) {
                CreateNotifyIcon();
            }

            base.OnStartup(e);

            OnActivate(args);
        }

        public override bool OnActivate(IList<string> args) {
            WindowSaveState? windowState = null;

            if (args.Contains(ArgsWindow)) {
                var index = args.IndexOf(ArgsWindow) + 1;
                if (index >= args.Count) {
                    throw new Exception("Invalid command line arguments provided");
                }

                windowState = JsonConvert.DeserializeObject<WindowSaveState>(args[index]);
            }

            if (args.Contains(ArgsInstall)) {
                var index = args.IndexOf(ArgsInstall) + 1;
                if (index >= args.Count) {
                    throw new Exception("Invalid command line arguments provided");
                }

                WindowService.Show<MainWindow>(InstallPage.Create(args[index]), windowState);
            }

            // If -s, run silently. Used for start-up service.
            if (!args.Contains(ArgsSilent)) {
                WindowService.Show<MainWindow>();
            }

            // This return value has no purpose.
            return true;
        }

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);

        private const int AttachParentProcess = -1;

        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void Main(string[] args) {
            AttachConsole(AttachParentProcess);

            Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .CreateLogger();

            // Divvun.Installer.Sdk.Settings.SetLoggingCallback((level, message, module, path) =>
            // {
            //     if (level == 0)
            //     {
            //         // Zero means no logging
            //         return;
            //     }
            //
            //     if (level > 5)
            //     {
            //         level = 5;
            //     }
            //
            //     var serilogLevel = 5 - level;
            //     Log.Write((LogEventLevel)serilogLevel, "[{Module}] {Path} {Message}", module, path, message);
            // });

            var mode = args.Any((x) => x == ArgsInstall)
                ? AppMode.Install
                : AppMode.Default;

            // Check if multiple instances of Divvun Installer are running before creating new app
            if (mode == AppMode.Default) {
                const string key = "Divvun.Installer";

                if (!SingleInstance<PahkatApp>.InitializeAsFirstInstance(key)) {
                    Log.Information("App already running; aborting.");
                    Log.CloseAndFlush();
                    return;
                }
            }

            // Set up Sentry exception capturing
            var sentry = new RavenClient(Constants.SentryDsn);
            AppDomain.CurrentDomain.UnhandledException += (sender, sargs) => {
                sentry.Capture(new SentryEvent((Exception) sargs.ExceptionObject));
            };


            // Start app
            var application = new PahkatApp {SentryClient = sentry, Mode = mode};
            application.Run();

            if (mode == AppMode.Default) {
                SingleInstance<PahkatApp>.Cleanup();
            }

            Log.CloseAndFlush();
        }
    }
}