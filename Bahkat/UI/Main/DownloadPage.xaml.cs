using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Bahkat.Extensions;
using Bahkat.Service;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Main
{
    public interface IDownloadPageView : IPageView
    {
        void StartInstallation(PackagePath[] packages);
        void InitProgressList(ObservableCollection<DownloadListItem> source);
        IObservable<EventArgs> OnCancelClicked();
        void DownloadCancelled();
        void HandleError(Exception error);
    }

    /// <summary>
    /// Interaction logic for DownloadPage.xaml
    /// </summary>
    public partial class DownloadPage : Page, IDownloadPageView, IDisposable
    {
        private DownloadPagePresenter _presenter;
        private Subject<EventArgs> _cancelSubject = new Subject<EventArgs>();
        private CompositeDisposable _bag = new CompositeDisposable();
        
        public DownloadPage()
        {
            InitializeComponent();

            BtnCancel.IsCancel = true;
            
            var app = (IBahkatApp)Application.Current;

            _presenter = new DownloadPagePresenter(this, app.PackageStore, app.PackageService);
            _bag.Add(_presenter.Start());
        }

        public void InitProgressList(ObservableCollection<DownloadListItem> source)
        {
            LvPrimary.ItemsSource = source;
        }

        public void StartInstallation(PackagePath[] packages)
        {
            this.ReplacePageWith(new InstallPage(packages));
        }
        
        public void DownloadCancelled()
        {
            LvPrimary.ItemsSource = null;
            this.ReplacePageWith(new MainPage());
        }

        public IObservable<EventArgs> OnCancelClicked()
        {
            return _cancelSubject.AsObservable();
        }

        public void HandleError(Exception error)
        {
            MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(
                Strings.CancelDownloadsBody,
                Strings.CancelDownloadsTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res != MessageBoxResult.Yes)
            {
                return;
            }
            
            BtnCancel.IsEnabled = false;
            _cancelSubject.OnNext(e);
        }

        public void Dispose()
        {
            _bag.Dispose();
        }
    }
}
