using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Models;

namespace Divvun.Installer.Service
{
    public class MockPahkatClient : IPahkatClient
    {
        static public MockPahkatClient Create() {
            return new MockPahkatClient();
        }
        
        public CancellationTokenSource ProcessTransaction(PackageAction[] actions, Action<TransactionResponseValue> callback) {
            throw new NotImplementedException();
        }

        public PackageStatus Status(PackageKey packageKey) {
            throw new NotImplementedException();
        }

        public Dictionary<Uri, ILoadedRepository> RepoIndexes() {
            var o = new Dictionary<Uri, ILoadedRepository>();
            // o.Add(new Uri("https://test.repo/"), new RepoRecord());
            return o;
        }

        public Dictionary<Uri, RepoRecord> GetRepoRecords() {
            var o = new Dictionary<Uri, RepoRecord>();
            o.Add(new Uri("https://test.repo/"), new RepoRecord());
            return o;
        }

        public Dictionary<Uri, RepoRecord> SetRepo(Uri url, RepoRecord record) {
            throw new NotImplementedException();
        }

        public Dictionary<Uri, RepoRecord> RemoveRepo(Uri url) {
            throw new NotImplementedException();
        }

        public IObservable<Notification> Notifications() {
            return Observable.Empty<Notification>();
        }

        public Dictionary<Uri, LocalizationStrings> Strings(string languageTag) {
            throw new NotImplementedException();
        }

        public string ResolvePackageQuery(PackageQuery query) {
            throw new NotImplementedException();
        }
    }
}