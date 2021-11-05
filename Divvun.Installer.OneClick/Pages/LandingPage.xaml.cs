using Divvun.Installer.OneClick.Models;
using Iterable;
using Newtonsoft.Json;
using Pahkat.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
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
using Serilog;

namespace Divvun.Installer.OneClick {

/// <summary>
/// Interaction logic for LandingPage.xaml
/// </summary>
public partial class LandingPage : Page {
    private ObservableCollection<LanguageItem> _dropDownData = new ObservableCollection<LanguageItem>();

    public LandingPage() {
        InitializeComponent();

        Languages.ItemsSource = _dropDownData;
    }

    private async Task<OneClickMeta> DownloadOneClickMetadata() {
        using var client = new WebClient();
        var jsonPayload = await client.DownloadStringTaskAsync(new Uri("https://pahkat.uit.no/main/oneclick.json"));
        return JsonConvert.DeserializeObject<OneClickMeta>(jsonPayload)!;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e) {
        var app = (App)Application.Current;
        try {
            app.Meta = await DownloadOneClickMetadata();
        }
        catch (Exception ex) {
            ((App)Application.Current).TerminateWithError(ex);
            return;
        }

        var items = app.Meta.Languages.Map((language) => new LanguageItem() {
            Tag = language.Tag,
            Name = Util.GetCultureDisplayName(language.Tag),
        }).ToList();
        items.Sort();

        foreach (var item in items) {
            _dropDownData.Add(item);
        }
    }

    private void InstallButton_OnClick(object sender, RoutedEventArgs args) {
        Log.Debug("Install button clicked");
        var app = (App)Application.Current;
        var meta = app.Meta;
        if (meta == null) {
            throw new Exception("The metadata necessary to download language files was not found.");
        }

        app.SelectedLanguage = Languages.SelectedItem as LanguageItem;
        if (app.SelectedLanguage == null) {
            throw new Exception("No language was selected for installation.");
        }

        var state = new TransactionState.InProgress {
            Actions = null,
            IsRebootRequired = false,
            State = new TransactionState.InProgress.TransactionProcessState.DownloadState {
                Progress = new ConcurrentDictionary<PackageKey, (long, long)>(),
            },
        };
        app.CurrentTransaction.OnNext(state);
    }

    private void Languages_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
        Log.Debug($"Changed language selection: {Languages.SelectedValue}");
        InstallButton.IsEnabled = true;
    }
}

}