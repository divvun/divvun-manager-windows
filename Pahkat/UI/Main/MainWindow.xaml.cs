using System.Reactive.Concurrency;
using System.Windows;
using Pahkat.UI.Shared;

namespace Pahkat.UI.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainWindowView
    {
        public MainWindow()
        {   
            InitializeComponent();
            var presenter = new MainWindowPresenter(this, DispatcherScheduler.Current);
            presenter.Start();
        }

        public void ShowMainPage()
        {
            ShowPage(new MainPage());
        }

        public void ShowLandingPage()
        {
            ShowPage(new LandingPage());
        }

        public void ShowPage(IPageView pageView)
        {
            FrmContainer.Navigate(pageView);
        }
    }
}
