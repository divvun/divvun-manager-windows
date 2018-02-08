using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.UI.Shared;

namespace Pahkat.UI.Settings
{
    public interface ISettingsWindowView : IWindowView
    {
        IObservable<EventArgs> OnSaveClicked();
        IObservable<EventArgs> OnCancelClicked();
        void SetInterfaceLanguage(string tag);
        void SetRepository(string repo);
        void SetRepositoryStatus(string status);
        void SetUpdateFrequency(PeriodInterval period);
        void SetUpdateFrequencyStatus(DateTimeOffset dateTime);
        SettingsFormData SettingsFormData();
        void HandleError(Exception error);
        void Close();
    }

    public struct SettingsFormData
    {
        public string InterfaceLanguage;
        public PeriodInterval UpdateCheckInterval;
        public Uri RepositoryUrl;
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
    
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, ISettingsWindowView
    {
        private readonly SettingsWindowPresenter _presenter;
        private CompositeDisposable _bag = new CompositeDisposable();

        public SettingsWindow()
        {
            InitializeComponent();

            DdlLanguage.ItemsSource = new ObservableCollection<LanguageTag>
            {
                new LanguageTag {Name = "English", Tag = "en"}
            };

            DdlUpdateFreq.ItemsSource = new ObservableCollection<PeriodIntervalMenuItem>
            {
                PeriodIntervalMenuItem.Create(PeriodInterval.Daily),
                PeriodIntervalMenuItem.Create(PeriodInterval.Weekly),
                PeriodIntervalMenuItem.Create(PeriodInterval.Fortnightly),
                PeriodIntervalMenuItem.Create(PeriodInterval.Monthly),
                PeriodIntervalMenuItem.Create(PeriodInterval.Never)
            };

            var app = (BahkatApp) Application.Current;
            _presenter = new SettingsWindowPresenter(this, app.RepositoryService, app.ConfigStore);
            _bag.Add(_presenter.Start());
        }

        public IObservable<EventArgs> OnSaveClicked() =>
            BtnSave.ReactiveClick().Select(x => x.EventArgs);

        public IObservable<EventArgs> OnCancelClicked() =>
            BtnCancel.ReactiveClick().Select(x => x.EventArgs);

        public void SetRepository(string repo)
        {
            TxtRepoUri.Text = repo;
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
            Uri.TryCreate(TxtRepoUri.Text, UriKind.Absolute, out var repoUri);
            
            return new SettingsFormData
            {
                InterfaceLanguage = (string) DdlLanguage.SelectedValue,
                UpdateCheckInterval = (PeriodInterval) DdlUpdateFreq.SelectedValue,
                RepositoryUrl = repoUri
            };
        }

        public void SetRepositoryStatus(string status)
        {
            LblRepoName.Content = status;
        }

        public void HandleError(Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
