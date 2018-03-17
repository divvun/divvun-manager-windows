using Newtonsoft.Json;
using Pahkat.Service;
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Serialization;

namespace Pahkat.Models
{
    public static class RepositoryChannelExtensions
    {
        public static string ToLocalisedName(this RepositoryMeta.Channel channel)
        {
            // TODO: localise
            return channel.Value();
            //switch (channel)
            //{
            //    case RepositoryMeta.Channel.Alpha:
            //        return Strings.alpha
            //}
        }
    }

    public partial class RepositoryMeta
    {
        public string NativeName
        {
            get
            {
                var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
                return Name.ContainsKey(tag) ? Name[tag] : Name["en"];
            }
        }
    }

    public partial class Package
    {
        public string NativeName
        {
            get
            {
                var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
                return Name.ContainsKey(tag) ? Name[tag] : Name["en"];
            }
        }

        public WindowsInstaller WindowsInstaller => Installer as WindowsInstaller;
    }

    public class Repository : IEquatable<Repository>, IComparable<Repository>
    {
        [JsonProperty("meta", Required = Required.Always)]
        public readonly RepositoryMeta Meta;
        private readonly PackagesMeta _packages;
        private readonly VirtualsMeta _virtuals;

        [JsonIgnore]
        public Dictionary<string, PackageStatus> Statuses;

        public Repository(RepositoryMeta repo, PackagesMeta packages, VirtualsMeta virtuals)
        {
            Meta = repo;
            _packages = packages;
            _virtuals = virtuals;

            Statuses = new Dictionary<string, PackageStatus>();
        }

        public Dictionary<string, Package> Packages => _packages.Packages;
        public Dictionary<string, string> Virtuals => _virtuals.Virtuals;

        public Uri PackageUrl(Package package)
        {
            return new Uri(_packages.Base, package.Id);
        }

        public PackageStatus PackageStatus(Package package)
        {
            return Statuses[package.Id];
        }

        public bool Equals(Repository other)
        {
            return Meta == other.Meta &&
                _packages == other._packages &&
                _virtuals == other._virtuals;
        }

        public int CompareTo(Repository other)
        {
            return Meta.NativeName.CompareTo(other.Meta.NativeName);
        }
    }
}
