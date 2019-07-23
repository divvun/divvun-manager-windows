using System;
using Pahkat.Extensions;
using Pahkat.Models.AppConfigEvent;
using Pahkat.Util;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;
using System.Windows;
using Pahkat.Sdk;
using System.Collections.Generic;

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
    
    public class AppConfigState
    {
        public List<RepoRecord> Repositories { get; internal set; }
        public PeriodInterval UpdateCheckInterval { get; internal set; }
        public DateTimeOffset NextUpdateCheck { get; internal set; }
        public string InterfaceLanguage { get; internal set; }

        public AppConfigState(PahkatApp app)
        {
            var config = app.PackageStore.Config();

            Repositories = config.Repos();
            UpdateCheckInterval = config.GetUiValue<PeriodInterval>("UpdateCheckInterval");
            NextUpdateCheck = config.GetUiValue<DateTimeOffset>("NextUpdateCheck");
            InterfaceLanguage = config.GetUiValue("InterfaceLanguage") ?? "";
        }
    }
    
    public interface IAppConfigEvent {}

    namespace AppConfigEvent
    {
        internal class SetRepositories : IAppConfigEvent
        {
            public List<RepoRecord> Repositories { get; }
            
            public SetRepositories(List<RepoRecord> repos)
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
        public static IAppConfigEvent SetRepositories(List<RepoRecord> repos)
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
        private static StoreConfig Config => ((PahkatApp) Application.Current).PackageStore.Config();

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
                    Config.SetUiValue("UpdateCheckInterval", v.Interval);
                    break;
                case SetInterfaceLanguage v:
                    if (state.InterfaceLanguage == v.Tag)
                    {
                        return state;
                    }

                    state.InterfaceLanguage = v.Tag;
                    Config.SetUiValue("InterfaceLanguage", v.Tag);
                    break;
                case SetRepositories v:
                    state.Repositories = v.Repositories;
                    Config.SetRepos(v.Repositories);
                    break;
                case CheckForUpdatesImmediately v:
                    var now = DateTimeOffset.Now;
                    state.NextUpdateCheck = now;
                    Config.SetUiValue("NextUpdateCheck", now);
                    break;
                case IncrementNextUpdateCheck v:
                    var next = state.NextUpdateCheck.Add(state.UpdateCheckInterval.ToTimeSpan());
                    state.NextUpdateCheck = next;
                    Config.SetUiValue("NextUpdateCheck", next);
                    break;
            }

            return state;
        }

        public AppConfigStore(PahkatApp app) : base(new AppConfigState(app), Reduce)
        {
        }
    }
}