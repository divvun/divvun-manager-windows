using System.Diagnostics;
using System.Windows;

namespace Divvun.Installer.UI.About {

public partial class AboutWindow : Window {
    public AboutWindow() {
        InitializeComponent();

        LblAppName.Text = Strings.AppName;
        LblVersion.Text = ThisAssembly.AssemblyInformationalVersion;
        BtnWebsite.Content = "Divvun Website";
        BtnWebsite.Click += (sender, e) => { Process.Start("http://divvun.no"); };
    }
}

}