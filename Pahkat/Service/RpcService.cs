using Pahkat.Models;
using Pahkat.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Newtonsoft.Json;
using System.Reactive.Concurrency;

namespace Pahkat.Service
{
    public struct PackageInstallResponse
    {
        [JsonProperty("status")]
        public PackageStatus Status;
    }

    public class RpcService : IDisposable
    {
        private ReactiveProcess _process;
        private Rpc.Client _client;

        public RpcService()
        {
            var appRoot = new Uri(System.Reflection.Assembly.GetEntryAssembly().CodeBase);
            var pahkatcPath = new Uri(appRoot, "pahkatc.exe").AbsolutePath;

            Console.WriteLine(pahkatcPath);

            _process = new ReactiveProcess(pahkatcPath, "ipc");
            _client = new Rpc.Client(_process.Output, _process.Input);
            _process.Start();
            //_process.Error.Subscribe(x => Console.WriteLine($"RPC[ERR] {x}"));
        }

        private IObservable<T> MakeRequest<T>(string method, object param)
        {
            return _client.Send<T>(new Rpc.Request
            {
                Method = method,
                Params = param
            });
        }

        public IObservable<Repository> Repository(Uri url, RepositoryMeta.Channel channel)
        {
            return MakeRequest<Repository>("repository", new[] { url.AbsoluteUri, channel.Value() });
        }

        public IObservable<Dictionary<string, PackageInstallResponse>> Statuses(Uri url)
        {
            return MakeRequest< Dictionary<string, PackageInstallResponse>>("repository_statuses", new[] { url.AbsoluteUri });
        }

        public void Dispose()
        {
            _process.ShamefullyKill();
            _client.Dispose();
        }
    }
}
