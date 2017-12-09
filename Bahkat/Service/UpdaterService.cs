using System;
using System.Reactive.Concurrency;
using Bahkat.Models;

namespace Bahkat.Service
{
    public class UpdaterState {
    
    }

    public interface IUpdaterWindowView
    {
        void Show();
    }

    public class UpdaterWindowPresenter
    {
        UpdaterWindowPresenter(IUpdaterWindowView view, IUpdaterService updaterService)
        {
            
        }
    }

    public interface IUpdaterService
    {
        void Start();
    }
    
    public class UpdaterService
    {
        public UpdaterService(PackageStore repoStore, AppConfigStore configStore, Func<IUpdaterWindowView> createUpdaterWindow, IScheduler scheduler)
        {
            // Oh fuk
        }
        
        public void Start()
        {
            
        }
    }
}