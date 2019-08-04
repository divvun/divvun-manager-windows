using Pahkat.UI.Main;
using Pahkat.UI.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Pahkat.UI.Main
{
    /// <summary>
    /// Interaction logic for SimpleMainPage.xaml
    /// </summary>
    public partial class SimpleMainPage : Page, IPageView
    {
        public SimpleMainPage()
        {
            InitializeComponent();
            webBrowser.Source = new Uri("https://google.com");
        }
    }
}
