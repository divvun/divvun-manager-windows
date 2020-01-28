using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Pahkat.Models;
using Pahkat.Sdk;
using Pahkat.UI.Updater;
using Quartz;
using Quartz.Impl;

namespace Pahkat.Service
{
    public class UpdaterService : IDisposable
    {
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        private class UpdateCheckJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                var service = (UpdaterService)context.MergedJobDataMap["UpdaterService"];
                Application.Current.Dispatcher.InvokeAsync(() => service.CheckForUpdates(false));
            }
        }
        
        private Quartz.IScheduler _jobScheduler = StdSchedulerFactory.GetDefaultScheduler();
        private readonly TriggerKey _updateCheckKey = new TriggerKey("NextUpdateCheck");

        private void InitRepoRefresher(AppConfigStore configStore)
        {
            configStore.State.Select(x => x.NextUpdateCheck)
                .Subscribe(date =>
                {
                    _jobScheduler.UnscheduleJob(_updateCheckKey);
                    
                    IDictionary<string, object> dict = new Dictionary<string, object>();
                    dict["UpdaterService"] = this;

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(_updateCheckKey)
                        .StartAt(date)
                        .UsingJobData(new JobDataMap(dict))
                        .Build();

                    var detail = JobBuilder.Create<UpdateCheckJob>().Build();

                    _jobScheduler.ScheduleJob(detail, trigger);
                });
        }

        public bool HasUpdates()
        {
            var app = (PahkatApp) Application.Current;
            return app.PackageStore.RepoIndexes().Any((repo) =>
            {
                return repo.Packages.Values.Any((p) =>
                {
                    return app.PackageStore.Status(repo.PackageKeyFor(p)).Item1 == PackageStatus.RequiresUpdate;
                });
            });
        }

        public void CheckForUpdates(bool skipSelfUpdate)
        {
            var app = (PahkatApp) Application.Current;
            if (!skipSelfUpdate)
            {
                // Unfortunately, this causes some very strange things. Loops, a random main window, oh god.
//                var selfUpdateClient = app.CheckForSelfUpdate();
//                if (selfUpdateClient != null)
//                {
//                    if (app.RunSelfUpdate())
//                    {
//                        return;
//                    }
//                }
            }
            
            if (HasUpdates())
            {
                app.WindowService.Show<UpdateWindow>();
            }
        }

        public UpdaterService(AppConfigStore configStore)
        {
            _jobScheduler.Start();
            InitRepoRefresher(configStore);
            CheckForUpdates(true);
        }

        public void Dispose()
        {
            _jobScheduler.Shutdown();
            _jobScheduler = null;
        }
    }
}