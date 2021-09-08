using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Divvun.Installer.Extensions;
using Divvun.Installer.Models;
using Divvun.Installer.UI.Shared;

namespace Divvun.Installer.UI.Main {

public interface ICompletionPageView : IPageView {
    IObservable<EventArgs> OnRestartButtonClicked { get; }
    IObservable<EventArgs> OnFinishButtonClicked { get; }

    void RequiresReboot(bool requiresReboot);
    void RebootSystem();
}

/// <summary>
///     Interaction logic for CompletionPage.xaml
/// </summary>
public partial class CompletionPage : Page, ICompletionPageView, IDisposable {
    private readonly CompositeDisposable _bag = new CompositeDisposable();
    private NavigationService? _navigationService;

    public CompletionPage() {
        InitializeComponent();
    }

    public IObservable<EventArgs> OnRestartButtonClicked =>
        BtnRestart.ReactiveClick().Map(x => x.EventArgs);

    public IObservable<EventArgs> OnFinishButtonClicked =>
        BtnFinish.ReactiveClick().Map(x => x.EventArgs);

    public void RequiresReboot(bool requiresReboot) {
        if (requiresReboot) {
            LblPrimary.Text = Strings.RestartRequiredTitle;
            LblSecondary.Text = Strings.RestartRequiredBody;

            DockPanel.SetDock(BtnFinish, Dock.Left);
            BtnFinish.Content = Strings.RestartLater;
            BtnRestart.Visibility = Visibility.Visible;
        }
        else {
            LblPrimary.Text = Strings.ProcessCompletedTitle;
            LblSecondary.Text = Strings.ProcessCompletedBody;

            BtnRestart.Visibility = Visibility.Collapsed;
            DockPanel.SetDock(BtnFinish, Dock.Right);
            BtnFinish.Content = Strings.Finish;
        }

        MainGrid.Visibility = Visibility.Visible;
    }

    public void RebootSystem() {
        ShutdownExtensions.Reboot();
        Application.Current.Shutdown();
    }

    public void Dispose() {
        _bag?.Dispose();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) {
        _navigationService = NavigationService;
        _navigationService.Navigating += NavigationService_Navigating;

        // Set total packages from the information we have
        var app = (PahkatApp)Application.Current;

        // Control the state of the current view
        app.CurrentTransaction.AsObservable()
            .ObserveOn(app.Dispatcher)
            .SubscribeOn(app.Dispatcher)
            .Subscribe(item => {
                var x = item.AsInProgress?.IsRebootRequired ?? false;
                RequiresReboot(x);
            })
            .DisposedBy(_bag);

        // Bind the buttons
        OnFinishButtonClicked.Subscribe(args => {
            BtnRestart.IsEnabled = false;
            BtnFinish.IsEnabled = false;
            app.CurrentTransaction.OnNext(new TransactionState.NotStarted());
        }).DisposedBy(_bag);

        OnRestartButtonClicked.Subscribe(args => {
            BtnRestart.IsEnabled = false;
            BtnFinish.IsEnabled = false;
            RebootSystem();
        }).DisposedBy(_bag);

        app.UserSelection.ResetSelection();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e) {
        if (_navigationService != null) {
            _navigationService.Navigating -= NavigationService_Navigating;
        }

        Dispose();
    }

    private void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e) {
        if (e.NavigationMode == NavigationMode.Back) {
            e.Cancel = true;
        }
    }
}

}