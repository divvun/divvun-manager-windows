using System;
using System.Globalization;
using System.Windows.Documents;
using Pahkat.Extensions;
using Pahkat.Models.AppConfigEvent;
using Pahkat.Properties;
using Pahkat.Util;
using Microsoft.Win32;
using NUnit.Framework.Constraints;
using Pahkat.UI.Settings;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;

namespace Pahkat.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PeriodInterval
    {
        [EnumMember(Value = "never")]
        Never,
        [EnumMember(Value = "daily")]
        Daily,
        [EnumMember(Value = "weekly")]
        Weekly,
        [EnumMember(Value = "fortnightly")]
        Fortnightly,
        [EnumMember(Value = "monthly")]
        Monthly
    }

    public static class PeriodIntervalExtensions
    {
        public static TimeSpan ToTimeSpan(this PeriodInterval periodInterval)
        {
            switch (periodInterval)
            {
                case PeriodInterval.Daily:
                    return TimeSpan.FromDays(1);
                case PeriodInterval.Weekly:
                    return TimeSpan.FromDays(7);
                case PeriodInterval.Fortnightly:
                    return TimeSpan.FromDays(14);
                case PeriodInterval.Monthly:
                    return TimeSpan.FromDays(28);
                case PeriodInterval.Never:
                    return TimeSpan.Zero;
                default:
                    return TimeSpan.Zero;
            }
        }

        public static string ToLocalisedName(this PeriodInterval periodInterval)
        {
            switch (periodInterval)
            {
                case PeriodInterval.Daily:
                    return Strings.Daily;
                case PeriodInterval.Weekly:
                    return Strings.Weekly;
                case PeriodInterval.Fortnightly:
                    return Strings.EveryTwoWeeks;
                case PeriodInterval.Monthly:
                    return Strings.EveryFourWeeks;
                case PeriodInterval.Never:
                    return Strings.Never;
                default:
                    return "";
            }
        }
    }
    
    public class AppConfigState
    {
        public static string ConfigPath
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                // TODO: make sure core client uses another file
                return Path.Combine(appData, "Pahkat", "config.json");
            }
        }

        [JsonProperty("repositories")]
        public RepoConfig[] Repositories { get; internal set; }
        [JsonProperty("updateCheckInterval")]
        public PeriodInterval UpdateCheckInterval { get; internal set; }
        [JsonProperty("nextUpdateCheck")]
        public DateTimeOffset NextUpdateCheck { get; internal set; }
        [JsonProperty("interfaceLanguage")]
        public string InterfaceLanguage { get; internal set; }

        public static AppConfigState Load()
        {
            AppConfigState state;

            try
            {
                state = JsonConvert.DeserializeObject<AppConfigState>(
                    File.ReadAllText(ConfigPath));
            }
            catch (Exception e)
            {
                state = new AppConfigState();
            }

            if (state.Repositories == null)
            {
                state.Repositories = new RepoConfig[] {
                    new RepoConfig(new Uri("https://x.brendan.so/test-repo"), RepositoryMeta.Channel.Stable)
                };
            }

            if (state.NextUpdateCheck == null)
            {
                state.NextUpdateCheck = new DateTimeOffset();
            }

            return state;
        }
    }
    
    public interface IAppConfigEvent {}

    namespace AppConfigEvent
    {
        internal class SetRepositories : IAppConfigEvent
        {
            public RepoConfig[] Repositories { get; }
            
            public SetRepositories(RepoConfig[] repos)
            {
                Repositories = repos;
            }
        }
        internal class SetInterfaceLanguage : IAppConfigEvent
        {
            public readonly string Tag;
            
            public SetInterfaceLanguage(string tag)
            {
                Tag = tag;
            }
        }

        internal class SetUpdateCheckInterval : IAppConfigEvent
        {
            public readonly PeriodInterval Interval;
            
            public SetUpdateCheckInterval(PeriodInterval interval)
            {
                Interval = interval;
            }
        }

        internal class IncrementNextUpdateCheck : IAppConfigEvent {}
        internal class CheckForUpdatesImmediately : IAppConfigEvent {}
    }

    public static class AppConfigAction
    {
        public static readonly IAppConfigEvent IncrementNextUpdateCheck = new IncrementNextUpdateCheck();
        public static readonly IAppConfigEvent CheckForUpdatesImmediately = new CheckForUpdatesImmediately();
        public static IAppConfigEvent SetRepositories(RepoConfig[] repos)
        {
            return new SetRepositories(repos);
        }
        public static IAppConfigEvent SetInterfaceLanguage(string tag)
        {
            return new SetInterfaceLanguage(tag);
        }
        public static IAppConfigEvent SetUpdateCheckInterval(PeriodInterval interval)
        {
            return new SetUpdateCheckInterval(interval);
        }
    }
    
    public class AppConfigStore : RxStore<AppConfigState, IAppConfigEvent>
    {   
        private static void Save(AppConfigState state)
        {
            var data = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(AppConfigState.ConfigPath, data);
        }
        
        internal static string CurrentSystemLanguage()
        {
            var culture = CultureInfo.CurrentCulture;

            if (culture.TwoLetterISOLanguageName != null)
            {
                return culture.TwoLetterISOLanguageName;
            }
            else if (culture.ThreeLetterISOLanguageName != null)
            {
                return culture.ThreeLetterISOLanguageName;
            }
            else
            {
                return "en";
            }
        }

        private static AppConfigState Reduce(AppConfigState state, IAppConfigEvent e)
        {
            switch (e)
            {
                case SetUpdateCheckInterval v:
                    if (state.UpdateCheckInterval == v.Interval)
                    {
                        return state;
                    }
                    state.UpdateCheckInterval = v.Interval;
                    break;
                case SetInterfaceLanguage v:
                    if (state.InterfaceLanguage == v.Tag)
                    {
                        return state;
                    }

                    state.InterfaceLanguage = v.Tag;
                    break;
                case SetRepositories v:
                    state.Repositories = v.Repositories;
                    break;
                case CheckForUpdatesImmediately v:
                    state.NextUpdateCheck = DateTimeOffset.Now;
                    break;
                case IncrementNextUpdateCheck v:
                    state.NextUpdateCheck = state.NextUpdateCheck.Add(state.UpdateCheckInterval.ToTimeSpan());
                    break;
            }

            Save(state);
            return state;
        }

        public AppConfigStore() : base(AppConfigState.Load(), Reduce)
        {
        }
    }
}