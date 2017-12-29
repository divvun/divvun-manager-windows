using System;
using System.Globalization;
using System.Windows.Documents;
using Bahkat.Models.AppConfigEvent;
using Bahkat.Properties;
using Bahkat.Util;
using Microsoft.Win32;
using NUnit.Framework.Constraints;

namespace Bahkat.Models
{
    public enum PeriodInterval
    {
        Never,
        Daily,
        Weekly,
        Fortnightly,
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
        public Uri RepositoryUrl { get; internal set; }
        public PeriodInterval UpdateCheckInterval { get; internal set; }
        public DateTimeOffset NextUpdateCheck { get; internal set; }
        public string InterfaceLanguage { get; internal set; }

        private readonly IWindowsRegKey _rk;

        internal static class Keys
        {
            public const string SubkeyId = @"SOFTWARE\" + Constants.RegistryId;
            
            public const string RepositoryUrl = "RepositoryUrl";
            public const string UpdateCheckInterval = "UpdateCheckInterval";
            public const string NextUpdateCheck = "NextUpdateCheck";
            public const string InterfaceLanguage = "InterfaceLanguage";
        }
        
        public AppConfigState(IWindowsRegistry registry)
        {
            _rk = registry.LocalMachine.CreateSubKey(Keys.SubkeyId);

            try
            {
                RepositoryUrl = new Uri(_rk.Get(Keys.RepositoryUrl, Constants.Repository));
            }
            catch (Exception)
            {
                RepositoryUrl = new Uri(Constants.Repository);
            }
            
            UpdateCheckInterval = _rk.Get(Keys.UpdateCheckInterval, v =>
            {
                if (v is string x)
                {
                    PeriodInterval p;
                    if (Enum.TryParse(x, out p))
                    {
                        return p;
                    }
                }

                return Constants.UpdateCheckInterval;
            });

            NextUpdateCheck = _rk.Get(Keys.NextUpdateCheck, v =>
            {
                if (v is long x)
                {
                    return DateTimeOffset.FromUnixTimeSeconds(x);
                }

                return DateTimeOffset.Now;
            });

            InterfaceLanguage = _rk.Get(Keys.InterfaceLanguage,
                CultureInfo.CurrentUICulture.IetfLanguageTag);
        }

        internal void UpdateRegKey(string valueName, object value, RegistryValueKind kind)
        {
            _rk.Set(valueName, value, kind);
        }
    }
    
    public interface IAppConfigEvent {}

    namespace AppConfigEvent
    {
        internal class SetRepositoryUrl : IAppConfigEvent
        {
            public Uri Uri { get; }
            
            public SetRepositoryUrl(Uri uri)
            {
                Uri = uri;
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
        public static IAppConfigEvent SetRepositoryUrl(Uri uri)
        {
            return new SetRepositoryUrl(uri);
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
        private static void SaveNextUpdateCheck(AppConfigState state, DateTimeOffset time)
        {
            state.UpdateRegKey(AppConfigState.Keys.NextUpdateCheck, time.ToUnixTimeSeconds(), RegistryValueKind.QWord);
            state.NextUpdateCheck = time;
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
                    state.UpdateRegKey(AppConfigState.Keys.UpdateCheckInterval, 
                        Enum.GetName(typeof(PeriodInterval), v.Interval),
                        RegistryValueKind.String);
                    return state;
                case SetInterfaceLanguage v:
                    if (state.InterfaceLanguage == v.Tag)
                    {
                        return state;
                    }
                    state.InterfaceLanguage = v.Tag;
                    state.UpdateRegKey(AppConfigState.Keys.InterfaceLanguage, v.Tag, RegistryValueKind.String);
                    return state;
                case SetRepositoryUrl v:
                    if (state.RepositoryUrl == v.Uri)
                    {
                        return state;
                    }
                    state.RepositoryUrl = v.Uri;
                    state.UpdateRegKey(AppConfigState.Keys.RepositoryUrl, v.Uri.AbsoluteUri, RegistryValueKind.String);
                    return state;
                case CheckForUpdatesImmediately v:
                    SaveNextUpdateCheck(state, DateTimeOffset.Now);
                    return state;
                case IncrementNextUpdateCheck v:
                    var nextUpdateCheck = state.NextUpdateCheck.Add(state.UpdateCheckInterval.ToTimeSpan());
                    SaveNextUpdateCheck(state, nextUpdateCheck);
                    return state;
            }

            return state;
        }

        public AppConfigStore(IWindowsRegistry registry) : base(new AppConfigState(registry), Reduce)
        {
        }
    }
}