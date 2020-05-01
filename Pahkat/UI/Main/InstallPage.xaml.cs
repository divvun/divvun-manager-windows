using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.Sdk;
using Pahkat.UI.Shared;
using System.Windows.Navigation;

namespace Pahkat.UI.Main
{
    public interface IInstallPageView : IPageView
    {
        IObservable<EventArgs> OnCancelClicked();
        void SetStarting(PackageAction action, PackageKey packageKey, string name, string version);
        void SetEnding();
        void SetTotalPackages(long total);
        void ShowCompletion(bool isCancelled, bool requiresReboot);
        void HandleError(Exception error);
        void ProcessCancelled();
        void RequestAdmin(string jsonFilePath);
    }

    /// <summary>
    /// Interaction logic for InstallPage.xaml
    /// </summary>
    public partial class InstallPage : Page, IInstallPageView, IDisposable
    {
        private InstallPagePresenter _presenter;
        private CompositeDisposable _bag = new CompositeDisposable();
        private NavigationService _navigationService;

        // public InstallPage(Transaction transaction) {
        //     InitializeComponent();
        //
        //     _presenter = new InstallPagePresenter(
        //         this,
        //         transaction,
        //         DispatcherScheduler.Current);
        //
        //     this.Loaded += (sender, args) => _bag.Add(_presenter.Start());
        // }

        public void SetTotalPackages(long total) {
            PrgBar.IsIndeterminate = false;
            PrgBar.Maximum = total;
        }

        public IObservable<EventArgs> OnCancelClicked() =>
            BtnCancel.ReactiveClick().Select(x => x.EventArgs);

        private void SetRemaining() {
            var max = PrgBar.Maximum;
            var value = PrgBar.Value;

            LblSecondary.Text = string.Format(Strings.NItemsRemaining, max - value);
        }

        public void SetStarting(PackageAction action, PackageKey packageKey, string name, string version) {
            var fmtString = action == PackageAction.Install
                ? Strings.InstallingPackage
                : Strings.UninstallingPackage;
            LblPrimary.Text = string.Format(fmtString, name, version);
            SetRemaining();
        }

        public void SetEnding() {
            PrgBar.Value += 1;
            SetRemaining();
        }

        public void ShowCompletion(bool isCancelled, bool requiresReboot) {
            var app = (PahkatApp) Application.Current;

            // Special case handling of app when in install mode
            if (app.Mode == AppMode.Install) {
                app.Shutdown();
                return;
            }

            if (isCancelled) {
                this.ReplacePageWith(new MainPage());
                return;
            }

            app.UserSelection.Dispatch(UserSelectionAction.ResetSelection);

            this.ReplacePageWith(new CompletionPage(requiresReboot));
        }

        public void ProcessCancelled() {
            BtnCancel.IsEnabled = false;
            LblSecondary.Text = Strings.WaitingForCompletion;
        }

        public void HandleError(Exception error) {
            var app = (PahkatApp) Application.Current;
            app.SentryClient.CaptureException(error);
            MessageBox.Show(error.Message,
                Strings.Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public void RequestAdmin(string jsonFilePath) {
            // var app = (PahkatApp) Application.Current;
            // var window = app.WindowService.Get<MainWindow>();
            // var windowState = JsonConvert.SerializeObject(new WindowSaveState(window.Instance));
            // app.WindowService.Hide<MainWindow>();
            //
            // var args = $"-i \"{jsonFilePath}\" -w {windowState}";
            // var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            // var process = new Process {
            //     StartInfo = {
            //         FileName = path,
            //         Arguments = args,
            //         Verb = "runas"
            //     }
            // };
            //
            // try {
            //     process.Start();
            //
            //     while (!File.Exists(_presenter.ResultsPath)) {
            //         Thread.Sleep(500);
            //     }
            //
            //     var state = _presenter.ReadResultsState();
            //     ShowCompletion(state.IsCancelled, state.RequiresReboot);
            // }
            // catch (Win32Exception ex) {
            //     HandleError(ex);
            //     ShowCompletion(true, false);
            // }
            // finally {
            //     app.WindowService.Show<MainWindow>();
            // }
        }

        public void Dispose() {
            _bag.Dispose();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) {
            _navigationService = this.NavigationService;
            _navigationService.Navigating += NavigationService_Navigating;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e) {
            _navigationService.Navigating -= NavigationService_Navigating;
        }

        private void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e) {
            if (e.NavigationMode == NavigationMode.Back) {
                e.Cancel = true;
            }
        }
    }

    public struct WindowSaveState
    {
        [JsonProperty("h")] public double Height;
        [JsonProperty("w")] public double Width;
        [JsonProperty("x")] public double Left;
        [JsonProperty("y")] public double Top;
        [JsonProperty("s")] public WindowState WindowState;

        public WindowSaveState(Window window) {
            Height = window.ActualHeight;
            Width = window.ActualWidth;
            Left = window.Left;
            Top = window.Top;
            WindowState = window.WindowState;
        }
    }
}