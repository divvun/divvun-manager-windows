using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Pahkat.Extensions;
using Pahkat.Models;
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
                var repoServ = (RepositoryService)context.Trigger.JobDataMap["repoServ"];
                repoServ.Refresh();
            }
        }

        private readonly AppConfigStore _configStore;
        private readonly RepositoryService _repoServ;
        private readonly IPackageService _pkgServ;
        private readonly Quartz.IScheduler _jobScheduler = StdSchedulerFactory.GetDefaultScheduler();
        private readonly TriggerKey _updateCheckKey = new TriggerKey("NextUpdateCheck");

        private void InitRepoRefresher(AppConfigStore configStore)
        {
            configStore.State.Select(x => x.NextUpdateCheck)
                .Subscribe(date =>
                {
                    _jobScheduler.UnscheduleJob(_updateCheckKey);
                    
                    var jobMap = (IDictionary) new Dictionary<string, object>
                    {
                        { "repoServ", _repoServ }
                    };
                    
                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(_updateCheckKey)
                        .StartAt(date)
                        .UsingJobData(new JobDataMap(jobMap))
                        .Build();

                    var detail = JobBuilder.Create<UpdateCheckJob>().Build();

                    _jobScheduler.ScheduleJob(detail, trigger);
                });
        }

        private bool HasNewUpdates(Repository repo)
        {
            // Update the next check time
            _configStore.Dispatch(AppConfigAction.IncrementNextUpdateCheck);

            return repo.Packages.Values.Any(_pkgServ.RequiresUpdate);
        }

        private void InitUpdateWindowSubscriber()
        {
            Observable.CombineLatest<Repository, DateTimeOffset, Repository>(
                    _repoServ.System.Select(s => s.RepoResult?.Repository),
                    _configStore.State.Select(x => x.NextUpdateCheck),
                    (repo, nextCheck) =>
                    {
                        if (repo == null)
                        {
                            return null;
                        }

                        if (DateTimeOffset.Now >= nextCheck)
                        {
                            return repo;
                        }

                        return null;
                    })
                .NotNull()
                .DistinctUntilChanged()
                .Where(HasNewUpdates)
                .Subscribe(_ =>
                {
                    var app = (IBahkatApp) Application.Current;
                    app.WindowService.Show<UpdateWindow>();
                });
        }

        public IObservable<bool> CheckForUpdatesImmediately()
        {
            _configStore.Dispatch(AppConfigAction.CheckForUpdatesImmediately);
            
            return _repoServ.System
                .Select(s => s.RepoResult?.Repository)
                .Take(1)
                .Select(repo => repo?.Packages.Values.Any(_pkgServ.RequiresUpdate) ?? false);
        }
        
        public UpdaterService(AppConfigStore configStore, RepositoryService repoServ, IPackageService pkgServ)
        {
            _configStore = configStore;
            _repoServ = repoServ;
            _pkgServ = pkgServ;
            _jobScheduler.Start();
            
            InitRepoRefresher(configStore);
            InitUpdateWindowSubscriber();
        }

        public void Dispose()
        {
            _jobScheduler.Shutdown();
        }
    }
}