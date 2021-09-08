using System;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Navigation;
using Castle.Core.Internal;
using Divvun.Installer.Extensions;
using Divvun.Installer.Models;
using Divvun.Installer.UI.Shared;
using Pahkat.Sdk;
using Serilog;

namespace Divvun.Installer.UI.Main {

public class PixelsToGridLengthConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return new GridLength((double)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}

internal enum Route {
    Landing,

    // Detailed,
    Download,
    Install,
    Completion,
    Error,
    VerificationFailed,
}

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IMainWindowView {
    private IPageView? _currentPage;
    private readonly CompositeDisposable bag = new CompositeDisposable();
    private readonly IObservable<Route> Router = MakeRouter();

    public MainWindow() {
        InitializeComponent();
    }

    public void ShowPage(IPageView pageView) {
        PahkatApp.Current.Dispatcher.Invoke(() => {
            ShowContent();
            FrmContainer.Navigate(pageView);
            _currentPage = pageView;

            JournalEntry page;
            while ((page = FrmContainer.RemoveBackEntry()) != null) {
                // page.
                Log.Verbose("Murdered a view. {page}", page);
                // Clean up everything
            }
        });
    }

    public void HideContent() {
        FrmContainer.Visibility = Visibility.Hidden;
        if (_currentPage is LandingPage page) {
            page.HideWebview();
        }
    }

    public void ShowContent() {
        FrmContainer.Visibility = Visibility.Visible;
        if (_currentPage is LandingPage page) {
            page.ShowWebview();
        }
    }

    private void ShowLandingPage(Uri? url) {
        if (url != null && url.Scheme == "divvun-installer") {
            ShowPage(new MainPage());
        }
        else {
            ShowPage(new LandingPage());
        }
    }

    private void ShowDownloadPage() {
        ShowPage(new DownloadPage());
    }

    private void ShowInstallPage() {
        ShowPage(new InstallPage());
    }

    private void ShowCompletionPage() {
        ShowPage(new CompletionPage());
    }

    private void ShowErrorPage() {
        var app = (PahkatApp)Application.Current;
        app.CurrentTransaction.AsObservable()
            .Take(1)
            .Subscribe(state => {
                var message = state.AsT2?.Message ?? "Unknown error";
                MessageBox.Show(message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                app.CurrentTransaction.OnNext(new TransactionState.NotStarted());
            });
    }

    private void ShowVerificationFailedPage() {
        ShowPage(new VerificationFailedPage());
    }

    private static IObservable<Route> MakeRouter() {
        var app = (PahkatApp)Application.Current;
        return app.CurrentTransaction.AsObservable()
            .DistinctUntilChanged()
            .ObserveOn(app.Dispatcher)
            .SubscribeOn(app.Dispatcher)
            .Map(evt => evt.Match(
                notStarted => Route.Landing,
                inProgress => inProgress.State.Match(
                    downloading => Route.Download,
                    installing => Route.Install,
                    complete => Route.Completion),
                error => Route.Error,
                verification => Route.VerificationFailed)
            )
            .DistinctUntilChanged();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
        var app = PahkatApp.Current;

        // Ensure there's always at least one repository.
        var packageStore = app.PackageStore;
        var notifications = packageStore.Notifications();
        notifications.Subscribe(value => { Log.Debug("Notification: {value}", value); }).DisposedBy(bag);

        Router
            .CombineLatest(app.Settings.SelectedRepository, (a, b) => (a, b))
            .Subscribe(tuple => {
                switch (tuple.a) {
                case Route.Landing:
                    ShowLandingPage(tuple.b);
                    break;
                }
            }).DisposedBy(bag);

        Router
            .Subscribe(route => {
                switch (route) {
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
                case Route.VerificationFailed:
                    ShowVerificationFailedPage();
                    return;
                }
            })
            .DisposedBy(bag);

        Task.Run(async () => {
            if ((await app.PackageStore.GetRepoRecords()).IsNullOrEmpty()) {
                await app.PackageStore.SetRepo(new Uri("https://pahkat.uit.no/main/"), new RepoRecord());
            }
        });
    }
}

}