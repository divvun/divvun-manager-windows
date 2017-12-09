using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Feedback;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bahkat.Models.PackageManager;
using Bahkat.Service.RepositoryServiceEvent;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bahkat.Service
{
    public class RepositoryServiceState
    {
        internal Uri RepoUri;
        internal Repository Repository;
    }

    public interface IRepositoryServiceEvent
    {
    }

    namespace RepositoryServiceEvent
    {
        public class DownloadIndex : IRepositoryServiceEvent
        {
            public Uri Uri;
        }
        
        public class SetRepository : IRepositoryServiceEvent
        {
            public Repository Repository;
        }
    }

    public static class RepositoryServiceAction
    {
        public static DownloadIndex DownloadIndex(Uri uri)
        {
            return new DownloadIndex()
            {
                Uri = uri
            };
        }
        
        public static SetRepository SetRepository(Repository repo)
        {
            return new SetRepository()
            {
                Repository = repo
            };
        }
    }

    public interface IRepositoryApi
    {
        IObservable<RepoIndex> RepoIndex(DownloadProgressChangedEventHandler onProgress = null);
        IObservable<Dictionary<string, Package>> PackagesIndex(
            DownloadProgressChangedEventHandler onProgress = null);
        IObservable<Dictionary<string, List<string>>> VirtualsIndex(
            DownloadProgressChangedEventHandler onProgress = null);
    }

    public class RepositoryApi : IRepositoryApi
    {
        public readonly Uri BaseUri;
        
        protected async Task<T> DownloadAsyncTask<T>(Uri uri, DownloadProgressChangedEventHandler onProgress)
        {
            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                if (onProgress != null)
                {
                    client.DownloadProgressChanged += onProgress;
                }
          
                var jsonString = await client.DownloadStringTaskAsync(uri);
                var serializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                
                return JsonConvert.DeserializeObject<T>(jsonString, serializerSettings);
            }
        }

        protected IObservable<T> DownloadAsync<T>(string uri, DownloadProgressChangedEventHandler onProgress)
        {
            return Observable.FromAsync(() => DownloadAsyncTask<T>(new Uri(BaseUri, uri), onProgress));
        }

        public static RepositoryApi Create(Uri baseUri)
        {
            return new RepositoryApi(baseUri);
        }
        
        public RepositoryApi(Uri baseUri)
        {
            BaseUri = baseUri;
        }

        public IObservable<RepoIndex> RepoIndex(DownloadProgressChangedEventHandler onProgress = null)
        {
            return DownloadAsync<RepoIndex>("index.json", onProgress);
        }

        public IObservable<Dictionary<string, Package>> PackagesIndex(
            DownloadProgressChangedEventHandler onProgress = null)
        {
            return DownloadAsync<Dictionary<string, Package>>("packages/index.json", onProgress);
        }
        
        public IObservable<Dictionary<string, List<string>>> VirtualsIndex(
            DownloadProgressChangedEventHandler onProgress = null)
        {
            return DownloadAsync<Dictionary<string, List<string>>>("virtuals/index.json", onProgress);
        }
    }

    public class RepositoryService
    {
        public IObservable<RepositoryServiceState> System { get; }
        private Subject<Uri> _uriSubject = new Subject<Uri>();
        private Func<Uri, IRepositoryApi> _createApi;

        protected IObservable<Repository> DownloadRepoIndexes(Uri repoUri)
        {
            var api = _createApi(repoUri);
            
            Console.WriteLine("OKKKKK");
            
            return Observable.CombineLatest(
                api.RepoIndex(),
                api.PackagesIndex(),
                api.VirtualsIndex(),
                (main, packages, virtuals) => new Repository(main, packages, virtuals));
        }
        
        protected RepositoryServiceState Reduce(RepositoryServiceState state, IRepositoryServiceEvent e)
        {
            switch (e)
            {
                case DownloadIndex v:
                    if (state.RepoUri != v.Uri)
                    {
                        state.RepoUri = v.Uri;
                        state.Repository = null;
                    }
                    return state;
                case SetRepository v:
                    Console.WriteLine("FUCKITY WHY");
                    state.Repository = v.Repository;
                    return state;
            }

            return state;
        }

        public RepositoryService(Func<Uri, IRepositoryApi> createApi, IScheduler scheduler)
        {
            _createApi = createApi;
            
            System = Feedback.System(new RepositoryServiceState(),
                Reduce,
                scheduler,
                Feedback.Bind<RepositoryServiceState, IRepositoryServiceEvent>(state =>
                {
                    return new UIBindings<IRepositoryServiceEvent>(
                        new IDisposable[]
                        {
                            // Things that subscribe to the UI, side effects and other luxuries.
                        },
                        new IObservable<IRepositoryServiceEvent>[]
                        {
                            _uriSubject.Select(RepositoryServiceAction.DownloadIndex),
                            
                            // Things that feed the feedback loop
                            state.Select(s => s.RepoUri)
                                .DistinctUntilChanged()
                                .Select(DownloadRepoIndexes)
                                .Select(x =>
                                {
                                    Console.WriteLine(("WWWWW"));
                                    return x;
                                })
                                .Switch()
                                .Select(RepositoryServiceAction.SetRepository)
                        }
                    );
                }));
        }

        public void SetRepositoryUri(Uri uri)
        {
            _uriSubject.OnNext(uri);
        }
    }
}