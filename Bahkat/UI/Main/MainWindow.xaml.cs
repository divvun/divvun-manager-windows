using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bahkat.UI.Main
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
        
        public MainWindow(IPageView startingPage)
        {
            InitializeComponent();
            var presenter = new MainWindowPresenter(this, DispatcherScheduler.Current);
            ShowPage(startingPage);
        }

        public void ShowMainPage()
        {
            ShowPage(new MainPage());
        }

        public void ShowPage(IPageView pageView)
        {
            FrmContainer.Navigate(pageView);
        }
    }
}
