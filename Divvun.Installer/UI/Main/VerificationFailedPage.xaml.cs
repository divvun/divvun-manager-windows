using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Divvun.Installer.Extensions;
using Divvun.Installer.Models;
using Divvun.Installer.UI.Shared;

namespace Divvun.Installer.UI.Main {

public interface IVerificationPageView : IPageView {
    IObservable<EventArgs> OnFinishButtonClicked { get; }
}

/// <summary>
///     Interaction logic for VerificationFailedPage.xaml
/// </summary>
public partial class VerificationFailedPage : Page, IVerificationPageView {
    private readonly CompositeDisposable _bag = new CompositeDisposable();
    private NavigationService? _navigationService;

    public VerificationFailedPage() {
        InitializeComponent();
    }

    public IObservable<EventArgs> OnFinishButtonClicked =>
        BtnFinish.ReactiveClick().Map(x => x.EventArgs);

    private void Page_Loaded(object sender, RoutedEventArgs e) {
        _navigationService = NavigationService;
        _navigationService.Navigating += NavigationService_Navigating;

        // Set total packages from the information we have
        var app = (PahkatApp)Application.Current;

        // Bind the button
        OnFinishButtonClicked.Subscribe(args => {
            BtnFinish.IsEnabled = false;
            app.CurrentTransaction.OnNext(new TransactionState.NotStarted());
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

    public void Dispose() {
        _bag?.Dispose();
    }
}

}