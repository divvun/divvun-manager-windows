using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Pahkat.Extensions;
using Pahkat.UI.Settings;
using Pahkat.UI.Shared;
using Pahkat.Util;
using Pahkat.UI.About;
using Pahkat.Sdk;
using System.Windows.Navigation;
using Pahkat.Service;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;

namespace Pahkat.UI.Main
{
    public interface IMainPageView : IPageView
    {
        IObservable<string> OnSearchTextChanged();
        IObservable<PackageMenuItem> OnPackageToggled();
        IObservable<PackageCategoryTreeItem> OnGroupToggled();
        IObservable<EventArgs> OnPrimaryButtonPressed();
        IObservable<RepositoryIndex[]> OnNewRepositories();
        void UpdateTitle(string title);
        void SetPackagesModel(ObservableCollection<RepoTreeItem> tree);
        void ShowDownloadPage();
        void UpdatePrimaryButton(bool isEnabled, string label);
        void HandleError(Exception error);
    }

    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page, IMainPageView, IDisposable
    {
        private readonly MainPagePresenter _presenter;
        private IObservable<PackageMenuItem> _packageToggled;
        private IObservable<PackageCategoryTreeItem> _groupToggled;
        private CompositeDisposable _bag = new CompositeDisposable();
        private NavigationService _navigationService;
        private ISubject<string> _searchTextChangedSubject = new BehaviorSubject<string>("");
        private IObservable<RepositoryIndex[]> _onNewRepositories;
        private ISubject<bool> _onForceRefreshClickedSubject = new Subject<bool>();

        public IObservable<string> OnSearchTextChanged() => _searchTextChangedSubject.AsObservable();
        public IObservable<PackageMenuItem> OnPackageToggled() => _packageToggled;
        public IObservable<PackageCategoryTreeItem> OnGroupToggled() => _groupToggled;
        public IObservable<EventArgs> OnPrimaryButtonPressed() => BtnPrimary.ReactiveClick()
            .Select(e => e.EventArgs);
        public IObservable<RepositoryIndex[]> OnNewRepositories() => _onNewRepositories;

        public MainPage()
        {
            InitializeComponent();
            
            var app = (PahkatApp)Application.Current;

            _presenter = new MainPagePresenter(this,
                app.UserSelection);
            
            _packageToggled = Observable.Merge(
                TvPackages.ReactiveKeyDown()
                    .Where(x => x.EventArgs.Key == Key.Space)
                    .Select(_ => Unit.Default),
                TvPackages.ReactiveDoubleClick()
                    .Where(x => {
                        var hitTest = TvPackages.InputHitTest(x.EventArgs.GetPosition((IInputElement) x.Sender));
                        return !(hitTest is System.Windows.Shapes.Rectangle);
                    })
                    .Select(_ => Unit.Default))
                .Select(_ => TvPackages.SelectedItem as PackageMenuItem)
                .NotNull();
            
            _groupToggled = 
                TvPackages.ReactiveKeyDown()
                .Where(x => x.EventArgs.Key == Key.Space)
                .Select(_ => TvPackages.SelectedItem as PackageCategoryTreeItem)
                .NotNull();

            var onConfigChanged = app.ConfigStore.State
                .Select(x => x.Repositories)
                .Select(async configs => await RequestRepos())
                .Switch()
                .ObserveOn(Dispatcher.CurrentDispatcher);

            var onForceRefreshClick = _onForceRefreshClickedSubject
                .AsObservable()
                .Select(async force => await ForceRequestRepos())
                .Switch()
                .ObserveOn(Dispatcher.CurrentDispatcher);

            _onNewRepositories = Observable.Merge(onConfigChanged, onForceRefreshClick);

            _presenter.Start().DisposedBy(_bag);

            TvPackages.Focus();
        }

        private async Task<RepositoryIndex[]> RequestRepos()
        {
            return await Task.Run(() =>
            {
                var app = (PahkatApp)Application.Current;
                app.PackageStore.RefreshRepos();
                return app.PackageStore.RepoIndexes();
            });
        }

        private async Task<RepositoryIndex[]> ForceRequestRepos()
        {
            return await Task.Run(() =>
            {
                var app = (PahkatApp)Application.Current;
                app.PackageStore.ForceRefreshRepos();
                return app.PackageStore.RepoIndexes();
            });
        }

        private void OnClickAboutMenuItem(object sender, RoutedEventArgs e)
        {
            var app = (PahkatApp)Application.Current;
            app.WindowService.Show<AboutWindow>();
        }

        private void OnClickSettingsMenuItem(object sender, RoutedEventArgs e)
        {
            var app = (PahkatApp)Application.Current;
            app.WindowService.Show<SettingsWindow>();
        }

        private void OnClickRefreshMenuItem(object sender, RoutedEventArgs e)
        {
            _onForceRefreshClickedSubject.OnNext(true);
        }

        private async void OnClickCheckForPackageUpdatesMenuItem(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnCheckForPackageUpdates.IsEnabled = false;
                var app = (PahkatApp)Application.Current;
                await Task.Run(() =>
                {
                    new UpdaterService(app.ConfigStore);
                });
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, Strings.CheckForUpdates, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnCheckForPackageUpdates.IsEnabled = true;
            }
        }

        private void OnClickExitMenuItem(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnClickBtnMenu(object sender, RoutedEventArgs e)
        {
            if (BtnMenu.ContextMenu.IsOpen) {
                BtnMenu.ContextMenu.IsOpen = false;
                return;
            }

            BtnMenu.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            BtnMenu.ContextMenu.PlacementTarget = BtnMenu;
            BtnMenu.ContextMenu.IsOpen = true;
        }

        public void SetPackagesModel(ObservableCollection<RepoTreeItem> tree)
        {
            TvPackages.ItemsSource = tree;
        }

        public void ShowDownloadPage()
        {
            this.ReplacePageWith(new DownloadPage(DownloadPagePresenter.Default));
        }

        public void UpdatePrimaryButton(bool isEnabled, string label)
        {
            BtnPrimary.Content = label;
            BtnPrimary.IsEnabled = isEnabled;
        }

        public void UpdateTitle(string title)
        {
            Title = title;
        }

        public void HandleError(Exception error)
        {
            MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void Dispose()
        {
            _bag.Dispose();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _navigationService = this.NavigationService;
            _navigationService.Navigating += NavigationService_Navigating;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigating -= NavigationService_Navigating;
        }

        private void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                e.Cancel = true;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: label goes on top, not this
            if (SearchTextBox.Text != Strings.Search)
            {
                _searchTextChangedSubject.OnNext(SearchTextBox.Text);
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == Strings.Search)
            {
                SearchTextBox.Text = string.Empty;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {            
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = Strings.Search;
            }
        }
    }
}
