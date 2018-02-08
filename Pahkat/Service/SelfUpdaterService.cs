using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using Pahkat.Extensions;
using Pahkat.Models;
using Pahkat.Properties;
using Pahkat.UI.Main;
using Pahkat.UI.Updater;
using Quartz;
using Quartz.Impl;
using IScheduler = System.Reactive.Concurrency.IScheduler;

namespace Pahkat.Service
{
    public class SelfUpdaterService : IPackageStore, IDisposable
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
        
        private readonly RepositoryService _selfRepoServ;
        private readonly AppConfigStore _configStore;
        private readonly IPackageService _pkgServ;
        private readonly Quartz.IScheduler _jobScheduler = StdSchedulerFactory.GetDefaultScheduler();
        private readonly TriggerKey _updateCheckKey = new TriggerKey("NextUpdateCheck");

        private Subject<Package> _packageSubject = new Subject<Package>();

        public IObservable<PackageState> State { get; }

        public SelfUpdaterService(AppConfigStore configStore, IPackageService pkgServ, IScheduler scheduler)
        {
            // Not to be confused with the repo serv used everywhere else in the codebase.
            _selfRepoServ = new RepositoryService(url => new RepositoryApi(url), scheduler);
            _selfRepoServ.SetRepositoryUri(Constants.BahkatUpdateUri);
            
            _configStore = configStore;
            _pkgServ = pkgServ;
            _jobScheduler.Start();
            
            InitRepoRefresher(configStore);
            InitUpdateWindowSubscriber();
            
            State = _packageSubject
                .Select(PackageState.SelfUpdate)
                .Replay(1)
                .RefCount();
        }
        
        private void InitRepoRefresher(AppConfigStore configStore)
        {
            configStore.State.Select(x => x.NextUpdateCheck)
                .Subscribe(date =>
                {
                    _jobScheduler.UnscheduleJob(_updateCheckKey);
                    
                    var jobMap = (IDictionary) new Dictionary<string, object>
                    {
                        { "repoServ", _selfRepoServ }
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
        
        private void InitUpdateWindowSubscriber()
        {
            Observable.CombineLatest<Repository, DateTimeOffset, Repository>(
                    _selfRepoServ.System.Select(s => s.RepoResult?.Repository),
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
                .Select(repo => repo.Packages["bahkat"])
                // TODO: this should really have its own trigger, and it should be at least daily and not configurable.
                // It should also always happen before a real update screen is meant to show up.
                .Where(_pkgServ.RequiresUpdate)
                .Subscribe(pkg =>
                {
                    var res = MessageBox.Show(string.Format(Strings.BahkatUpdateBody, pkg.Version),
                        Strings.BahkatUpdateTitle,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        var app = (BahkatApp) Application.Current;
                        app.WindowService.Show<MainWindow>(new DownloadPage(DownloadPagePresenter.SelfUpdate));
                    }
                });
        }

        public void Dispatch(IPackageEvent e)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _packageSubject?.Dispose();
            _jobScheduler.Shutdown();
        }
    }
}