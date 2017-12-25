using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bahkat.Extensions;
using Bahkat.Service;
using Bahkat.UI.Shared;

namespace Bahkat.UI.Main
{
    public interface ICompletionPageView : IPageView
    {
        void ShowErrors(ProcessResult[] errors);
        void SetRequiresReboot();
        void ShowMain();
        void RebootSystem();
    }

    public class CompletionPagePresenter
    {
        private readonly ICompletionPageView _view;
        private readonly ProcessResult[] _results;
        
        public CompletionPagePresenter(ICompletionPageView view, ProcessResult[] results)
        {
            _view = view;
            _results = results;
        }

        public void Start()
        {
            var errors = _results.Where(r => !r.IsSuccess).ToArray();
            
            if (errors.Length > 0)
            {
                _view.ShowErrors(errors);
            }

            if (_results.Any(r => r.Package.Installer.RequiresReboot))
            {
                _view.SetRequiresReboot();
            }
        }
    }

    /// <summary>
    /// Interaction logic for CompletionPage.xaml
    /// </summary>
    public partial class CompletionPage : Page, ICompletionPageView
    {
        public CompletionPage(ProcessResult[] results)
        {
            InitializeComponent();
            
            var presenter = new CompletionPagePresenter(this, results);
            presenter.Start();

            BtnPrimary.ReactiveClick().Subscribe(_ => ShowMain());
        }

        public void ShowErrors(ProcessResult[] errors)
        {
            MessageBox.Show("There were errors.");
        }

        public void SetRequiresReboot()
        {
            //throw new NotImplementedException();
        }

        public void ShowMain()
        {
            this.ReplacePageWith(new MainPage());
        }

        public void RebootSystem()
        {
            // TODO this is crap, getting permission to use better code for this.
            Process.Start("shutdown.exe /r /t 0");
            
            Application.Current.Shutdown();
        }
    }
}
