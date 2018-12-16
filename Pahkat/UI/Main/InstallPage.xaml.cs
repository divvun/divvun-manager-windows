using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.Service;
using Pahkat.Service.CoreLib;
using Pahkat.UI.Shared;

namespace Pahkat.UI.Main
{
    public interface IInstallPageView : IPageView
    {
        IObservable<EventArgs> OnCancelClicked();
        void SetStarting(PackageActionType action, Package package);
        void SetEnding();
        void SetTotalPackages(long total);
        void ShowCompletion(bool isCancelled, bool requiresReboot);
        void HandleError(Exception error);
        void ProcessCancelled();
        void RequestAdmin(string urlListFile);
    }

    /// <summary>
    /// Interaction logic for InstallPage.xaml
    /// </summary>
    public partial class InstallPage : Page, IInstallPageView, IDisposable
    {
        private InstallPagePresenter _presenter;
        private CompositeDisposable _bag = new CompositeDisposable();

        public static InstallPage Create(string txActionsPath)
        {
            var str = File.ReadAllText(txActionsPath);
            var weakActions = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(str);
            var actions = weakActions.Select(TransactionAction.FromJson).ToArray();
            var transaction = ((IPahkatApp) Application.Current).Client.Transaction(actions);
            return new InstallPage(transaction);
        }

        public InstallPage(IPahkatTransaction transaction)
        {
            InitializeComponent();
            
            _presenter = new InstallPagePresenter(
                this,
                transaction,
                DispatcherScheduler.Current);
            
            _bag.Add(_presenter.Start());
        }

        public void SetTotalPackages(long total)
        {
            PrgBar.IsIndeterminate = false;
            PrgBar.Maximum = total;
        }

        public IObservable<EventArgs> OnCancelClicked() =>
            BtnCancel.ReactiveClick().Select(x => x.EventArgs);

        private void SetRemaining()
        {
            var max = PrgBar.Maximum;
            var value = PrgBar.Value;
            
            LblSecondary.Text = string.Format(Strings.NItemsRemaining, max - value);
        }

        public void SetStarting(PackageActionType action, Package package)
        {
            var fmtString = action == PackageActionType.Install
                ? Strings.InstallingPackage
                : Strings.UninstallingPackage;
            LblPrimary.Text = string.Format(fmtString, package.NativeName, package.Version);
            SetRemaining();
        }

        public void SetEnding()
        {
            PrgBar.Value += 1;
            SetRemaining();
        }

        public void ShowCompletion(bool isCancelled, bool requiresReboot)
        {
            var app = (PahkatApp)Application.Current;

            // Special case handling of app when in install mode
            if (app.Mode == AppMode.Install)
            {
                _presenter.SaveResultsState(new InstallSaveState
                {
                    IsCancelled = isCancelled,
                    RequiresReboot = requiresReboot
                });
                app.Shutdown();
                return;
            }

            if (isCancelled)
            {
                this.ReplacePageWith(new MainPage());
                return;
            }

            app.PackageStore.Dispatch(PackageStoreAction.ResetSelection);

            this.ReplacePageWith(new CompletionPage(requiresReboot));
        }

        public void ProcessCancelled()
        {
            BtnCancel.IsEnabled = false;
            LblSecondary.Text = Strings.WaitingForCompletion;
        }

        public void HandleError(Exception error)
        {
            var app = (PahkatApp)Application.Current;
            app.RavenClient.CaptureException(error);
            MessageBox.Show(error.Message,
                Strings.Error, 
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public void RequestAdmin(string urlListFile)
        {
            var app = (PahkatApp)Application.Current;
            var window = app.WindowService.Get<MainWindow>();
            var windowState = JsonConvert.SerializeObject(new WindowSaveState(window.Instance));
            app.WindowService.Hide<MainWindow>();

            var args = $"-i {urlListFile} -w {windowState}";
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var process = new Process
            {
                StartInfo =
                {
                    FileName = path,
                    Arguments = args,
                    Verb = "runas"
                }
            };

            try
            {
                process.Start();
                process.WaitForExit();
                var state = _presenter.ReadResultsState();
                ShowCompletion(state.IsCancelled, state.RequiresReboot);
            }
            catch (Win32Exception ex)
            {
                HandleError(ex);
                ShowCompletion(true, false);
            }
            finally
            {
                app.WindowService.Show<MainWindow>();
            }

        }

        public void Dispose()
        {
            _bag.Dispose();
        }
    }

    public struct WindowSaveState
    {
        [JsonProperty("h")]
        public double Height;
        [JsonProperty("w")]
        public double Width;
        [JsonProperty("x")]
        public double Left;
        [JsonProperty("y")]
        public double Top;
        [JsonProperty("s")]
        public WindowState WindowState;

        public WindowSaveState(Window window)
        {
            Height = window.ActualHeight;
            Width = window.ActualWidth;
            Left = window.Left;
            Top = window.Top;
            WindowState = window.WindowState;
        }
    }
}
