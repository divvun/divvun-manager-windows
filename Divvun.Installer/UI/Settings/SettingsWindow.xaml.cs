using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Divvun.Installer.Extensions;
using Divvun.Installer.UI.Main.Dialog;
using Divvun.Installer.UI.Shared;
using Divvun.Installer.Util;
using ModernWpf.Controls;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Serilog;

namespace Divvun.Installer.UI.Settings {

public interface ISettingsWindowView : IWindowView {
    // IObservable<EventArgs> OnSaveClicked();
    // IObservable<EventArgs> OnCancelClicked();
    // IObservable<EventArgs> OnRepoAddClicked();
    // IObservable<int> OnRepoRemoveClicked();
    // void SelectLastRow();
    // SettingsFormData SettingsFormData();
    // void HandleError(Exception error);
    // void Close();
}

public struct SettingsFormData {
    public string InterfaceLanguage;
    public LoadedRepository[] Repositories;
}

internal struct LanguageTag {
    public string Name { get; set; }
    public string Tag { get; set; }
}

public struct ChannelMenuItem {
    public string Name { get; set; }
    public string Value { get; set; }

    internal static ChannelMenuItem Create(string name, string value) {
        return new ChannelMenuItem {
            Name = name,
            Value = value,
        };
    }
}

public class RepositoryListItem {
    public RepositoryListItem(Uri url, string name, List<ChannelMenuItem> channels, string channel) {
        Url = url;
        Name = name;
        Channel = channel;
        Channels = channels;
    }

    public Uri Url { get; set; }
    public string Name { get; set; }
    public string Channel { get; set; }
    public List<ChannelMenuItem> Channels { get; set; }
}

/// <summary>
///     Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window, ISettingsWindowView {
    // private readonly SettingsWindowPresenter _presenter;
    private CompositeDisposable _bag = new CompositeDisposable();

    public SettingsWindow() {
        InitializeComponent();
    }

    public ObservableCollection<RepositoryListItem> RepoList { get; set; }
        = new ObservableCollection<RepositoryListItem>();

    private LanguageTag LanguageTag(string tag) {
        var data = Iso639.GetTag(tag);
        var simplestTag = data?.Tag1 ?? data?.Tag3 ?? tag;
        var name = data?.Autonym ?? data?.Name ?? tag;
        return new LanguageTag { Name = name, Tag = simplestTag };
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        var app = (PahkatApp)Application.Current;

        RepoListView.ItemsSource = RepoList;

        DdlLanguage.ItemsSource = new ObservableCollection<LanguageTag> {
            new LanguageTag { Name = "System Default", Tag = "" },
            LanguageTag("en"),
            LanguageTag("nb"),
            LanguageTag("nn"),
            new LanguageTag { Name = "ᚿᛦᚿᚮᚱᛌᚴ", Tag = "nn-Runr" },
            LanguageTag("se"),
            LanguageTag("ru"),
        };

        DdlLanguage.SelectedValue = app.Settings.GetLanguage() ?? "";

        BtnAddRepo.Click += async (o, args) => {
            var dialog = new AddRepoDialog();
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary) {
                Uri url;
                try {
                    url = new Uri(dialog.RepositoryUrl.Text);
                }
                catch {
                    MessageBox.Show("Not a valid URL.",
                        Strings.Error,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                try {
                    await app.PackageStore.SetRepo(url, new RepoRecord());
                }
                catch (Exception e) {
                    MessageBox.Show(e.Message,
                        Strings.Error,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                await RefreshRepoTable();
            }
        };

        BtnRemoveRepo.Click += async (o, args) => {
            if (RepoListView.SelectedIndex >= 0) {
                var item = (RepositoryListItem)RepoListView.SelectedItem;
                await app.PackageStore.RemoveRepo(item.Url);
                await RefreshRepoTable();
            }
        };

#pragma warning disable 4014
        RefreshRepoTable();
#pragma warning restore 4014
    }

    private async Task RefreshRepoTable() {
        var app = (PahkatApp)Application.Current;

        try {
            var repos = await app.PackageStore.RepoIndexes();
            var repoRecords = await app.PackageStore.GetRepoRecords();
            var repoStrings = await app.PackageStore.Strings(app.Settings.GetLanguage() ?? "en");

            RepoList.Clear();

            foreach (var keyValuePair in repoRecords) {
                var name = keyValuePair.Key.AbsoluteUri;
                if (repos.TryGetValue(keyValuePair.Key, out var repo)) {
                    name = repo.Index.NativeName();
                }
                else {
                    name += " ⚠️";
                }

                var strings = repoStrings.Get(keyValuePair.Key);
            
                var channels = new List<ChannelMenuItem>();
                if (strings != null) {
                    var defaultString = strings.Channels.Get("default");

                    if (defaultString == null) {
                        channels.Add(ChannelMenuItem.Create(Strings.Stable, ""));
                    }
                    else {
                        channels.Add(ChannelMenuItem.Create(defaultString, ""));
                    }
                
                
                    foreach (var channelPair in strings.Channels) {
                        if (channelPair.Key == "default") {
                            continue;
                        }
                    
                        channels.Add(ChannelMenuItem.Create(channelPair.Value, channelPair.Key));
                    }
                }
                else {
                    channels.Add(ChannelMenuItem.Create(Strings.Stable, ""));
                }
            
                var selectedChannel = keyValuePair.Value.Channel ?? "";

                RepoList.Add(new RepositoryListItem(keyValuePair.Key, name, channels, selectedChannel));
            }
        }
        catch (PahkatServiceException ex)
        {
                switch (ex)
                {
                    case PahkatServiceConnectionException _:
                        MessageBox.Show(Strings.PahkatServiceConnectionException);
                        break;
                    case PahkatServiceNotRunningException _:
                        MessageBox.Show(Strings.PahkatServiceNotRunningException);
                        break;
                }
                Application.Current.Dispatcher.Invoke(
                () =>
                {
                    Application.Current.Shutdown(1);
                }
            );
        }
    }

    public IObservable<EventArgs> OnRepoAddClicked() {
        return BtnAddRepo.ReactiveClick().Map(x => x.EventArgs);
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (e.AddedItems.Count > 0) {
            var app = PahkatApp.Current;
            var item = (LanguageTag)e.AddedItems[0];
            app.Settings.Mutate(x => {
                if (item.Tag == "") {
                    x.Language = null;
                }
                else {
                    x.Language = item.Tag;
                }
            });
        }
    }

    private void OnChannelSelectionChanged(object sender, SelectionChangedEventArgs e) {
        var combo = (ComboBox)sender;
        var item = (RepositoryListItem)RepoListView.SelectedItem;

        Task.Run(async () => {
            var app = PahkatApp.Current;
            var repoRecords = await app.PackageStore.GetRepoRecords();
            var newRepoRecords = new Dictionary<Uri, RepoRecord>();

            foreach (var repositoryListItem in RepoList) {
                var url = repositoryListItem.Url;
                var channel = repositoryListItem.Channel;

                newRepoRecords[url] = new RepoRecord {
                    Channel = channel == "" ? null : channel,
                };
            }

            foreach (var key in newRepoRecords.Keys) {
                if (newRepoRecords[key] != repoRecords[key]) {
                    var value = newRepoRecords[key];
                    // Work around protobuf null hatred
                    value.Channel ??= "";

                    await app.PackageStore.SetRepo(key, newRepoRecords[key]);
                }
            }
        });
    }
}

}