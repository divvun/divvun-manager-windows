using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Iterable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FlatBuffers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk.Rpc.Fbs;
using Pahkat.Sdk.Rpc.Models;
using Serilog;
using System.Threading.Tasks;
using System.Net.Http;
using Pahkat.Sdk.Grpc;
using Grpc.Net.Client;
using System.Net;
using System.IO.Pipes;
using System.Net.Sockets;
using Grpc.Core;

namespace Pahkat.Sdk.Rpc {
    public class LoadedRepository : ILoadedRepository
    {
        public class IndexValue
        {
            public class AgentValue
            {
                public string Name;
                public string Version;
                public Uri? Url;
            }

            public Uri Url;
            public string[] Channels;
            public string? DefaultChannel;
            public IReadOnlyDictionary<string, string> Name;
            public IReadOnlyDictionary<string, string> Description;
            public AgentValue Agent;
            public Uri? LandingUrl;

            string[] LinkedRepositories;
            string[] AcceptedRepositories;
        }

        public class MetaValue
        {
            public string? Channel;
        }

        public byte[] PackagesFbs { get; set; }

        public IndexValue Index { get; set; }
        public MetaValue Meta { get; set; }
        public IPackages Packages => Fbs.Packages.GetRootAsPackages(new ByteBuffer(PackagesFbs));

        public PackageKey PackageKey(IDescriptor descriptor)
        {
            return Sdk.PackageKey.Create(Index.Url, descriptor.Id);
        }
    }

    public interface IPahkatClient
    {
        Task<CancellationTokenSource> ProcessTransaction(PackageAction[] actions, Action<TransactionResponseValue> callback);
        Task<PackageStatus> Status(PackageKey packageKey);
        Task<Dictionary<Uri, ILoadedRepository>> RepoIndexes();
        Task<Dictionary<Uri, RepoRecord>> GetRepoRecords();
        Task<Dictionary<Uri, RepoRecord>> SetRepo(Uri url, RepoRecord record);
        Task<Dictionary<Uri, RepoRecord>> RemoveRepo(Uri url);
        IObservable<Notification> Notifications();
        Task<Dictionary<Uri, LocalizationStrings>> Strings(string languageTag);
        Task<string> ResolvePackageQuery(PackageQuery query);
    }

    public class PahkatClient : IPahkatClient, IDisposable {
        private Grpc.Pahkat.PahkatClient innerClient;

        public PahkatClient()
        {


            var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions()
            {
                HttpHandler = CreateHttpHandler(),
            });

            this.innerClient = new Grpc.Pahkat.PahkatClient(channel);
        }

        public async Task<Dictionary<Uri, RepoRecord>> GetRepoRecords()
        {
            var response = await this.innerClient.GetRepoRecordsAsync(new GetRepoRecordsRequest());
            return response.Records.Map(pair =>
            {
                return (new Uri(pair.Key), new RepoRecord() { Channel = pair.Value.Channel });
            }).ToDict();
        }

        public IObservable<Notification> Notifications()
        {
            var call = this.innerClient.Notifications(new NotificationsRequest());

            return Observable.Create<Notification>(emitter =>
            {
                var cancellationToken = new CancellationTokenSource();
                _ = Task.Run(async () =>
                {
                    await foreach (var notification in call.ResponseStream.ReadAllAsync(cancellationToken.Token))
                    {
                        emitter.OnNext((Notification)notification.Value);
                    }
                });

                return Disposable.Create(() =>
                {
                    cancellationToken.Cancel();
                });
            });
        }

        public async Task<CancellationTokenSource> ProcessTransaction(PackageAction[] actions, Action<TransactionResponseValue> callback)
        {
            var cancellationToken = new CancellationTokenSource();
            var transaction = new TransactionRequest.Types.Transaction();
            transaction.Actions.Add(actions.Map(u => new Grpc.PackageAction() {Action = (uint)u.Action, Id = u.PackageKey.ToString(), Target = (uint)u.Target }));
            var call = this.innerClient.ProcessTransaction();
            _ = Task.Run(async () =>
              {
                  await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken.Token))
                  {
                      TransactionResponseValue responseValue = null;
                      switch (response.ValueCase)
                      {
                          case Grpc.TransactionResponse.ValueOneofCase.None:
                              break;
                          case Grpc.TransactionResponse.ValueOneofCase.TransactionStarted:
                              responseValue = new TransactionResponseValue.TransactionStarted()
                              {
                                  Actions = response.TransactionStarted.Actions.Map(r =>
                                  new ResolvedAction()
                                  {
                                      Action = new PackageAction(PackageKey.From(r.Action.Id), (InstallAction)r.Action.Action, (InstallTarget)r.Action.Target),
                                      Name = r.Name,
                                      Version = r.Version,
                                  }).ToArray(),
                                  IsRebootRequired = response.TransactionStarted.IsRebootRequired,
                              };
                              break;
                          case Grpc.TransactionResponse.ValueOneofCase.TransactionProgress:
                              responseValue = new TransactionResponseValue.TransactionProgress()
                              {
                                  Current = response.TransactionProgress.Current,
                                  Total = response.TransactionProgress.Total,
                                  Message = response.TransactionProgress.Message,
                              };
                              break;
                          case Grpc.TransactionResponse.ValueOneofCase.TransactionComplete:
                              responseValue = new TransactionResponseValue.TransactionComplete();
                              break;
                          case Grpc.TransactionResponse.ValueOneofCase.TransactionError:
                              responseValue = new TransactionResponseValue.TransactionError()
                              {
                                  Error = response.TransactionError.Error,
                                  PackageKey = response.TransactionError.PackageId
                              };
                              break;
                          case Grpc.TransactionResponse.ValueOneofCase.TransactionQueued:
                              responseValue = new TransactionResponseValue.TransactionQueued();
                              break;
                          case Grpc.TransactionResponse.ValueOneofCase.DownloadProgress:
                              responseValue = new TransactionResponseValue.DownloadProgress()
                              {
                                  Current = response.DownloadProgress.Current,
                                  PackageKey = PackageKey.From(response.DownloadProgress.PackageId),
                                  Total = response.DownloadProgress.Total,
                              };
                              break;
                          case Grpc.TransactionResponse.ValueOneofCase.DownloadComplete:
                              responseValue = new TransactionResponseValue.DownloadComplete()
                              {
                                  PackageKey = PackageKey.From(response.DownloadComplete.PackageId),
                              };
                              break;
                          case Grpc.TransactionResponse.ValueOneofCase.InstallStarted:
                              responseValue = new TransactionResponseValue.InstallStarted()
                              {
                                  PackageKey = PackageKey.From(response.InstallStarted.PackageId),
                              };
                              break;
                          case Grpc.TransactionResponse.ValueOneofCase.UninstallStarted:
                              responseValue = new TransactionResponseValue.UninstallStarted()
                              {
                                  PackageKey = PackageKey.From(response.UninstallStarted.PackageId),
                              };
                              break;
                      }
                      callback(responseValue);
                  }
              });
            
            await call.RequestStream.WriteAsync(new TransactionRequest()
            {
                Transaction = transaction,
            });

            await call.RequestStream.CompleteAsync();

            return cancellationToken;
        }

        public async Task<Dictionary<Uri, RepoRecord>> RemoveRepo(Uri url)
        {
            var response = await this.innerClient.RemoveRepoAsync(new RemoveRepoRequest() { Url = url.AbsoluteUri });
            return response.Records.Map(pair =>
            {
                return (new Uri(pair.Key), new RepoRecord() { Channel = pair.Value.Channel });
            }).ToDict();
        }

        public async Task<Dictionary<Uri, ILoadedRepository>> RepoIndexes()
        {
            var response = await this.innerClient.RepositoryIndexesAsync(new RepositoryIndexesRequest());
            return response.Repositories.Map(repo =>
            {
                var ser = JsonConvert.SerializeObject(repo);
                var de = JsonConvert.DeserializeObject<LoadedRepository>(ser);
                return (de.Index.Url, (ILoadedRepository)de);
            }).ToDict();
        }

        public async Task<string> ResolvePackageQuery(PackageQuery query)
        {
            var response = await this.innerClient.ResolvePackageQueryAsync(new JsonRequest() { Json = JsonConvert.SerializeObject(query, Json.Settings.Value) });
            return response.Json;
        }

        public async Task<Dictionary<Uri, RepoRecord>> SetRepo(Uri url, RepoRecord record)
        {
            var response = await this.innerClient.SetRepoAsync(new SetRepoRequest() 
            { 
                Settings = new Grpc.RepoRecord() 
                { 
                    Channel = record.Channel 
                }, Url = url.AbsoluteUri 
            });

            return response.Records.Map(pair =>
            {
                return (new Uri(pair.Key), new RepoRecord() { Channel = pair.Value.Channel });
            }).ToDict();
        }

        public async Task<PackageStatus> Status(PackageKey packageKey)
        {
            var response = await this.innerClient.StatusAsync(new StatusRequest() { PackageId = packageKey.ToString(), Target = 0 });
            return (PackageStatus)response.Value;
        }

        public async Task<Dictionary<Uri, LocalizationStrings>> Strings(string languageTag)
        {
            var response = await this.innerClient.StringsAsync(new StringsRequest() { Language = languageTag });
            return response.Repos.Map(pair =>
            {
                var uri = new Uri(pair.Key);
                var channels = pair.Value.Channels.Map(map => (map.Key, map.Value)).ToDict();
                var tags = pair.Value.Tags.Map(map => (map.Key, map.Value)).ToDict();
                var localizationStrings = new LocalizationStrings()
                {
                    Channels = channels,
                    Tags = tags,
                };

                return (uri, localizationStrings);
            }).ToDict();
        }

        private static SocketsHttpHandler CreateHttpHandler()
        {
            var connectionFactory = new NamedPipeConnectionFactory("pahkat");
            var socketsHttpHandler = new SocketsHttpHandler
            {
                ConnectCallback = connectionFactory.ConnectAsync
            };

            return socketsHttpHandler;
        }

        public void Dispose()
        {
            
        }
    }

    public class NamedPipeConnectionFactory
    {
        private readonly string endPoint;

        public NamedPipeConnectionFactory(string endPoint)
        {
            this.endPoint = endPoint;
        }

        public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _, CancellationToken cancellationToken = default)
        {
            var namedPipe = new NamedPipeClientStream(".", endPoint, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                await namedPipe.ConnectAsync(cancellationToken).ConfigureAwait(false);
                return namedPipe;
            }
            catch
            {
                namedPipe.Dispose();
                throw;
            }
        }
    }
}