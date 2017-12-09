using System;
using Bahkat.Models.AppConfigEvent;
using Bahkat.Properties;
using Bahkat.UI.Main;
using Bahkat.Util;
using Castle.Core.Internal;
using Microsoft.Win32;

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
    
    public class AppConfigState
    {
        public Uri RepositoryUrl { get; internal set; }
        public PeriodInterval UpdateCheckInterval { get; internal set; }

        private readonly IWindowsRegKey _rk;
        
        public AppConfigState(IWindowsRegistry registry)
        {
            _rk = registry.LocalMachine.CreateSubKey(@"SOFTWARE\" + Constants.RegistryId);

            RepositoryUrl = new Uri(_rk.Get("RepositoryUrl", Constants.Repository));
            UpdateCheckInterval = _rk.Get("UpdateCheckInterval", v =>
            {
                if (v is string x)
                {
                    return (PeriodInterval) Enum.Parse(typeof(PeriodInterval), x);
                }

                return Constants.UpdateCheckInterval;
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
    }
    
    public class AppConfigStore : RxStore<AppConfigState>
    {   
        private static AppConfigState Reduce(AppConfigState state, IStoreEvent e)
        {
            Console.WriteLine(e);
            
            switch (e as IAppConfigEvent)
            {
                case SetRepositoryUrl v:
                    state.RepositoryUrl = v.Uri;
                    state.UpdateRegKey("RepositoryUrl", v.Uri.AbsoluteUri, RegistryValueKind.String);
                    return state;
            }

            return state;
        }

        public AppConfigStore(IWindowsRegistry registry) : base(new AppConfigState(registry), Reduce)
        {
        }
    }
}