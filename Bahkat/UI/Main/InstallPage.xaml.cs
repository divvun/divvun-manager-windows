using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
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
using Bahkat.Models;
using Bahkat.Service;
using Bahkat.Util;

namespace Bahkat.UI.Main
{
    /// <summary>
    /// Interaction logic for InstallPage.xaml
    /// </summary>
    public partial class InstallPage : Page, IInstallPageView, IDisposable
    {
        private InstallPagePresenter _presenter;
        private CompositeDisposable _bag = new CompositeDisposable();
        
        public InstallPage(PackagePath[] packages)
        {
            InitializeComponent();

            _presenter = new InstallPagePresenter(
                this,
                packages,
                new InstallService(),
                DispatcherScheduler.Current);
            
            _bag.Add(_presenter.Start());
        }

        public void SetCurrentPackage(Package package)
        {
            TxtPackageName.Text = package.NativeName;
        }

        public void ShowCompletion(ProcessResult[] results)
        {
            var app = (BahkatApp) Application.Current;
            app.PackageStore.Dispatch(PackageAction.ResetSelection);

            this.ReplacePageWith(new CompletionPage(results));
        }

        public void HandleError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _bag.Dispose();
        }
    }
}
