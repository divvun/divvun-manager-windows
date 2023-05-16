using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Divvun.Installer.Extensions;
using Divvun.Installer.UI.Shared;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.UI.Main {

public interface IMainPageView : IPageView {
    IObservable<PackageMenuItem> OnPackageToggled();
    IObservable<PackageCategoryTreeItem> OnGroupToggled();
    IObservable<EventArgs> OnPrimaryButtonPressed();
    void UpdateTitle(string title);

    void SetPackagesModel(ObservableCollection<RepoTreeItem> tree);

    // void ShowDownloadPage();
    void UpdatePrimaryButton(bool isEnabled, string label);
    void HandleError(Exception error);
}

internal enum SortBy {
    Language,
    Category,
}

/// <summary>
///     Interaction logic for MainPage.xaml
/// </summary>
public partial class MainPage : Page, IMainPageView, IDisposable {
    private readonly MainPagePresenter _presenter;

    private CompositeDisposable _bag = new CompositeDisposable();

    // Package handling events
    private IObservable<PackageCategoryTreeItem> _groupToggled;
    private IObservable<PackageMenuItem> _packageToggled;

    private readonly BehaviorSubject<SortBy> _sortedBy = new BehaviorSubject<SortBy>(SortBy.Language);

    public MainPage() {
        InitializeComponent();

        var app = (PahkatApp)Application.Current;
        _presenter = new MainPagePresenter(this, app.UserSelection);
    }

    public void Dispose() {
        _bag.Dispose();
    }

    // Package handling observables
    public IObservable<PackageCategoryTreeItem> OnGroupToggled() {
        return _groupToggled;
    }

    public IObservable<PackageMenuItem> OnPackageToggled() {
        return _packageToggled;
    }

    // Package search events
    public IObservable<EventArgs> OnPrimaryButtonPressed() {
        return BtnPrimary.ReactiveClick()
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .SubscribeOn(Dispatcher.CurrentDispatcher)
            .Map(e => e.EventArgs);
    }

    public void SetPackagesModel(ObservableCollection<RepoTreeItem> tree) {
        TvPackages.ItemsSource = tree;
    }

    public void UpdatePrimaryButton(bool isEnabled, string label) {
        BtnPrimary.Content = label;
        BtnPrimary.IsEnabled = isEnabled;
    }

    public void UpdateTitle(string title) {
        Title = title;
    }

    public void HandleError(Exception error) {
        if (Debugger.IsAttached) {
            throw error;
        }

        MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ConfigureSortBy() {
        var sortByLanguage = new MenuItem { Header = Strings.Language };
        sortByLanguage.Click += (sender, args) => { _sortedBy.OnNext(SortBy.Language); };

        var sortByCategory = new MenuItem { Header = Strings.Category };
        sortByCategory.Click += (sender, args) => { _sortedBy.OnNext(SortBy.Category); };

        TitleBarSortByFlyout.Items.Add(sortByCategory);
        TitleBarSortByFlyout.Items.Add(sortByLanguage);

        _sortedBy.Subscribe(value => {
            switch (value) {
            case SortBy.Language:
                TitleBarSortByButton.Content = $"{Strings.SortBy}: {Strings.Language}";
                break;
            case SortBy.Category:
                TitleBarSortByButton.Content = $"{Strings.SortBy}: {Strings.Category}";
                break;
            }

            PahkatApp.Current.Dispatcher.InvokeAsync(async () => await _presenter.BindNewRepositories(value));
        }).DisposedBy(_bag);
    }

    private void OnClickBtnMenu(object sender, RoutedEventArgs e) {
        if (BtnMenu.ContextMenu.IsOpen) {
            BtnMenu.ContextMenu.IsOpen = false;
            return;
        }

        BtnMenu.ContextMenu.Placement = PlacementMode.Bottom;
        BtnMenu.ContextMenu.PlacementTarget = BtnMenu;
        BtnMenu.ContextMenu.IsOpen = true;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e) {
        _bag = new CompositeDisposable();
        NavigationService.Navigating += NavigationService_Navigating;

        _packageToggled = TvPackages.ReactiveKeyDown()
            .Filter(x => x.EventArgs.Key == Key.Space)
            .Map(_ => Unit.Default).Merge(TvPackages.ReactiveDoubleClick()
                .Filter(x => {
                    var hitTest = TvPackages.InputHitTest(x.EventArgs.GetPosition((IInputElement)x.Sender));
                    return !(hitTest is Rectangle);
                })
                .Map(_ => Unit.Default))
            .Map(_ => TvPackages.SelectedItem as PackageMenuItem)
            .NotNull()!;

        _groupToggled =
            TvPackages.ReactiveKeyDown()
                .Filter(x => x.EventArgs.Key == Key.Space)
                .Map(_ => TvPackages.SelectedItem as PackageCategoryTreeItem)
                .NotNull()!;

        _presenter.Start().DisposedBy(_bag);

        TitleBarHandler.BindRepoDropdown(_bag, async x => {
            try {
                var app = (PahkatApp)Application.Current;
                var repos = (await app.PackageStore.RepoIndexes()).Values.ToArray();
                var records = await app.PackageStore.GetRepoRecords();

                TitleBarHandler.RefreshFlyoutItems(TitleBarReposButton, TitleBarReposFlyout, repos, records);
                // _presenter.BindNewRepositories(_sortedBy.Value);
            }
            catch (PahkatServiceConnectionException)
            {
                var current = (PahkatApp)Application.Current;

                if (!current.IsShutdown) {
                    current.IsShutdown = true;
                    MessageBox.Show(Strings.PahkatServiceConnectionException);
                    Application.Current.Dispatcher.Invoke(() =>
                        {
                            Application.Current.Shutdown(1);
                        }
                    );
                }
            }
        });

        ConfigureSortBy();

        TvPackages.Focus();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e) {
        if (NavigationService != null) {
            NavigationService.Navigating -= NavigationService_Navigating;
        }

        _bag.Dispose();
    }

    private void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e) {
        if (e.NavigationMode == NavigationMode.Back) {
            e.Cancel = true;
        }
    }

    private void OnClickAboutMenuItem(object sender, RoutedEventArgs e) {
        TitleBarHandler.OnClickAboutMenuItem(sender, e);
    }

    private void OnClickSettingsMenuItem(object sender, RoutedEventArgs e) {
        TitleBarHandler.OnClickSettingsMenuItem(sender, e);
    }

    private void OnClickExitMenuItem(object sender, RoutedEventArgs e) {
        TitleBarHandler.OnClickExitMenuItem(sender, e);
    }

    private void OnClickBundleLogsItem(object sender, RoutedEventArgs e) {
        TitleBarHandler.OnClickBundleLogsItem(sender, e);
    }
}

}