using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Models.AppConfigEvent;
using Bahkat.UI.Updater;
using Quartz;
using Quartz.Impl;

namespace Bahkat.Service
{
    public class UpdaterService : IDisposable
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

        private readonly AppConfigStore _configStore;
        private readonly RepositoryService _repoServ;
        private readonly PackageService _pkgServ;
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

        private bool HasNewUpdates(Repository repo)
        {
            // Update the next check time
            _configStore.Dispatch(AppConfigAction.IncrementNextUpdateCheck);

            return repo.PackagesIndex.Values.Any(_pkgServ.RequiresUpdate);
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
                .Select(repo => repo?.PackagesIndex.Values.Any(_pkgServ.RequiresUpdate) ?? false);
        }
        
        public UpdaterService(AppConfigStore configStore, RepositoryService repoServ, PackageService pkgServ)
        {
            // Static hacks suck but are a necessary part of enterprise OO life.
            UpdateCheckJob.RepoServ = repoServ;

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