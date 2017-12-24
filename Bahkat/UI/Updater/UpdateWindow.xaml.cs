using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows;
using Bahkat.Extensions;
using Bahkat.UI.Main;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Updater
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window, IUpdateWindowView
    {
        private IDisposable _disposable;
        
        public UpdateWindow()
        {
            InitializeComponent();
            var app = (BahkatApp) Application.Current;
            var presenter = new UpdateWindowPresenter(this,
                app.RepositoryService,
                app.PackageService,
                app.PackageStore);
            _disposable = presenter.Start();
        }

        public IObservable<EventArgs> OnInstallClicked() => BtnPrimary.ReactiveClick().Select(x => x.EventArgs);
        public IObservable<EventArgs> OnRemindMeLaterClicked() => BtnLater.ReactiveClick().Select(x => x.EventArgs);
        public IObservable<EventArgs> OnSkipClicked() => BtnSkip.ReactiveClick().Select(x => x.EventArgs);

        public void StartDownloading()
        {
            var app = (BahkatApp) Application.Current;
            app.WindowService.Close<UpdateWindow>();
            app.WindowService.Show<MainWindow, DownloadPage>();
        }

        public void UpdatePrimaryButton(bool isEnabled, string label)
        {
            BtnPrimary.Content = label;
            BtnPrimary.IsEnabled = isEnabled;
        }

        public void SetPackagesModel(ObservableCollection<PackageMenuItem> items)
        {
            LvPackages.ItemsSource = items;
        }

        public void HandleError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void CloseMainWindow()
        {
            var app = (BahkatApp) Application.Current;
            app.WindowService.Close<MainWindow>();
        }
    }
}
