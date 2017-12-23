using System;
using Bahkat.Models.AppConfigEvent;
using Bahkat.Properties;
using Bahkat.UI.Main;
using Bahkat.Util;
using Castle.Core.Internal;
using Microsoft.Win32;
using NUnit.Framework;

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
        public static TimeSpan AsTimeSpan(this PeriodInterval periodInterval)
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
    }
    
    public class AppConfigState
    {
        public Uri RepositoryUrl { get; internal set; }
        public PeriodInterval UpdateCheckInterval { get; internal set; }
        public DateTimeOffset NextUpdateCheck { get; internal set; }

        private readonly IWindowsRegKey _rk;

        internal static class Keys
        {
            public const string SubkeyId = @"SOFTWARE\" + Constants.RegistryId;
            
            public const string RepositoryUrl = "RepositoryUrl";
            public const string UpdateCheckInterval = "UpdateCheckInterval";
            public const string NextUpdateCheck = "NextUpdateCheck";
        }
        
        public AppConfigState(IWindowsRegistry registry)
        {
            _rk = registry.LocalMachine.CreateSubKey(Keys.SubkeyId);

            RepositoryUrl = new Uri(_rk.Get(Keys.RepositoryUrl, Constants.Repository));

            UpdateCheckInterval = _rk.Get(Keys.UpdateCheckInterval, v =>
            {
                if (v is string x)
                {
                    return (PeriodInterval) Enum.Parse(typeof(PeriodInterval), x);
                }

                return Constants.UpdateCheckInterval;
            });

            NextUpdateCheck = _rk.Get(Keys.NextUpdateCheck, v =>
            {
                if (v is string x)
                {
                    return DateTimeOffset.Parse(x);
                }

                return DateTimeOffset.Now;
            });
        }

        internal void UpdateRegKey(string valueName, object value, RegistryValueKind kind)
        {
            _rk.Set(valueName, value, kind);
        }
    }
    
    public interface IAppConfigEvent : IStoreEvent {}

    namespace AppConfigEvent
    {
        public class SetRepositoryUrl : IAppConfigEvent
        {
            public Uri Uri { get; }
            
            public SetRepositoryUrl(Uri uri)
            {
                Uri = uri;
            }
        }
        
        public class IncrementNextUpdateCheck : IAppConfigEvent {}
    }

    public static class AppConfigAction
    {
        public static readonly IncrementNextUpdateCheck IncrementNextUpdateCheck = new IncrementNextUpdateCheck();
    }
    
    public class AppConfigStore : RxStore<AppConfigState>
    {   
        private static AppConfigState Reduce(AppConfigState state, IStoreEvent e)
        {
            switch (e as IAppConfigEvent)
            {
                case SetRepositoryUrl v:
                    state.RepositoryUrl = v.Uri;
                    state.UpdateRegKey(AppConfigState.Keys.RepositoryUrl, v.Uri.AbsoluteUri, RegistryValueKind.String);
                    return state;
                case IncrementNextUpdateCheck v:
                    var nextUpdateCheck = state.NextUpdateCheck.Add(state.UpdateCheckInterval.AsTimeSpan());
                    state.UpdateRegKey(AppConfigState.Keys.NextUpdateCheck,
                        nextUpdateCheck.ToString(),
                        RegistryValueKind.String);
                    state.NextUpdateCheck = nextUpdateCheck;
                    return state;
            }

            return state;
        }

        public AppConfigStore(IWindowsRegistry registry) : base(new AppConfigState(registry), Reduce)
        {
        }
    }
}