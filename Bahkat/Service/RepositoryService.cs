using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Bahkat.Extensions;
using Bahkat.Models;
using Bahkat.Properties;
using Bahkat.Service.RepositoryServiceEvent;
using Bahkat.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bahkat.Service
{
    public class RepositoryResult
    {
        public Uri Uri;
        public Exception Error;
        public Repository Repository;
    }

    public class RepositoryServiceState
    {
        internal Uri RepoUri = new Uri(Constants.Repository);
        internal RepositoryResult RepoResult;
    }

    public interface IRepositoryServiceEvent
    {
    }

    namespace RepositoryServiceEvent
    {
        public class ForceRefresh : IRepositoryServiceEvent
        {
        }

        public class DownloadIndex : IRepositoryServiceEvent
        {
            public Uri Uri;
        }
        
        public class SetRepository : IRepositoryServiceEvent
        {
            public RepositoryResult RepoResult;
        }
    }

    public static class RepositoryServiceAction
    {
        public static ForceRefresh ForceRefresh => new ForceRefresh();
        
        public static DownloadIndex DownloadIndex(Uri uri)
        {
            return new DownloadIndex()
            {
                Uri = uri
            };
        }
        
        public static SetRepository SetRepository(RepositoryResult repo)
        {
            return new SetRepository()
            {
                RepoResult = repo
            };
        }
    }

    public interface IRepositoryApi
    {
        IObservable<RepoIndex> RepoIndex(DownloadProgressChangedEventHandler onProgress = null);
        IObservable<PackagesIndex> PackagesIndex(
            DownloadProgressChangedEventHandler onProgress = null);
        IObservable<VirtualsIndex> VirtualsIndex(
            DownloadProgressChangedEventHandler onProgress = null);
    }

    public class RepositoryApi : IRepositoryApi
    {
        public readonly Uri BaseUri;

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSerializerSettings);
        }
        
        protected async Task<T> DownloadAsyncTask<T>(Uri uri, DownloadProgressChangedEventHandler onProgress)
        {
            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                if (onProgress != null)
                {
                    client.DownloadProgressChanged += onProgress;
                }
          
                var jsonString = await client.DownloadStringTaskAsync(uri);
                return FromJson<T>(jsonString);
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
            return DownloadAsync<RepoIndex>("index.json", onProgress)
                .DoIfNull(() => Observable.Throw<RepoIndex>(new NullReferenceException()));
        }

        public IObservable<PackagesIndex> PackagesIndex(
            DownloadProgressChangedEventHandler onProgress = null)
        {
            return DownloadAsync<PackagesIndex>("packages/index.json", onProgress)
                .DoIfNull(() => Observable.Throw<PackagesIndex>(new NullReferenceException()));
        }
        
        public IObservable<VirtualsIndex> VirtualsIndex(
            DownloadProgressChangedEventHandler onProgress = null)
        {
            return DownloadAsync<VirtualsIndex>("virtuals/index.json", onProgress)
                .DoIfNull(() => Observable.Throw<VirtualsIndex>(new NullReferenceException()));
        }
        
        public IObservable<VirtualPackage> Virtual(string id,
            DownloadProgressChangedEventHandler onProgress = null)
        {
            return DownloadAsync<VirtualPackage>($"virtuals/{id}/index.json", onProgress)
                .DoIfNull(() => Observable.Throw<VirtualPackage>(new NullReferenceException()));
        }
    }

    public class RepositoryService
    {
        public IObservable<RepositoryServiceState> System { get; }
        private Subject<IRepositoryServiceEvent> _eventSubject = new Subject<IRepositoryServiceEvent>();
        private Func<Uri, IRepositoryApi> _createApi;

        protected IObservable<RepositoryResult> DownloadRepoIndexes(Uri repoUri)
        {
            var api = _createApi(repoUri);

            return Observable.CombineLatest(
                api.RepoIndex(),
                api.PackagesIndex(),
                api.VirtualsIndex(),
                (main, packages, virtuals) => 
                {
                    Console.WriteLine("Repository downloaded successfully.");
                    return new RepositoryResult()
                    {
                        Uri = repoUri,
                        Repository = new Repository(main, packages, virtuals).Validate()
                    };
                });
        }
        
        protected RepositoryServiceState Reduce(RepositoryServiceState state, IRepositoryServiceEvent e)
        {
            switch (e)
            {
                case ForceRefresh v:
                    state.RepoResult = null;
                    return state;
                case DownloadIndex v:
                    if (state.RepoUri != v.Uri)
                    {
                        state.RepoUri = v.Uri;
                        state.RepoResult = null;
                    }
                    return state;
                case SetRepository v:
                    Console.WriteLine("Set repository.");
                    state.RepoResult = v.RepoResult;
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
                            _eventSubject.AsObservable(),
                            
                            Observable.CombineLatest<Uri, RepositoryResult, Uri>(
                                state.Select(s => s.RepoUri).DistinctUntilChanged(),
                                state.Select(s => s.RepoResult).DistinctUntilChanged(),
                                (uri, result) =>
                                {
                                    // If no result, always try to get a new repo
                                    if (result == null)
                                    {
                                        return uri;
                                    }
                                    
                                    // If the uri is not equal to the result, try also.
                                    if (result.Uri != uri)
                                    {
                                        return uri;
                                    }

                                    return null;
                                })
                                .NotNull()
                                .Select(DownloadRepoIndexes)
                                .Switch()
                                .Catch<RepositoryResult, Exception>(error => Observable.Return(new RepositoryResult
                                {
                                    Error = error
                                }))
                                .Select(RepositoryServiceAction.SetRepository),
                        }
                    );
                }))
                .Replay(1)
                .RefCount();
        }

        public void SetRepositoryUri(Uri uri)
        {
            _eventSubject.OnNext(RepositoryServiceAction.DownloadIndex(uri));
        }

        public void Refresh()
        {
            Console.WriteLine("Repository service refresh has been triggered.");
            _eventSubject.OnNext(RepositoryServiceAction.ForceRefresh);
        }
    }
}