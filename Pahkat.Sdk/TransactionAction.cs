using Newtonsoft.Json;
using Pahkat.Sdk.Native;
using System;
using System.Collections.Generic;
using static Pahkat.Sdk.PahkatClientException;

namespace Pahkat.Sdk
{
    public class TransactionAction
    {
        [JsonProperty("id")]
        public AbsolutePackageKey Id { get; internal set; }
        [JsonProperty("action")]
        public PackageAction Action { get; internal set; }
        [JsonProperty("target")]
        public PackageTarget Target { get; internal set; }

        public static TransactionAction Install(AbsolutePackageKey key, PackageTarget target)
        {
            return new TransactionAction
            {
                Id = key,
                Action = PackageAction.Install,
                Target = target
            };
        }

        public static TransactionAction Uninstall(AbsolutePackageKey key, PackageTarget target)
        {
            return new TransactionAction
            {
                Id = key,
                Action = PackageAction.Uninstall,
                Target = target
            };
        }
    }
}
