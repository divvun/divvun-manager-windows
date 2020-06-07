using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Divvun.Installer.UI.Shared;
using Divvun.Installer.Util;
using Divvun.Installer.Extensions;
using Divvun.Installer.UI.Main.Dialog;
using ModernWpf.Controls;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.UI.Settings
{
    public interface ISettingsWindowView : IWindowView
    {
        // IObservable<EventArgs> OnSaveClicked();
        // IObservable<EventArgs> OnCancelClicked();
        // IObservable<EventArgs> OnRepoAddClicked();
        // IObservable<int> OnRepoRemoveClicked();
        // void SelectLastRow();
        // SettingsFormData SettingsFormData();
        // void HandleError(Exception error);
        // void Close();
    }

    public struct SettingsFormData
    {
        public string InterfaceLanguage;
        public LoadedRepository[] Repositories;
    }

    struct LanguageTag
    {
        public string Name { get; set; }
        public string Tag { get; set; }
    }

    public struct ChannelMenuItem
    {
        public string Name { get; set; }
        public string Value { get; set; }

        internal static ChannelMenuItem Create(string name, string value) {
            return new ChannelMenuItem {
                Name = name,
                Value = value
            };
        }
    }

    public class RepositoryListItem
    {
        public Uri Url { get; set; }
        public string Name { get; set; }
        public string Channel { get; set; }
        public List<ChannelMenuItem> Channels { get; set; }
        
        public RepositoryListItem(Uri url, string name, List<ChannelMenuItem> channels, string channel) {
            Url = url;
            Name = name;
            Channel = channel;
            Channels = channels;
        }
    }

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, ISettingsWindowView
    {
        // private readonly SettingsWindowPresenter _presenter;
        private CompositeDisposable _bag = new CompositeDisposable();
        
        public ObservableCollection<RepositoryListItem> RepoList { get; set; }
            = new ObservableCollection<RepositoryListItem>();

        private LanguageTag LanguageTag(string tag) {
            var data = Iso639.GetTag(tag);
            var simplestTag = data.Tag1 ?? data.Tag3;
            var name = data.Autonym ?? data.Name;
            return new LanguageTag {Name = name, Tag = simplestTag};
        }

        public SettingsWindow() {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            var app = (PahkatApp) Application.Current;
            
            RepoListView.ItemsSource = RepoList;

            DdlLanguage.ItemsSource = new ObservableCollection<LanguageTag> {
                new LanguageTag {Name = "System Default", Tag = ""},
                LanguageTag("en"),
                LanguageTag("nb"),
                LanguageTag("nn"),
                new LanguageTag {Name = "ᚿᛦᚿᚮᚱᛌᚴ", Tag = "nn-Runr"},
                LanguageTag("se")
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
                    
                    using (var guard = app.PackageStore.Lock()) {
                        try {
                            guard.Value.SetRepo(url, new RepoRecord());
                        } catch (Exception e) {
                            MessageBox.Show(e.Message,
                                Strings.Error,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }

                    RefreshRepoTable();
                }
            };

            BtnRemoveRepo.Click += (o, args) => {
                if (RepoListView.SelectedIndex >= 0) {
                    var item = (RepositoryListItem) RepoListView.SelectedItem;
                    
                    using (var guard = app.PackageStore.Lock()) {
                        guard.Value.RemoveRepo(item.Url);
                    }

                    RefreshRepoTable();
                }
            };

            RefreshRepoTable();
        }

        void RefreshRepoTable() {
            var app = (PahkatApp) Application.Current;
            using var guard = app.PackageStore.Lock();
            
            var repos = guard.Value.RepoIndexes();
            var repoRecords = guard.Value.GetRepoRecords();
            var strings = guard.Value.Strings(app.Settings.GetLanguage() ?? "en");
            
            RepoList.Clear();
            
            foreach (var keyValuePair in repoRecords) {
                var name = keyValuePair.Key.AbsoluteUri;
                if (repos.TryGetValue(keyValuePair.Key, out var repo)) {
                    var n = repo.Index.NativeName();
                    if (n != null) {
                        name = n;
                    }
                }
                else {
                    name += " ⚠️";
                }

                // TODO: do not hardcode channel list
                var channels = new List<ChannelMenuItem>();
                channels.Add(ChannelMenuItem.Create(Strings.Stable, ""));
                channels.Add(ChannelMenuItem.Create(Strings.Nightly, "nightly"));
                var selectedChannel = keyValuePair.Value.Channel ?? "";

                RepoList.Add(new RepositoryListItem(keyValuePair.Key, name, channels, selectedChannel));
            }
        }

        public IObservable<EventArgs> OnRepoAddClicked() =>
            BtnAddRepo.ReactiveClick().Select(x => x.EventArgs);

        private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0) {
                var app = (PahkatApp) Application.Current;
                using var guard = app.PackageStore.Lock();
                var item = (LanguageTag) e.AddedItems[0];
                app.Settings.Mutate(x => {
                    if (item.Tag == "") {
                        x.Language = null;
                    } else {
                        x.Language = item.Tag;
                    }
                });
                
            }
        }

        private void OnChannelSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var combo = (ComboBox) sender;
            var item = (RepositoryListItem) RepoListView.SelectedItem;
            
            var app = (PahkatApp) Application.Current;
            using var guard = app.PackageStore.Lock();
            var repoRecords = guard.Value.GetRepoRecords();
            var newRepoRecords = new Dictionary<Uri, RepoRecord>();
            
            foreach (var repositoryListItem in RepoList) {
                var url = repositoryListItem.Url;
                var channel = repositoryListItem.Channel;

                newRepoRecords[url] = new RepoRecord() {
                    Channel = channel == "" ? null : channel
                };
            }
            
            foreach (var key in newRepoRecords.Keys) {
                if (newRepoRecords[key] != repoRecords[key]) {
                    var value = newRepoRecords[key];
                    // Work around protobuf null hatred
                    value.Channel ??= "";
                    
                    guard.Value.SetRepo(key, newRepoRecords[key]);
                }
            }
        }
    }
}