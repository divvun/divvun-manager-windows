using Sentry;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Divvun.Installer.OneClick
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            ConfigureLogging();

            if (!Debugger.IsAttached)
            {
                SentrySdk.Init(options => {
                    options.Release = ThisAssembly.AssemblyInformationalVersion;
                    options.Dsn = new Dsn("https://30865d1cdb374a5a98fd20edf1050397@o157567.ingest.sentry.io/5656321");
                });

                Current.DispatcherUnhandledException += (o, args) => {
                    Log.Fatal(args.Exception, "Unhandled exception in dispatcher");
                    SentrySdk.CaptureException(args.Exception);
                    MessageBox.Show(args.Exception.Message, "Error");
                    Current.Shutdown(1);
                };
            }
            else
            {
                Log.Warning("RUNNING WITH DEBUGGER -- No Sentry and no uncaught exception handling.");
            }
        }

        private void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .MinimumLevel.Verbose()
                .WriteTo.Console(
                    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3} {ThreadName}:{ThreadId}] {Message:lj} {NewLine}{Exception}"
                )
                .WriteTo.Sentry(o => {
                    o.InitializeSdk = false;
                })
                .CreateLogger();
        }
    }
}
