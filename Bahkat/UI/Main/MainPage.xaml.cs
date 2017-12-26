using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Bahkat.Extensions;
using Bahkat.UI.Settings;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Main
{
    public interface IMainPageView : IPageView
    {
        IObservable<PackageMenuItem> OnPackageToggled();
        IObservable<PackageCategoryTreeItem> OnGroupToggled();
        IObservable<EventArgs> OnPrimaryButtonPressed();
        void UpdateTitle(string title);
        void SetPackagesModel(ObservableCollection<PackageCategoryTreeItem> tree);
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

        public IObservable<PackageMenuItem> OnPackageToggled() => _packageToggled;
        public IObservable<PackageCategoryTreeItem> OnGroupToggled() => _groupToggled;
        public IObservable<EventArgs> OnPrimaryButtonPressed() => BtnPrimary.ReactiveClick()
            .Select(e => e.EventArgs);
        
        public MainPage()
        {
            InitializeComponent();

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

            var app = (IBahkatApp)Application.Current;
            _presenter = new MainPagePresenter(this, 
                app.RepositoryService,
                app.PackageService,
                app.PackageStore);
            
            _bag.Add(_presenter.Start());

            TvPackages.Focus();
        }
        
        private void OnClickSettingsMenuItem(object sender, RoutedEventArgs e)
        {
            var app = (IBahkatApp)Application.Current;
            app.WindowService.Show<SettingsWindow>();
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

        public void SetPackagesModel(ObservableCollection<PackageCategoryTreeItem> tree)
        {
            TvPackages.ItemsSource = tree;
        }

        public void ShowDownloadPage()
        {
            this.ReplacePageWith(new DownloadPage());
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
    }
}
