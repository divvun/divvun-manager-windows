using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Iterable;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;
using Pahkat.Sdk.Rpc.Models;

namespace Divvun.Installer.Service
{
    internal class MockWindowsExecutable : IWindowsExecutable
    {
        public ulong Size { get; } = 1;
    }

    internal class MockTarget : ITarget
    {
        public string Platform { get; }
        public Payload PayloadType { get; }
        
        public IWindowsExecutable Payload { get; }

        internal MockTarget() {
            this.Platform = "windows";
            this.PayloadType = Pahkat.Sdk.Rpc.Fbs.Payload.WindowsExecutable;
            this.Payload = new MockWindowsExecutable();
        }
    }
    
    internal class MockRelease : IRelease
    {
        public string? Channel { get; }
        public string? Version { get; }
        public IReadOnlyList<string> Authors { get; } = new List<string>();
        public IReadOnlyList<ITarget?> Target { get; } = new List<ITarget?>();
        // public ITarget? WindowsTarget { get; }
        // public IWindowsExecutable? WindowsExecutable { get; }

        internal MockRelease() {
            Version = "0.0.0";
            var t = new List<ITarget?>();
            t.Add(new MockTarget());
            Target = t;
        }
    }
    
    internal class MockDescriptor : IDescriptor
    {
        public string Id { get; set; }
        public IReadOnlyDictionary<string, string> Name { get; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Description { get; } = new Dictionary<string, string>();
        public IReadOnlyList<string> Tags { get; set; } = new List<string>();
        public IReadOnlyList<IRelease?> Release { get; set; } = new List<IRelease?>();

        internal MockDescriptor(string id, string name, string[] tags) {
            Id = id;
            var nameDict = new Dictionary<string, string>();
            nameDict.Add("en", name);
            Name = nameDict;
            Tags = tags;
            
        }
        
        internal MockDescriptor(string id) {
            Id = id;
            var rel = new List<IRelease?>();
            rel.Add(new MockRelease());
            Release = rel;
        }
    }
    
    internal class MockPackages : IPackages
    {
        public IReadOnlyDictionary<string, IDescriptor?> Packages { get; }

        internal MockPackages() {
            var list = new Dictionary<string, IDescriptor?>();
            for (int i = 1; i <= 5; i++) {
                var id = $"test{i}";
                list.Add(id, new MockDescriptor(id));
            }
            
            this.Packages = list;
        }
    }
    
    internal class MockLoadedRepository : ILoadedRepository
    {
        public LoadedRepository.IndexValue Index { get; } = new LoadedRepository.IndexValue {
            Agent = new LoadedRepository.IndexValue.AgentValue {
                Name = "Mock",
                Version = "0.0.0"
            },
            Channels = new []{ "nightly" },
            Description = new Dictionary<string, string>(),
            Name = new Dictionary<string, string>(),
            Url = new Uri("https://test.repo/")
        };

        public LoadedRepository.MetaValue Meta { get; } = new LoadedRepository.MetaValue();
        public IPackages Packages { get; } = new MockPackages();
        
        public PackageKey PackageKey(IDescriptor descriptor) {
            throw new NotImplementedException();
        }
    }
    
    public class MockPahkatClient : IPahkatClient
    {
        public static MockPahkatClient Create() {
            return new MockPahkatClient();
        }
        
        public Task<CancellationTokenSource> ProcessTransaction(PackageAction[] actions, Action<TransactionResponseValue> callback)
        {
            return Task.Run(() =>
            {
                Scheduler.NewThread.Schedule(() =>
                {
                    callback(new TransactionResponseValue.TransactionStarted
                    {
                        Actions = actions.Map(x => new ResolvedAction()
                        {
                            Action = x,
                            Name = new Dictionary<string, string>(),
                            Version = "0.0.0"
                        }).ToArray(),
                        IsRebootRequired = true,
                    });
                    Thread.Sleep(100);
                    callback(new TransactionResponseValue.DownloadProgress
                    {
                        PackageKey = actions[0].PackageKey,
                        Current = 0,
                        Total = 1000000,
                    });
                    Thread.Sleep(100);
                    callback(new TransactionResponseValue.DownloadProgress
                    {
                        PackageKey = actions[0].PackageKey,
                        Current = 100,
                        Total = 1000000,
                    });
                    Thread.Sleep(100);
                    callback(new TransactionResponseValue.DownloadProgress
                    {
                        PackageKey = actions[0].PackageKey,
                        Current = 1000,
                        Total = 1000000,
                    });
                    Thread.Sleep(1000);
                    callback(new TransactionResponseValue.DownloadProgress
                    {
                        PackageKey = actions[0].PackageKey,
                        Current = 5000,
                        Total = 1000000,
                    });
                    Thread.Sleep(500);
                    callback(new TransactionResponseValue.DownloadProgress
                    {
                        PackageKey = actions[0].PackageKey,
                        Current = 50000,
                        Total = 1000000,
                    });
                    Thread.Sleep(500);
                    callback(new TransactionResponseValue.DownloadProgress
                    {
                        PackageKey = actions[0].PackageKey,
                        Current = 500000,
                        Total = 1000000,
                    });
                    // for (ulong i = 500000; i < 1000000; i += 1000) {
                    //     callback(new TransactionResponseValue.DownloadProgress {
                    //         PackageKey = actions[0].PackageKey,
                    //         Current = i,
                    //         Total = 1000000,
                    //     });
                    //     Thread.Sleep(200);
                    // }
                    callback(new TransactionResponseValue.DownloadComplete
                    {
                        PackageKey = actions[0].PackageKey,
                    });
                    Thread.Sleep(1000);
                    callback(new TransactionResponseValue.InstallStarted
                    {
                        PackageKey = actions[0].PackageKey,
                    });
                    callback(new TransactionResponseValue.TransactionComplete
                    {

                    });
                });

                return new CancellationTokenSource();
            });
        }

        public Task<PackageStatus> Status(PackageKey packageKey) {
            return Task.Run(() => PackageStatus.NotInstalled);
        }

        public Task<Dictionary<Uri, ILoadedRepository>> RepoIndexes()
        {
            return Task.Run(() =>
            {
                var o = new Dictionary<Uri, ILoadedRepository>();
                o.Add(new Uri("https://test.repo/"), new MockLoadedRepository());
                return o;

            });
        }

        public Task<Dictionary<Uri, RepoRecord>> GetRepoRecords()
        {
            return Task.Run(() =>
            {
                var o = new Dictionary<Uri, RepoRecord>();
                o.Add(new Uri("https://test.repo/"), new RepoRecord());
                return o;
            });
        }

        public Task<Dictionary<Uri, RepoRecord>> SetRepo(Uri url, RepoRecord record)
        {
            return Task.Run(() =>
            {
                throw new NotImplementedException();
                return new Dictionary<Uri, RepoRecord>();
            });
        }

        public Task<Dictionary<Uri, RepoRecord>> RemoveRepo(Uri url)
        {
            return Task.Run(() =>
            {
                throw new NotImplementedException();
                return new Dictionary<Uri, RepoRecord>();
            });
        }

        public IObservable<Notification> Notifications()
        {
           return Observable.Empty<Notification>();
        }

        public Task<Dictionary<Uri, LocalizationStrings>> Strings(string languageTag)
        {
            return Task.Run(() => { return new Dictionary<Uri, LocalizationStrings>(); });
        }

        public Task<string> ResolvePackageQuery(PackageQuery query)
        {
            return Task.Run(() => {
                throw new NotImplementedException();
                return ""; 
            });
        }
    }
}