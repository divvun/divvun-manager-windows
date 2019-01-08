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
using System.ComponentModel;
using System.Windows;
using Pahkat.Sdk;

namespace Pahkat.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PeriodInterval
    {
        [EnumMember(Value = "daily")]
        Daily,
        [EnumMember(Value = "weekly")]
        Weekly,
        [EnumMember(Value = "fortnightly")]
        Fortnightly,
        [EnumMember(Value = "monthly")]
        Monthly,
        [EnumMember(Value = "never")]
        Never
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
        public RepoConfig[] Repositories { get; internal set; }
        public PeriodInterval UpdateCheckInterval { get; internal set; }
        public DateTimeOffset NextUpdateCheck { get; internal set; }
        public string InterfaceLanguage { get; internal set; }

        public AppConfigState(IPahkatApp app)
        {
            var config = app.Client.Config;

            Repositories = config.GetRepos();
            UpdateCheckInterval = config.GetUiSetting<PeriodInterval>("UpdateCheckInterval");
            NextUpdateCheck = config.GetUiSetting<DateTimeOffset>("NextUpdateCheck");
            InterfaceLanguage = config.GetUiSetting("InterfaceLanguage") ?? "";
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
        private static PahkatConfig Config => ((IPahkatApp) Application.Current).Client.Config;

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
                    Config.SetUiSetting("UpdateCheckInterval", v.Interval);
                    break;
                case SetInterfaceLanguage v:
                    if (state.InterfaceLanguage == v.Tag)
                    {
                        return state;
                    }

                    state.InterfaceLanguage = v.Tag;
                    Config.SetUiSetting("InterfaceLanguage", v.Tag);
                    break;
                case SetRepositories v:
                    state.Repositories = v.Repositories;
                    Config.SetRepos(v.Repositories);
                    break;
                case CheckForUpdatesImmediately v:
                    var now = DateTimeOffset.Now;
                    state.NextUpdateCheck = now;
                    Config.SetUiSetting("NextUpdateCheck", now);
                    break;
                case IncrementNextUpdateCheck v:
                    var next = state.NextUpdateCheck.Add(state.UpdateCheckInterval.ToTimeSpan());
                    state.NextUpdateCheck = next;
                    Config.SetUiSetting("NextUpdateCheck", next);
                    break;
            }

            return state;
        }

        public AppConfigStore(IPahkatApp app) : base(new AppConfigState(app), Reduce)
        {
        }
    }
}