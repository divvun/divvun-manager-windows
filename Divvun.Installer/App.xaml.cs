using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using Divvun.Installer.Models;
using Divvun.Installer.Properties;
using Divvun.Installer.Service;
using Divvun.Installer.UI.About;
using Divvun.Installer.UI.Main;
using Divvun.Installer.UI.Settings;
using Divvun.Installer.Util;
using Hardcodet.Wpf.TaskbarNotification;
using Trustsoft.SingleInstanceApp;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Sentry;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.File.GZip;

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
                Log.Debug("-- New event: " + value);
                var newState = CurrentTransaction.Value.Reduce(value);
                CurrentTransaction.OnNext(newState);
            });
        }
        
        public Mutex<IPahkatClient> PackageStore { get; protected set; }
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
        
        protected override void OnStartup(StartupEventArgs e) {
            UserSelection = new UserPackageSelectionStore();
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

        private void ConfigureLogging() {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Divvun Installer",
                "log"
            );
            
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .MinimumLevel.Verbose()
                .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .WriteTo.File(Path.Combine(logPath, "app.log.gz"),
                    buffered: true,
                    fileSizeLimitBytes: 1024 * 1024 * 20,
                    retainedFileCountLimit: 3,
                    encoding: Encoding.UTF8,
                    hooks: new GZipHooks()
                )
                .WriteTo.Sentry(o => {
                    o.InitializeSdk = false;
                })
                .CreateLogger();
        }

        private void WriteClosed() {
            Log.Information("Application exited.");
        }
        
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int AttachParentProcess = -1;

        private void OnStartup(object sender, StartupEventArgs e) {
            const string key = "DivvunInstaller";
            AttachConsole(AttachParentProcess);

            Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));
            ConfigureLogging();
            
            Log.Information("Loading Divvun Installer v{version}",
                ThisAssembly.AssemblyInformationalVersion);
            
            if (!SingleInstance<PahkatApp>.InitializeAsFirstInstance(key)) {
                Log.Information("App already running; aborting.");
                Log.CloseAndFlush();
                return;
            }

            SentrySdk.Init(options => {
                options.Release = ThisAssembly.AssemblyInformationalVersion;
                options.Dsn = new Dsn(Constants.SentryDsn);
                options.SendDefaultPii = true;
            });
            
            Current.DispatcherUnhandledException += (o, args) => {
                Log.Fatal(args.Exception, "Unhandled exception in dispatcher");
                SentrySdk.CaptureException(args.Exception);
                PreExit();
                MessageBox.Show(args.Exception.Message, "Error");
                Current.Shutdown(1);
            };

            Settings = Settings.Create();
            
            // Set the UI language
            Settings.Mutate(file => {
                if (file.Language != null) {
                    Strings.Culture = Util.Util.GetCulture(file.Language);
                }
            });
            
            try {
                PackageStore = new Mutex<IPahkatClient>(PahkatClient.Create());
            }
            catch (Exception _) {
                MessageBox.Show(
                    "The RPC service was not found. If problems persist, try rebooting your computer.",
                    "Could not connect to Pahkat Client"
                );
                Current.Shutdown(1);
            }
        }
        
        private void PreExit() {
            SingleInstance<PahkatApp>.Cleanup();
            WriteClosed();
            Log.CloseAndFlush();
        }

        protected override void OnExit(ExitEventArgs e) {
            PreExit();
            base.OnExit(e);
        }
    }
}