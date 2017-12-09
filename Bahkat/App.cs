using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Bahkat.Models;
using Bahkat.Service;
using Bahkat.Util;

namespace Bahkat
{
    public interface IBahkatApp
    {
        AppConfigStore ConfigStore { get; }
        RepositoryService RepositoryService { get; }
        void OnStartup(EventArgs e);
    }

    public abstract class AbstractBahkatApp : IBahkatApp
    {
        public abstract AppConfigStore ConfigStore { get; }
        public abstract RepositoryService RepositoryService { get; }

        public virtual void OnStartup(EventArgs e)
        {
            // main window turn on, who set up us the bomb
            ConfigStore.State
                .Select(s => s.RepositoryUrl)
                .Subscribe(RepositoryService.SetRepositoryUri);
        }
    }

    public class BahkatApp : AbstractBahkatApp
    {
        public override AppConfigStore ConfigStore { get; } =
            new AppConfigStore(new WindowsRegistry());
        public override RepositoryService RepositoryService { get; } =
            new RepositoryService(RepositoryApi.Create, Scheduler.CurrentThread);

        public override void OnStartup(EventArgs e)
        {
            RepositoryService.System.Subscribe();
            
            base.OnStartup(e);
        }
    }

//    public class MockApp : AbstractBahkatApp
//    {
//        public override AppConfigStore ConfigStore { get; } =
//            new AppConfigStore(new MockRegistry());
//        public override RepositoryService RepositoryService { get; } =
//            new RepositoryService();
//    }
}