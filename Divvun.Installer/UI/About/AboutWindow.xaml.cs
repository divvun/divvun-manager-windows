using System.Diagnostics;
using System.Windows;

namespace Divvun.Installer.UI.About {

public partial class AboutWindow : Window {
    public AboutWindow() {
        InitializeComponent();

        LblAppName.Text = Strings.AppName;
        LblVersion.Text = ThisAssembly.AssemblyInformationalVersion;
        BtnWebsite.Content = "Divvun Website";
        BtnWebsite.Click += (sender, e) => {
            var info = new ProcessStartInfo {
                FileName = "https://divvun.org/",
                UseShellExecute = true,
            };
            Process.Start(info);
            Close();
        };
    }
}

}