using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using ModernWpf.Controls;

namespace Divvun.Installer.UI.Main.Dialog
{
    public partial class ConfirmationDialog : ContentDialog
    {
        public ConfirmationDialog(
            string titleText,
            string promptText,
            string? bodyText,
            string primaryButtonText
        ) : base() {
            InitializeComponent();

            Title = titleText;
            PromptText.Text = promptText;
            if (bodyText != null) {
                BodyText.Text = bodyText;
            }
            PrimaryButtonText = primaryButtonText;
            CloseButtonText = Strings.Cancel;
        }
        
        public ConfirmationDialog(
            string titleText,
            string promptText,
            string? bodyText,
            string primaryButtonText,
            string? cancelButtonText
        ) : base() {
            InitializeComponent();

            Title = titleText;
            PromptText.Text = promptText;
            if (bodyText != null) {
                BodyText.Text = bodyText;
            }
            PrimaryButtonText = primaryButtonText;
            if (cancelButtonText != null) {
                CloseButtonText = cancelButtonText;
            }
        }

        public async Task<ContentDialogResult> ShowAsync(Dispatcher dispatcher) {
            return await await dispatcher.InvokeAsync(async () => await ShowAsync());
        }
    }
}