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

    enum Router
    {
        Landing,
        Detailed,
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

        public void ShowMainPage() {
            // ShowPage(new MainPage());
            // }
            //
            // public void ShowLandingPage() {
                ShowPage(new LandingPage());
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

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            var app = (PahkatApp)Application.Current;

            // var indexes = app.PackageStore.RepoIndexes();
            // Console.WriteLine(indexes);
            
            app.CurrentTransaction.AsObservable()
                .DistinctUntilChanged()
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                .Select(evt => evt.Match(
                    notStarted => Router.Landing,
                    inProgress => inProgress.State.Match(
                        downloading => Router.Download,
                        installing => Router.Install,
                        complete => Router.Completion),
                    error => Router.Error)
                )
                .DistinctUntilChanged()
                .Subscribe(route => {
                    switch (route)
                    {
                        case Router.Landing:
                            ShowMainPage();
                            return;
                        case Router.Download:
                            ShowDownloadPage();
                            return;
                        case Router.Install:
                            ShowInstallPage();
                            return;
                        case Router.Completion:
                            ShowCompletionPage();
                            return;
                        case Router.Error:
                            ShowErrorPage();
                            return;
                    }
                })
                .DisposedBy(bag);
        }
    }
}