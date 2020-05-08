using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Divvun.Installer.UI.Shared;
using Divvun.Installer.Util;
using Divvun.Installer.Extensions;
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
            = new ObservableCollection<RepositoryListItem>(new [] {new RepositoryListItem(new Uri("https://x.brendan.so/"), "Hello", new List<ChannelMenuItem>(), "")}); 

        private LanguageTag LanguageTag(string tag) {
            var data = Iso639.GetTag(tag);
            var simplestTag = data.Tag1 ?? data.Tag3;
            var name = data.Autonym ?? data.Name;
            return new LanguageTag {Name = name, Tag = simplestTag};
        }

        public SettingsWindow() {
            InitializeComponent();

            DdlLanguage.ItemsSource = new ObservableCollection<LanguageTag> {
                new LanguageTag {Name = "System Default", Tag = ""},
                LanguageTag("en"),
                LanguageTag("nb"),
                LanguageTag("nn"),
                new LanguageTag {Name = "ᚿᛦᚿᚮᚱᛌᚴ", Tag = "nn-Runr"},
                LanguageTag("se")
            };
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            RefreshRepoTable();
        }

        void RefreshRepoTable() {
            var app = (PahkatApp) Application.Current;
            using var guard = app.PackageStore.Lock();
            
            var repos = guard.Value.RepoIndexes();
            var repoRecords = guard.Value.GetRepoRecords();
            // var strings = guard.Value.Strings(Strings.Culture.IetfLanguageTag);
            
            RepoList.Clear();
            
            foreach (var keyValuePair in repoRecords) {
                var name = keyValuePair.Key.AbsoluteUri;
                if (repos.TryGetValue(keyValuePair.Key, out var repo)) {
                    var n = repo.Index.NativeName();
                    if (n != null) {
                        name = n;
                    }
                }

                var channels = new List<ChannelMenuItem>();
                channels.Add(ChannelMenuItem.Create(Strings.Stable, ""));
                channels.Add(ChannelMenuItem.Create(Strings.Nightly, "nightly"));
                var selectedChannel = keyValuePair.Value.Channel ?? "";

                RepoList.Add(new RepositoryListItem(keyValuePair.Key, name, channels, selectedChannel));
            }
        }

        public IObservable<EventArgs> OnSaveClicked() =>
            BtnSave.ReactiveClick().Select(x => x.EventArgs);

        public IObservable<EventArgs> OnCancelClicked() =>
            BtnCancel.ReactiveClick().Select(x => x.EventArgs);

        public IObservable<EventArgs> OnRepoAddClicked() =>
            BtnAddRepo.ReactiveClick().Select(x => x.EventArgs);

        // public IObservable<int> OnRepoRemoveClicked() =>
        //     BtnRemoveRepo.ReactiveClick()
        //         .Where(_ => DgRepos.SelectedIndex > -1)
        //         .Select(_ => DgRepos.SelectedIndex);

        // public void SetRepoItemSource(ObservableCollection<RepoDataGridItem> repos) {
        //     
        // }
        //
        // public void SetInterfaceLanguage(string tag) {
        //     DdlLanguage.SelectedValue = tag;
        // }
        //
        // public SettingsFormData SettingsFormData() {
        //     return new SettingsFormData {
        //         InterfaceLanguage = (string) DdlLanguage.SelectedValue,
        //     };
        // }
        //
        public void HandleError(Exception error) {
            MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        //
        // public void SelectRow(int index) {
        //     // DgRepos.SelectedIndex = Math.Min(DgRepos.Items.Count - 1, index);
        // }
        //
        // public void SelectLastRow() {
        //     // DgRepos.SelectedIndex = DgRepos.Items.Count - 1;
        // }
    }
}