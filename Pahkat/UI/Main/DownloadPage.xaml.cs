using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Pahkat.Extensions;
using Pahkat.Sdk;
using Pahkat.UI.Shared;

namespace Pahkat.UI.Main
{
    public interface IDownloadPageView : IPageView
    {
        // void StartInstallation(Transaction transaction);
        void InitProgressList(ObservableCollection<DownloadListItem> source);
        IObservable<EventArgs> OnCancelDialogOpen();
        IObservable<EventArgs> OnCancelClicked();
        IObservable<EventArgs> OnResumeClicked();
        void DownloadCancelled();
        void SetStatus(DownloadListItem item, DownloadProgress progress);
        void HandleError(Exception error);
    }

    /// <summary>
    /// Interaction logic for DownloadPage.xaml
    /// </summary>
    public partial class DownloadPage : Page, IDownloadPageView, IDisposable
    {
        private Subject<EventArgs> _cancelSubject = new Subject<EventArgs>();
        private Subject<EventArgs> _resumeSubject = new Subject<EventArgs>();
        private Subject<EventArgs> _cancelDialogOpenSubject = new Subject<EventArgs>();
        private CompositeDisposable _bag = new CompositeDisposable();
        private NavigationService? _navigationService;

        public DownloadPage(Func<IDownloadPageView, DownloadPagePresenter> presenter) {
            InitializeComponent();
            _bag.Add(presenter(this).Start());
        }

        // public void StartInstallation() {
        //     this.ReplacePageWith(new InstallPage(transaction));
        // }

        public void InitProgressList(ObservableCollection<DownloadListItem> source) {
            LvPrimary.ItemsSource = source;
        }

        public void DownloadCancelled() {
            BtnCancel.IsEnabled = false;
            LvPrimary.ItemsSource = null;
            this.ReplacePageWith(new MainPage());
        }

        public void SetStatus(DownloadListItem candidate, DownloadProgress progress) {
            if (LvPrimary.ItemsSource == null) {
                return;
            }

            var source = (ObservableCollection<DownloadListItem>) LvPrimary.ItemsSource;
            var item = source.First(candidate.Equals);

            Console.WriteLine($"{progress.Status} {progress.Downloaded} {progress.Total} {progress.PackageId}");

            switch (progress.Status) {
                case PackageDownloadStatus.Progress:
                    item.Downloaded = (long) progress.Downloaded;
                    break;
                case PackageDownloadStatus.Error:
                    item.Downloaded = -1;
                    break;
            }
        }

        public IObservable<EventArgs> OnCancelDialogOpen() {
            return _cancelDialogOpenSubject.AsObservable();
        }

        public IObservable<EventArgs> OnCancelClicked() {
            return _cancelSubject.AsObservable();
        }

        public IObservable<EventArgs> OnResumeClicked() {
            return _resumeSubject.AsObservable();
        }

        public void HandleError(Exception error) {
            MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            DownloadCancelled();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
            _cancelDialogOpenSubject.OnNext(e);

            var res = MessageBox.Show(
                Strings.CancelDownloadsBody,
                Strings.CancelDownloadsTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res != MessageBoxResult.Yes) {
                _resumeSubject.OnNext(e);
                return;
            }

            BtnCancel.IsEnabled = false;
            _cancelSubject.OnNext(e);
        }

        public void Dispose() {
            _bag.Dispose();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) {
            var svc = this.NavigationService;
            if (svc != null) {
                _navigationService = svc;
                svc.Navigating += NavigationService_Navigating;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e) {
            var svc = _navigationService;
            if (svc != null) {
                svc.Navigating -= NavigationService_Navigating;
            }
        }

        private void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e) {
            if (e.NavigationMode == NavigationMode.Back) {
                e.Cancel = true;
            }
        }
    }
}