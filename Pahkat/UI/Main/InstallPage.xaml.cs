using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.Service;
using Pahkat.UI.Shared;

namespace Pahkat.UI.Main
{
    public interface IInstallPageView : IPageView
    {
        IObservable<EventArgs> OnCancelClicked();
        void SetCurrentPackage(OnStartPackageInfo info);
        void SetTotalPackages(long total);
        void ShowCompletion(bool isCancelled, ProcessResult[] results);
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

        static public InstallPage Create(string installReqsPath)
        {
            var processInfo = JsonConvert.DeserializeObject<PackageProcessInfo>(
                File.ReadAllText(installReqsPath));
            return new InstallPage(processInfo);
        }

        public InstallPage(PackageProcessInfo pkgProcessInfo)
        {
            InitializeComponent();
            
            _presenter = new InstallPagePresenter(
                this,
                pkgProcessInfo,
                new InstallService(),
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

        public void SetCurrentPackage(OnStartPackageInfo info)
        {
            var fmtString = info.Action == PackageAction.Install
                ? Strings.InstallingPackage
                : Strings.UninstallingPackage;
            LblPrimary.Text = string.Format(fmtString, info.Package.NativeName, info.Package.Version);
            LblSecondary.Text = string.Format(Strings.NItemsRemaining, info.Remaining);
            PrgBar.Value = info.Count;
        }

        public void ShowCompletion(bool isCancelled, ProcessResult[] results)
        {
            var app = (PahkatApp)Application.Current;

            // Special case handling of app when in install mode
            if (app.Mode == AppMode.Install)
            {
                _presenter.SaveResultsState(new InstallSaveState
                {
                    IsCancelled = isCancelled,
                    Results = results
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

            this.ReplacePageWith(new CompletionPage(results));
        }

        public void ProcessCancelled()
        {
            BtnCancel.IsEnabled = false;
            LblSecondary.Text = Strings.WaitingForCompletion;
        }

        public void HandleError(Exception error)
        {
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
            var path = new Uri(System.Reflection.Assembly.GetEntryAssembly().CodeBase).AbsolutePath;
            var process = new Process
            {
                StartInfo =
                {
                    FileName = path,
                    Arguments = args,
                    Verb = "runas"
                }
            };

            process.Start();
            process.WaitForExit();
            var state = _presenter.ReadResultsState();

            ShowCompletion(state.IsCancelled, state.Results);

            app.WindowService.Show<MainWindow>();
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
