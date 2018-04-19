using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.UI.Shared;
using Pahkat.Util;

namespace Pahkat.UI.Settings
{
    public interface ISettingsWindowView : IWindowView
    {
        IObservable<EventArgs> OnSaveClicked();
        IObservable<EventArgs> OnCancelClicked();
        IObservable<EventArgs> OnRepoAddClicked();
        IObservable<int> OnRepoRemoveClicked();
        void SetInterfaceLanguage(string tag);
        void SetRepoItemSource(ObservableCollection<RepoDataGridItem> repos);
        void SetUpdateFrequency(PeriodInterval period);
        void SetUpdateFrequencyStatus(DateTimeOffset dateTime);
        void SelectRow(int index);
        void SelectLastRow();
        SettingsFormData SettingsFormData();
        void HandleError(Exception error);
        void Close();
    }

    public struct SettingsFormData
    {
        public string InterfaceLanguage;
        public PeriodInterval UpdateCheckInterval;
        public RepoConfig[] Repositories;
    }

    struct LanguageTag
    {
        public string Name { get; set; }
        public string Tag { get; set; }
    }

    struct PeriodIntervalMenuItem
    {
        public string Name { get; set; }
        public PeriodInterval Value { get; set; }

        internal static PeriodIntervalMenuItem Create(PeriodInterval period)
        {
            return new PeriodIntervalMenuItem()
            {
                Name = period.ToLocalisedName(),
                Value = period
            };
        }
    }

    struct ChannelMenuItem
    {
        public string Name { get; set; }
        public RepositoryMeta.Channel Value { get; set; }

        internal static ChannelMenuItem Create(RepositoryMeta.Channel channel)
        {
            return new ChannelMenuItem
            {
                Name = channel.ToLocalisedName(),
                Value = channel
            };
        }
    }

    public class RepoConfig
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }
        [JsonProperty("channel")]
        public RepositoryMeta.Channel Channel { get; set; }

        public RepoConfig(Uri url, RepositoryMeta.Channel channel)
        {
            Url = url;
            Channel = channel;
        }

    }

    public class RepoDataGridItem
    {
        public string Url { get; set; }
        public RepositoryMeta.Channel Channel { get; set; }

        public RepoDataGridItem(String url, RepositoryMeta.Channel channel)
        {
            Url = url;
            Channel = channel;
        }

        public RepoConfig ToRepoConfig()
        {
            return new RepoConfig(new Uri(Url), Channel);
        }

        public static RepoDataGridItem Empty => new RepoDataGridItem(null, RepositoryMeta.Channel.Stable);
    }

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, ISettingsWindowView
    {
        private readonly SettingsWindowPresenter _presenter;
        private CompositeDisposable _bag = new CompositeDisposable();

        private LanguageTag LanguageTag(string tag)
        {
            var data = Iso639.GetTag(tag);
            var simplestTag = data.Tag1 ?? data.Tag3;
            var name = data.Autonym ?? data.Name;
            return new LanguageTag { Name = name, Tag = simplestTag };
        }

        public SettingsWindow()
        {
            InitializeComponent();

            DdlLanguage.ItemsSource = new ObservableCollection<LanguageTag>
            {
                new LanguageTag { Name = "System Default", Tag = null },
                LanguageTag("en"),
                LanguageTag("nb"),
                LanguageTag("nn"),
                new LanguageTag { Name = "ᚿᛦᚿᚮᚱᛌᚴ", Tag = "nn-Runr" },
                LanguageTag("se")
            };

            DdlUpdateFreq.ItemsSource = new ObservableCollection<PeriodIntervalMenuItem>
            {
                PeriodIntervalMenuItem.Create(PeriodInterval.Daily),
                PeriodIntervalMenuItem.Create(PeriodInterval.Weekly),
                PeriodIntervalMenuItem.Create(PeriodInterval.Fortnightly),
                PeriodIntervalMenuItem.Create(PeriodInterval.Monthly),
                PeriodIntervalMenuItem.Create(PeriodInterval.Never)
            };

            DgComboBoxChannel.ItemsSource = new ObservableCollection<ChannelMenuItem>
            {
                ChannelMenuItem.Create(RepositoryMeta.Channel.Stable),
                ChannelMenuItem.Create(RepositoryMeta.Channel.Alpha),
                ChannelMenuItem.Create(RepositoryMeta.Channel.Beta),
                ChannelMenuItem.Create(RepositoryMeta.Channel.Nightly)
            };

            //DgRepos.CanUserResizeColumns = false;
            DgRepos.CanUserResizeRows = false;
            DgRepos.CanUserReorderColumns = false;
            DgRepos.CanUserAddRows = true;

            var app = (PahkatApp)Application.Current;
            _presenter = new SettingsWindowPresenter(this, app.ConfigStore);

            _presenter.Start().DisposedBy(_bag);
        }

        public IObservable<EventArgs> OnSaveClicked() =>
            BtnSave.ReactiveClick().Select(x => x.EventArgs);

        public IObservable<EventArgs> OnCancelClicked() =>
            BtnCancel.ReactiveClick().Select(x => x.EventArgs);

        public IObservable<EventArgs> OnRepoAddClicked() =>
            BtnAddRepo.ReactiveClick().Select(x => x.EventArgs);

        public IObservable<int> OnRepoRemoveClicked() =>
            BtnRemoveRepo.ReactiveClick()
                .Where(_ => DgRepos.SelectedIndex > -1)
                .Select(_ => DgRepos.SelectedIndex);

        public void SetRepoItemSource(ObservableCollection<RepoDataGridItem> repos)
        {
            DgRepos.ItemsSource = repos;
        }

        public void SetInterfaceLanguage(string tag)
        {
            DdlLanguage.SelectedValue = tag;
        }

        public void SetUpdateFrequency(PeriodInterval period)
        {
            DdlUpdateFreq.SelectedValue = period;
        }

        public void SetUpdateFrequencyStatus(DateTimeOffset dateTime)
        {
            LblUpdateStatus.Content = string.Format(Strings.NextUpdateDue, dateTime.ToString());
        }

        public SettingsFormData SettingsFormData()
        {
            // TODO: wtf is this for?
            //Uri.TryCreate(TxtRepoUri.Text, UriKind.Absolute, out var repoUri);
            
            return new SettingsFormData
            {
                InterfaceLanguage = (string) DdlLanguage.SelectedValue,
                UpdateCheckInterval = (PeriodInterval) DdlUpdateFreq.SelectedValue
            };
        }

        public void SetRepositoryStatus(string status)
        {

            //LblRepoName.Content = status;
        }

        public void HandleError(Exception error)
        {
            MessageBox.Show(error.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void SelectRow(int index)
        {
            DgRepos.SelectedIndex = Math.Min(DgRepos.Items.Count - 1, index);
        }

        public void SelectLastRow()
        {
            DgRepos.SelectedIndex = DgRepos.Items.Count - 1;
        }
    }
}
