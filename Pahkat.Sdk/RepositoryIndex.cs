using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace Pahkat.Sdk
{
    public class RepositoryIndex : IEquatable<RepositoryIndex>, IComparable<RepositoryIndex>
    {
        [JsonProperty("meta", Required = Required.Always)]
        public readonly RepositoryMeta Meta;

        [JsonProperty("channel", Required = Required.Always)]
        public readonly RepositoryMeta.Channel Channel;

        [JsonProperty("packages", Required = Required.Always)]
        public readonly PackagesMeta PackagesMeta;

        [JsonProperty("virtuals", Required = Required.Always)]
        public readonly VirtualsMeta VirtualsMeta;

        public Dictionary<string, Package> Packages => PackagesMeta.Packages;
        public Dictionary<string, string> Virtuals => VirtualsMeta.Virtuals;

        public Uri PackageUrl(Package package)
        {
            return new Uri(PackagesMeta.Base, package.Id);
        }
        
        public Package? Package(PackageKey key)
        {
            if (Meta.Base.ToString() != key.BaseUrl || Channel.Value() != key.Channel)
            {
                return null;
            }

            return Packages[key.Id];
        }

        public PackageKey PackageKeyFor(Package package)
        {
            var builder = new UriBuilder(Meta.Base);
            builder.Path += $"packages/{package.Id}";
            builder.Fragment = Channel.Value();
            return PackageKey.New(builder.Uri);
        }

        public bool Equals(RepositoryIndex other)
        {
            return other != null && Meta.Equals(other.Meta) &&
                PackagesMeta.Equals(other.PackagesMeta) &&
                VirtualsMeta.Equals(other.VirtualsMeta) &&
                Channel == other.Channel;
        }

        public int CompareTo(RepositoryIndex other)
        {
            return Meta.NativeName.CompareTo(other.Meta.NativeName);
        }
    }
}

