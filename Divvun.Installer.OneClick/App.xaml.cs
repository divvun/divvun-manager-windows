using Divvun.Installer.OneClick.Models;
using Sentry;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Divvun.Installer.OneClick {

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
    private CancellationTokenSource? _runningTransaction;

    public BehaviorSubject<TransactionState> CurrentTransaction
        = new BehaviorSubject<TransactionState>(new TransactionState.NotStarted());

    public LanguageItem? SelectedLanguage;
    public OneClickMeta? Meta = null;

    public static Task RunProcess(string filePath, string args, CancellationToken token = default) {
        Log.Debug("Running process: {filePath} {args}", filePath, args);
        var source = new TaskCompletionSource<int>();

        var process = new Process() {
            StartInfo = new ProcessStartInfo {
                FileName = filePath,
                Arguments = args,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        process.Exited += (sender, args) => {
            source.SetResult(process.ExitCode);
            process.Dispose();
        };

        process.Start();
        return process.WaitForExitAsync(token);
    }

    public void TerminateWithError(Exception e) {
        if (Debugger.IsAttached) {
            throw e;
        }

        var msg = string.Format(Strings.ErrorText, e.Message);

        Log.Fatal(e, "Fatal error while running application");
        SentrySdk.CaptureException(e);

        MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);
    private const int AttachParentProcess = -1;
    
    private void OnStartup(object sender, StartupEventArgs e) {
        ConfigureLogging();
        AttachConsole(AttachParentProcess);

        if (!Debugger.IsAttached) {
            SentrySdk.Init(options => {
                options.Release = ThisAssembly.AssemblyInformationalVersion;
                options.Dsn = "https://30865d1cdb374a5a98fd20edf1050397@o157567.ingest.sentry.io/5656321";
            });

            Current.DispatcherUnhandledException += (o, args) => {
                Log.Fatal(args.Exception, "Unhandled exception in dispatcher");
                SentrySdk.CaptureException(args.Exception);
                MessageBox.Show(args.Exception.Message, "Error");
                Current.Shutdown(1);
            };
        }
        else {
            Log.Warning("RUNNING WITH DEBUGGER -- No Sentry and no uncaught exception handling.");
        }
    }

    private void ConfigureLogging() {
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithExceptionDetails()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .MinimumLevel.Verbose()
            .WriteTo.Console(
                theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3} {ThreadName}:{ThreadId}] {Message:lj} {NewLine}{Exception}"
            )
            .WriteTo.Sentry(o => { o.InitializeSdk = false; })
            .CreateLogger();
    }
}

}