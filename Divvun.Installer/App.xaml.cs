using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Reactive.Subjects;
using System.Threading;
using Divvun.Installer.Models;
using Divvun.Installer.Properties;
using Divvun.Installer.Service;
using Divvun.Installer.UI.About;
using Divvun.Installer.UI.Main;
using Divvun.Installer.UI.Settings;
using Divvun.Installer.Util;
using Hardcodet.Wpf.TaskbarNotification;
using SharpRaven;
using SharpRaven.Data;
using Trustsoft.SingleInstanceApp;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Serilog;

namespace Divvun.Installer
{
    
    public partial class PahkatApp : ISingleInstanceApp
    {
        public const string ArgsSilent = "-s";

        public IWindowService WindowService { get; protected set; } = Service.WindowService.Create(
            CloseHandlingWindowConfig.Create<MainWindow>(),
            CloseHandlingWindowConfig.Create<SettingsWindow>(),
            CloseHandlingWindowConfig.Create<AboutWindow>()
        );

        private CancellationTokenSource? _runningTransaction;
        public BehaviorSubject<TransactionState> CurrentTransaction
            = new BehaviorSubject<TransactionState>(new TransactionState.NotStarted());

        public void StartTransaction(PackageAction[] actions) {
            using var guard = PackageStore.Lock();
            _runningTransaction?.Cancel();
            
            _runningTransaction = guard.Value.ProcessTransaction(actions, value => {
                Console.WriteLine("-- New event: " + value);
                var newState = CurrentTransaction.Value.Reduce(value);
                CurrentTransaction.OnNext(newState);
            });
        }
                    

        public Mutex<IPahkatClient> PackageStore { get; protected set; }
        public IRavenClient SentryClient { get; protected set; }
        public UserPackageSelectionStore UserSelection { get; protected set; }
        public Settings Settings { get; protected set;  }

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

            CreateNotifyIcon();

            base.OnStartup(e);

            OnActivate(args);
        }

        public bool OnActivate(IList<string> args) {
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

        private void OnStartup(object sender, StartupEventArgs e) {
            const string key = "Divvun.Installer";
            
            AttachConsole(AttachParentProcess);

            Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .CreateLogger();
            

            if (!SingleInstance<PahkatApp>.InitializeAsFirstInstance(key)) {
                Log.Information("App already running; aborting.");
                Log.CloseAndFlush();
                return;
            }

            // Set up Sentry exception capturing
            var sentry = new RavenClient(Constants.SentryDsn);
            AppDomain.CurrentDomain.UnhandledException += (sender, sargs) => {
                sentry.Capture(new SentryEvent((Exception) sargs.ExceptionObject));
            };

            Settings = Settings.Create();
            
            // Set the UI language
            Settings.Mutate(file => {
                if (file.Language != null) {
                    Strings.Culture = Util.Util.GetCulture(file.Language);
                }
            });
            
            SentryClient = sentry;
            PackageStore = new Mutex<IPahkatClient>(PahkatClient.Create());
        }

        protected override void OnExit(ExitEventArgs e) {
            SingleInstance<PahkatApp>.Cleanup();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}