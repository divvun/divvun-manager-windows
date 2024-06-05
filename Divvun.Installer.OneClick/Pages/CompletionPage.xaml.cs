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

namespace Divvun.Installer.OneClick {

/// <summary>
/// Interaction logic for CompletionPage.xaml
/// </summary>
public partial class CompletionPage : Page {
    public CompletionPage() {
        InitializeComponent();
        LblSecondary.Text = string.Format(Strings.FinishedSecondary, ((App)Application.Current).SelectedLanguage!.Name);
    }

    private void RebootButton_OnClick(object sender, RoutedEventArgs e) {
        ShutdownExtensions.Reboot();
    }

    private void RebootLaterButton_OnClick(object sender, RoutedEventArgs e) {
        Application.Current.MainWindow.Close();
    }
}

}