using System;
using System.ComponentModel;
using System.Windows;
using Pahkat.Extensions;
using Pahkat.UI.Shared;

namespace Pahkat.UI.SelfUpdate
{
    /// <summary>
    /// Interaction logic for SelfUpdateWindow.xaml
    /// </summary>
    public partial class SelfUpdateWindow : Window, IWindowPageView
    {
        public SelfUpdateWindow()
        {
            InitializeComponent();
        }

        public void ShowPage(IPageView pageView)
        {
            FrmContainer.Navigate(pageView);
        }
    }
}
