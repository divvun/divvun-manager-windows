using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Pahkat.Sdk.Rpc.Example
{
    class Program
    {
        static void Main(string[] args) {
            var settings = Json.Settings.Value;
            var foo = JsonConvert.DeserializeObject<TransactionResponseValue>("{\"type\":\"DownloadComplete\",\"package_id\":\"https://package.tld/ohno/packages/foo?channel=lol\"}", settings);
            foo.Switch(
                x => { },
                x => {
                    Console.WriteLine("HEno");
                    Console.WriteLine(x.PackageKey.RepositoryUrl);
                    Console.WriteLine(x.PackageKey.Id);
                    Console.WriteLine(x.PackageKey.Params);
                    Console.WriteLine(x.PackageKey.ToString());
                },
                x => { },
                x => { },
                x => { },
                x => { },
                x => { },
                x => { });
            Console.WriteLine(foo);

            // var client = PahkatClient.Create();
            //
            // client.RepoIndexes();
        }
    }
}