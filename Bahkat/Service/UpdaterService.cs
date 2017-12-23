using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Models.PackageManager;
using Quartz;
using Quartz.Impl;

namespace Bahkat.Service
{
    public class UpdaterService
    {
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        private class UpdateCheckJob : IJob
        {
            internal static RepositoryService RepoServ;

            public void Execute(IJobExecutionContext context)
            {
                RepoServ?.Refresh();
            }
        }

        private readonly Quartz.IScheduler _jobScheduler = StdSchedulerFactory.GetDefaultScheduler();
        private readonly TriggerKey _updateCheckKey = new TriggerKey("NextUpdateCheck");

        private void InitRepoRefresher(AppConfigStore configStore)
        {
            configStore.State.Select(x => x.NextUpdateCheck)
                .Subscribe(date =>
                {
                    _jobScheduler.UnscheduleJob(_updateCheckKey);
                    
                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(_updateCheckKey)
                        .StartAt(date)
                        .Build();

                    var detail = JobBuilder.Create<UpdateCheckJob>().Build();

                    _jobScheduler.ScheduleJob(detail, trigger);
                });
        }

        private void InitUpdateWindowSubscriber(AppConfigStore configStore, RepositoryService repoServ, PackageService pkgServ)
        {
            Observable.CombineLatest<Repository, DateTimeOffset, Repository>(
                    repoServ.System.Select(s => s.RepoResult?.Repository),
                    configStore.State.Select(x => x.NextUpdateCheck),
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
                .Subscribe(repo =>
                {
                    // Update the next check time
                    configStore.Dispatch(AppConfigAction.IncrementNextUpdateCheck);

                    var hasUpdates = repo.PackagesIndex.Values.Any(pkg =>
                        pkgServ.GetInstallStatus(pkg) == PackageInstallStatus.NeedsUpdate);

                    if (hasUpdates)
                    {
                        var app = (IBahkatApp) Application.Current;
                        app.ShowUpdaterWindow();
                    }
                });
        }
        
        public UpdaterService(AppConfigStore configStore, RepositoryService repoServ, PackageService pkgServ)
        {
            // Static hacks suck but are a necessary part of enterprise OO life.
            UpdateCheckJob.RepoServ = repoServ;

            _jobScheduler.Start();
            
            InitRepoRefresher(configStore);
            InitUpdateWindowSubscriber(configStore, repoServ, pkgServ);
        }
    }
}