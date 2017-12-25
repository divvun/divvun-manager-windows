using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.UI.Settings;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Main
{
    internal abstract class ModelFilter : IValueConverter
    {
        protected abstract object ProcessModel(Package model);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue == value ? value : ProcessModel((Package)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class GroupSelectionModel
    {
        dynamic _thing;

        GroupSelectionModel(object thing)
        {
            _thing = thing;
        }

        string Name => _thing.Name;
    }

    class CategoryFilter : ModelFilter
    {
        protected override object ProcessModel(Package model)
        {
            return model.Category;
        }
    }

    class LanguageFilter : ModelFilter
    {
        protected override object ProcessModel(Package model)
        {
            return model.Languages
                .Select(tag => new CultureInfo(tag).DisplayName)
                .ToList();
        }
    }

    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page, IMainPageView, IDisposable
    {
        private readonly MainPagePresenter _presenter;

        private readonly Subject<Package> _packageDeselectSubject = new Subject<Package>();
        private readonly Subject<Package> _packageSelectSubject = new Subject<Package>();
        private CompositeDisposable _bag = new CompositeDisposable();

        public IObservable<Package> OnPackageDeselected() => _packageDeselectSubject;
        public IObservable<Package> OnPackageSelected() => _packageSelectSubject;
        public IObservable<EventArgs> OnPrimaryButtonPressed() => BtnPrimary.ReactiveClick()
            .Select(e => e.EventArgs);
        
        public MainPage()
        {
            InitializeComponent();
            var app = (IBahkatApp)Application.Current;

            _presenter = new MainPagePresenter(this, 
                app.RepositoryService,
                app.PackageService,
                app.PackageStore);
            
            _bag.Add(_presenter.Start());
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

        public void SetPackagesModel(ObservableCollection<PackageMenuItem> model)
        {
            LvPackages.ItemsSource = model;
        }
        
        public void SetPackageFilter<T>() where T : IValueConverter, new()
        {
            var view = (CollectionView)CollectionViewSource.GetDefaultView(LvPackages.ItemsSource);
            view.GroupDescriptions.Clear();
            var groupDescription = new PropertyGroupDescription("Model", new T());
            view.GroupDescriptions.Add(groupDescription);
        }

        public void HandleError(Exception error)
        {
            MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void Dispose()
        {
            _packageDeselectSubject?.Dispose();
            _packageSelectSubject?.Dispose();
            _bag.Dispose();
        }
    }
}
