using ModernWpf.Controls;

namespace Divvun.Installer.UI.Main.Dialog {

public partial class AddRepoDialog : ContentDialog {
    public AddRepoDialog() {
        InitializeComponent();

        Title = "Add New Repository";
        PromptText.Text = "Paste your repository URL into the field below.";
        PrimaryButtonText = Strings.Save;
        CloseButtonText = Strings.Cancel;
    }
}

}