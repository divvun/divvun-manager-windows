using System.Windows.Controls;
using ModernWpf.Controls;

namespace Divvun.Installer.UI.Main.Dialog
{
    public partial class ConfirmationDialog : ContentDialog
    {
        public ConfirmationDialog(string titleText, string promptText, string? bodyText, string primaryButtonText) : base() {
            InitializeComponent();

            Title = titleText;
            PromptText.Text = promptText;
            if (bodyText != null) {
                BodyText.Text = bodyText;
            }
            PrimaryButtonText = primaryButtonText;
            CloseButtonText = Strings.Cancel;
        }
    }
}