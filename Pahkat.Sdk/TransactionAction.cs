using Newtonsoft.Json;
using Pahkat.Sdk.Native;
using System;
using System.Collections.Generic;
using static Pahkat.Sdk.PahkatClientException;

namespace Pahkat.Sdk
{
    public class TransactionAction
    {
        [JsonProperty("id", Required = Required.Always)]
        public PackageKey Id { get; internal set; }
        [JsonProperty("action", Required = Required.Always)]
        public PackageAction Action { get; internal set; }
        [JsonProperty("target", Required = Required.Always)]
        public PackageTarget Target { get; internal set; }

        public static TransactionAction Install(PackageKey key, PackageTarget target)
        {
            return new TransactionAction
            {
                Id = key,
                Action = PackageAction.Install,
                Target = target
            };
        }

        public static TransactionAction Uninstall(PackageKey key, PackageTarget target)
        {
            return new TransactionAction
            {
                Id = key,
                Action = PackageAction.Uninstall,
                Target = target
            };
        }

        public static TransactionAction New(PackageAction action, PackageKey key, PackageTarget target)
        {
            return new TransactionAction
            {
                Id = key,
                Action = action,
                Target = target
            };
        }
    }
}
