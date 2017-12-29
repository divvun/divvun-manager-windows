using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Bahkat.Extensions;
using Bahkat.Service;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Main
{
    public interface ICompletionPageView : IPageView
    {
        IObservable<EventArgs> OnRestartButtonClicked();
        IObservable<EventArgs> OnFinishButtonClicked();
        
        void ShowErrors(ProcessResult[] errors);
        void RequiresReboot(bool requiresReboot);
        void ShowMain();
        void RebootSystem();
    }

    /// <summary>
    /// Interaction logic for CompletionPage.xaml
    /// </summary>
    public partial class CompletionPage : Page, ICompletionPageView, IDisposable
    {
        private CompositeDisposable _bag = new CompositeDisposable();
        
        public CompletionPage(ProcessResult[] results)
        {
            InitializeComponent();
            
            var presenter = new CompletionPagePresenter(this, results);
            _bag.Add(presenter.Start());
        }

        public IObservable<EventArgs> OnRestartButtonClicked() =>
            BtnRestart.ReactiveClick().Select(x => x.EventArgs);

        public IObservable<EventArgs> OnFinishButtonClicked() => 
            BtnFinish.ReactiveClick().Select(x => x.EventArgs);

        public void ShowErrors(ProcessResult[] errors)
        {
            // TODO: add error handling when things go wrong.
            // MessageBox.Show("Some errors occurred while procs");
        }

        public void RequiresReboot(bool requiresReboot)
        {
            if (requiresReboot)
            {
                LblPrimary.Text = Strings.RestartRequiredTitle;
                LblSecondary.Text = Strings.RestartRequiredBody;
                
                DockPanel.SetDock(BtnFinish, Dock.Left);
                BtnFinish.Content = Strings.RestartLater;
                BtnRestart.Visibility = Visibility.Visible;
            }
            else
            {
                LblPrimary.Text = Strings.ProcessCompletedTitle;
                LblSecondary.Text = Strings.ProcessCompletedBody;
                
                BtnRestart.Visibility = Visibility.Collapsed;
                DockPanel.SetDock(BtnFinish, Dock.Right);
                BtnFinish.Content = Strings.Finish;
            }
        }

        public void ShowMain()
        {
            this.ReplacePageWith(new MainPage());
        }

        public void RebootSystem()
        {
            ShutdownExtensions.Reboot();
            Application.Current.Shutdown();
        }

        public void Dispose()
        {
            _bag?.Dispose();
        }
    }
}
