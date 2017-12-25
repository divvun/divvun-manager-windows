using System.Windows;

namespace Bahkat.UI.Settings
{
    public interface ISettingsWindowView
    {
        void Show();
    }
    
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, ISettingsWindowView
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }
    }
}
