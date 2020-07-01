using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Iterable;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Divvun.Installer.Extensions;
using Divvun.Installer.Util;
using System.Windows.Navigation;
using System.Reactive.Subjects;
using System.Windows.Threading;
using Divvun.Installer.UI.About;
using Divvun.Installer.UI.Settings;
using Divvun.Installer.UI.Shared;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.UI.Main
{
    public interface IMainPageView : IPageView
    {
        IObservable<PackageMenuItem> OnPackageToggled();
        IObservable<PackageCategoryTreeItem> OnGroupToggled();
        IObservable<EventArgs> OnPrimaryButtonPressed();
        void UpdateTitle(string title);
        void SetPackagesModel(ObservableCollection<RepoTreeItem> tree);
        // void ShowDownloadPage();
        void UpdatePrimaryButton(bool isEnabled, string label);
        void HandleError(Exception error);
    }

    enum SortBy
    {
        Language,
        Category
    }

    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page, IMainPageView, IDisposable
    {
        private readonly MainPagePresenter _presenter;
        
        private CompositeDisposable _bag = new CompositeDisposable();
        private NavigationService? _navigationService;
        
        // Package handling events
        private IObservable<PackageCategoryTreeItem> _groupToggled;
        private IObservable<PackageMenuItem> _packageToggled;
        
        // Package handling observables
        public IObservable<PackageCategoryTreeItem> OnGroupToggled() => _groupToggled;
        public IObservable<PackageMenuItem> OnPackageToggled() => _packageToggled;
        
        // Package search events
        public IObservable<EventArgs> OnPrimaryButtonPressed() => BtnPrimary.ReactiveClick()
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .SubscribeOn(Dispatcher.CurrentDispatcher)
            .Map(e => e.EventArgs);
        
        private BehaviorSubject<SortBy> _sortedBy = new BehaviorSubject<SortBy>(SortBy.Language);

        private void ConfigureSortBy() {
            var sortByLanguage = new MenuItem {Header = Strings.Language};
            sortByLanguage.Click += (sender, args) => {
                _sortedBy.OnNext(SortBy.Language);
            };
            
            var sortByCategory = new MenuItem {Header = Strings.Category};
            sortByCategory.Click += (sender, args) => {
                _sortedBy.OnNext(SortBy.Category);
            };

            TitleBarSortByFlyout.Items.Add(sortByCategory);
            TitleBarSortByFlyout.Items.Add(sortByLanguage);
            
            _sortedBy.Subscribe((value) => {
                switch (value) {
                    case SortBy.Language:
                        TitleBarSortByButton.Content = $"{Strings.SortBy}: {Strings.Language}";
                        break;
                    case SortBy.Category:
                        TitleBarSortByButton.Content = $"{Strings.SortBy}: {Strings.Category}";
                        break;
                }
                _presenter.BindNewRepositories(value);
            }).DisposedBy(_bag);
        }
        
        public MainPage() {
            InitializeComponent();
            
            var app = (PahkatApp) Application.Current;

            _presenter = new MainPagePresenter(this,
                app.UserSelection);
        }

        private void OnClickBtnMenu(object sender, RoutedEventArgs e) {
            if (BtnMenu.ContextMenu.IsOpen) {
                BtnMenu.ContextMenu.IsOpen = false;
                return;
            }

            BtnMenu.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            BtnMenu.ContextMenu.PlacementTarget = BtnMenu;
            BtnMenu.ContextMenu.IsOpen = true;
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
            MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void Dispose() {
            _bag.Dispose();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) {
            _bag = new CompositeDisposable();
            _navigationService = this.NavigationService;
            _navigationService.Navigating += NavigationService_Navigating;
            
            _packageToggled = Observable.Merge(
                    TvPackages.ReactiveKeyDown()
                        .Filter(x => x.EventArgs.Key == Key.Space)
                        .Map(_ => Unit.Default),
                    TvPackages.ReactiveDoubleClick()
                        .Filter(x => {
                            var hitTest = TvPackages.InputHitTest(x.EventArgs.GetPosition((IInputElement) x.Sender));
                            return !(hitTest is System.Windows.Shapes.Rectangle);
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
            
            TitleBarHandler.BindRepoDropdown(_bag, x => {
                var app = (PahkatApp) Application.Current;
                LoadedRepository[] repos;
                Dictionary<Uri, RepoRecord> records;
                
                using (var guard = app.PackageStore.Lock()) {
                    repos = guard.Value.RepoIndexes().Values.ToArray();
                    records = guard.Value.GetRepoRecords();
                }
                
                TitleBarHandler.RefreshFlyoutItems(TitleBarReposButton, TitleBarReposFlyout, repos, records);
                _presenter.BindNewRepositories(_sortedBy.Value);
            });

            ConfigureSortBy();

            TvPackages.Focus();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e) {
            if (_navigationService != null) {
                _navigationService.Navigating -= NavigationService_Navigating;
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
    }
}