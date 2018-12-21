using System;
using System.ComponentModel;
using System.Windows;

namespace PahkatUpdater.UI
{
    /// <summary>
    /// Interaction logic for SelfUpdateWindow.xaml
    /// </summary>
    public partial class SelfUpdateWindow : Window
    {
        public SelfUpdateWindow(PahkatClient client, string installDir)
        {
            InitializeComponent();
            FrmContainer.Navigate(new SelfUpdatePage(client, installDir));
        }
    }
}
