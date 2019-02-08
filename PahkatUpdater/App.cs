using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Permissions;
using System.Windows;
using Pahkat.Sdk;
using PahkatUpdater.UI;

namespace PahkatUpdater
{
    public static class Constants
    {
        public const string PackageId = "divvun-installer-windows";
        public const string PahkatBinName = "DivvunInstaller.exe";
    }
    
    public class App : Application
    {
        public void ShowError(string message)
        {
            MessageBox.Show(message);
            
            if (clientWriter != null)
            {
                clientWriter.WriteLine("error");
                clientWriter.Flush();
            }

            //StartPahkat();
            //Shutdown();
        }

        public void StartPahkat()
        {
            Process.Start(Path.Combine(installDir, Constants.PahkatBinName), "-n");
            
        }

        private string installDir;
        public PahkatClient Client { private set; get; }
        public Window Window { private set; get; }
        
        NamedPipeClientStream clientStream;
        StreamWriter clientWriter;

        public void SendReady()
        {
            clientWriter.WriteLine("ready");
            clientWriter.Flush();
            clientStream = null;
            clientWriter = null;
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                clientStream = new NamedPipeClientStream(Constants.PackageId);
                clientWriter = new StreamWriter(clientStream);
                clientStream.Connect(1000);
            }
            catch (Exception ex)
            {
                clientStream = null;
                clientWriter = null;
                ShowError(ex.Message);
                return;
            }
            
            var args = Environment.GetCommandLineArgs().ToList();
            var index = args.IndexOf("-c");
            if (index < 0 || index + 1 >= args.Count)
            {
                ShowError("Invalid command passed to updater.");
                return;
            }

            installDir = args[index + 1];
            var selfUpdateConfig = Path.Combine(installDir, "selfupdate.json");

            if (!File.Exists(selfUpdateConfig))
            {
                ShowError("No self-update manifest found. Your Divvun Installer installation might be damaged.");
                return;
            }

            try
            {
                Client = new PahkatClient(selfUpdateConfig, false);
            } catch (Exception ex) {
                ShowError(ex.Message);
                return;
            }
            
            Window = new SelfUpdateWindow(Client, installDir);
            Window.Show();
        }

        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, sargs) =>
            {
                var ex = (Exception) sargs.ExceptionObject;
                MessageBox.Show(ex.Message);
            };
            var application = new App();
            application.Run();
        }
    }
}