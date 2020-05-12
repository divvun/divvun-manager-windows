using System;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using Divvun.Installer.Models;
using Divvun.Installer.UI.Shared;
using Divvun.Installer.Util;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Divvun.Installer.UI.Main
{
    
    public class PixelsToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new GridLength((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    enum Route
    {
        Landing,
        // Detailed,
        Download,
        Install,
        Completion,
        Error,
    }
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainWindowView
    {
        private CompositeDisposable bag = new CompositeDisposable();
        
        public MainWindow() {
            InitializeComponent();
        }
        
        void ShowLandingPage(Uri? url) {
            if (url != null && url.Scheme == "divvun-installer") {
                ShowPage(new MainPage());
            }
            else {
                ShowPage(new LandingPage());
            }
        }

        void ShowDownloadPage() {
            ShowPage(new DownloadPage());
        }

        void ShowInstallPage() {
            ShowPage(new InstallPage());
        }

        void ShowCompletionPage() {
            ShowPage(new CompletionPage());
        }

        void ShowErrorPage() {
            var app = (PahkatApp)Application.Current;
            app.CurrentTransaction.AsObservable()
                .Take(1)
                .Subscribe(state => {
                    var message = state.AsT2?.Message ?? "Unknown error";
                    MessageBox.Show(message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    app.CurrentTransaction.OnNext(new TransactionState.NotStarted());
                });
        }
        
        public void ShowPage(IPageView pageView) {
            DispatcherScheduler.Current.Schedule(() => FrmContainer.Navigate(pageView));
        }

        private IObservable<Route> Router() {
            var app = (PahkatApp)Application.Current;
            return app.CurrentTransaction.AsObservable()
                .DistinctUntilChanged()
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                .Select(evt => evt.Match(
                    notStarted => Route.Landing,
                    inProgress => inProgress.State.Match(
                        downloading => Route.Download,
                        installing => Route.Install,
                        complete => Route.Completion),
                    error => Route.Error)
                ).DistinctUntilChanged();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            var app = (PahkatApp) Application.Current;

            Router()
                .CombineLatest(app.Settings.SelectedRepository, (a, b) => (a, b))
                .Subscribe(tuple => {
                    switch (tuple.a) {
                        case Route.Landing:
                            ShowLandingPage(tuple.b);
                            break;
                    }
                }).DisposedBy(bag);
            
            Router()
                .Subscribe(route => {
                    switch (route)
                    {
                        case Route.Download:
                            ShowDownloadPage();
                            return;
                        case Route.Install:
                            ShowInstallPage();
                            return;
                        case Route.Completion:
                            ShowCompletionPage();
                            return;
                        case Route.Error:
                            ShowErrorPage();
                            return;
                    }
                })
                .DisposedBy(bag);
        }
    }
}