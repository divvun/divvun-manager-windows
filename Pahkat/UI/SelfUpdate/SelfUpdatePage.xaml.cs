using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using Pahkat.Sdk;
using Pahkat.UI.Main;
using Pahkat.UI.Shared;
using SharpRaven.Data.Context;

namespace Pahkat.UI.SelfUpdate
{
    interface ISelfUpdateView
    {
        void SetProgress(ulong downloaded, ulong total);
        void IndeterminateProgress();
        void SetSubtitle(string subtitle);
        void HandleError(Exception error);
    }

    public partial class SelfUpdatePage : Page, IPageView, ISelfUpdateView
    {
        private SelfUpdatePresenter _presenter;
        private CompositeDisposable _bag = new CompositeDisposable();
        
        public SelfUpdatePage(PahkatClient client, bool isInstalling = false)
        {
            InitializeComponent();
            
            _presenter = new SelfUpdatePresenter(this, client, isInstalling);
            _presenter.Start(_bag);
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
            MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);

            if (!Util.Util.IsAdministrator())
            {
                // Let's reopen the main window so we can at least get something done.
                var app = (IPahkatApp) Application.Current;
                app.WindowService.Show<MainWindow>();
                app.WindowService.Close<SelfUpdateWindow>();
            }
        }
    }
}
