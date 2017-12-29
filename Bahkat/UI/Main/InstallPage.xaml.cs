using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Service;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Main
{
    public interface IInstallPageView : IPageView
    {
        IObservable<EventArgs> OnCancelClicked();
        void SetCurrentPackage(OnStartPackageInfo info);
        void SetTotalPackages(long total);
        void ShowCompletion(bool isCancelled, ProcessResult[] results);
        void HandleError(Exception error);
        void ProcessCancelled();
    }

    /// <summary>
    /// Interaction logic for InstallPage.xaml
    /// </summary>
    public partial class InstallPage : Page, IInstallPageView, IDisposable
    {
        private InstallPagePresenter _presenter;
        private CompositeDisposable _bag = new CompositeDisposable();
        
        public InstallPage(PackageProcessInfo pkgProcessInfo)
        {
            InitializeComponent();
            
            _presenter = new InstallPagePresenter(
                this,
                pkgProcessInfo,
                new InstallService(),
                DispatcherScheduler.Current);
            
            _bag.Add(_presenter.Start());
        }

        public void SetTotalPackages(long total)
        {
            PrgBar.IsIndeterminate = false;
            PrgBar.Maximum = total;
        }

        public IObservable<EventArgs> OnCancelClicked() =>
            BtnCancel.ReactiveClick().Select(x => x.EventArgs);

        public void SetCurrentPackage(OnStartPackageInfo info)
        {
            var fmtString = info.Action == PackageAction.Install
                ? Strings.InstallingPackage
                : Strings.UninstallingPackage;
            LblPrimary.Text = string.Format(fmtString, info.Package.NativeName, info.Package.Version);
            LblSecondary.Text = string.Format(Strings.NItemsRemaining, info.Remaining);
            PrgBar.Value = info.Count;
        }

        public void ShowCompletion(bool isCancelled, ProcessResult[] results)
        {
            if (isCancelled)
            {
                this.ReplacePageWith(new MainPage());
                return;
            }
            
            var app = (BahkatApp) Application.Current;
            app.PackageStore.Dispatch(PackageStoreAction.ResetSelection);

            this.ReplacePageWith(new CompletionPage(results));
        }

        public void ProcessCancelled()
        {
            BtnCancel.IsEnabled = false;
            LblSecondary.Text = Strings.WaitingForCompletion;
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
