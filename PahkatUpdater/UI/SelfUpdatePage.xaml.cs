using Pahkat.Sdk;
using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;

namespace PahkatUpdater.UI
{
    interface ISelfUpdateView
    {
        void SetProgress(ulong downloaded, ulong total);
        void IndeterminateProgress();
        void SetSubtitle(string subtitle);
        void HandleError(Exception error);
    }

    public partial class SelfUpdatePage : Page, ISelfUpdateView
    {
        private SelfUpdatePresenter _presenter;
        private CompositeDisposable _bag = new CompositeDisposable();
        
        public SelfUpdatePage(PackageStore client, string installDir)
        {
            InitializeComponent();
            
            _presenter = new SelfUpdatePresenter(this, client, installDir);
            Loaded += (sender, args) => _presenter.Start(_bag);
        }
        
        public void SetProgress(ulong downloaded, ulong total)
        {
            PrgBar.IsIndeterminate = false;
            PrgBar.Minimum = 0;
            PrgBar.Maximum = total;
            PrgBar.Value = downloaded;
        }

        public void IndeterminateProgress()
        {
            PrgBar.IsIndeterminate = true;
        }

        public void SetSubtitle(string subtitle)
        {
            LblSecondary.Text = subtitle;
        }

        public void HandleError(Exception error)
        {
            MessageBox.Show(error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
