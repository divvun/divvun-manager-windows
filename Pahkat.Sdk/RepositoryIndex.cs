using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;


namespace Pahkat.Sdk
{
    public class RepoConfig
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }
        [JsonProperty("channel")]
        public RepositoryMeta.Channel Channel { get; set; }

        public RepoConfig(Uri url, RepositoryMeta.Channel channel)
        {
            Url = url;
            Channel = channel;
        }

    }

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

        [JsonIgnore]
        public Dictionary<AbsolutePackageKey, PackageStatusResponse> Statuses =
            new Dictionary<AbsolutePackageKey, PackageStatusResponse>();

        public Dictionary<string, Package> Packages => PackagesMeta.Packages;
        public Dictionary<string, string> Virtuals => VirtualsMeta.Virtuals;

        public Uri PackageUrl(Package package)
        {
            return new Uri(PackagesMeta.Base, package.Id);
        }

        [CanBeNull]
        public Package Package(AbsolutePackageKey key)
        {
            if (Meta.Base.ToString() != key.Url || Channel.Value() != key.Channel)
            {
                return null;
            }

            return Packages[key.Id];
        }

        [CanBeNull]
        public PackageStatusResponse PackageStatus(AbsolutePackageKey key)
        {
            return Statuses.Get(key, null);
        }

        [Obsolete("Use PackageStatus(AbsolutePackageKey)")]
        [CanBeNull]
        public PackageStatusResponse PackageStatus(Package package)
        {
            var key = Statuses.Keys.FirstOrDefault((x) => x.Id == package.Id);
            return key != null ? PackageStatus(key) : null;
        }

        public AbsolutePackageKey AbsoluteKeyFor(Package package)
        {
            var builder = new UriBuilder(Meta.Base);
            builder.Path += $"packages/{package.Id}";
            builder.Fragment = Channel.Value();
            return new AbsolutePackageKey(builder.Uri);
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

    //public static class RepositoryChannelExtensions
    //{
    //    public static string ToLocalisedName(this RepositoryMeta.Channel channel)
    //    {
    //        // TODO: localise
    //        return channel.Value();
    //        //switch (channel)
    //        //{
    //        //    case RepositoryMeta.Channel.Alpha:
    //        //        return Strings.alpha
    //        //}
    //    }
    //}

    //public partial class RepositoryMeta
    //{
    //    public string NativeName
    //    {
    //        get
    //        {
    //            var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
    //            return Name.ContainsKey(tag) ? Name[tag] : Name["en"];
    //        }
    //    }
    //}

    //public partial class Package
    //{
    //    public string NativeName
    //    {
    //        get
    //        {
    //            var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
    //            return Name.ContainsKey(tag) ? Name[tag] : Name["en"];
    //        }
    //    }

    //    public WindowsInstaller WindowsInstaller => Installer as WindowsInstaller;
    //}

    //public class RepositoryIndex : IEquatable<RepositoryIndex>, IComparable<RepositoryIndex>
    //{
    //    [JsonProperty("meta", Required = Required.Always)]
    //    public readonly RepositoryMeta Meta;

    //    [JsonProperty("channel", Required = Required.Always)]
    //    public readonly RepositoryMeta.Channel Channel;

    //    [JsonProperty("packages", Required = Required.Always)]
    //    public readonly PackagesMeta PackagesMeta;

    //    [JsonProperty("virtuals", Required = Required.Always)]
    //    public readonly VirtualsMeta VirtualsMeta;

    //    [JsonIgnore]
    //    public Dictionary<AbsolutePackageKey, PackageStatusResponse> Statuses =
    //        new Dictionary<AbsolutePackageKey, PackageStatusResponse>();

    //    public Dictionary<string, Package> Packages => PackagesMeta.Packages;
    //    public Dictionary<string, string> Virtuals => VirtualsMeta.Virtuals;

    //    public Uri PackageUrl(Package package)
    //    {
    //        return new Uri(PackagesMeta.Base, package.Id);
    //    }

    //    [CanBeNull]
    //    public Package Package(AbsolutePackageKey key)
    //    {
    //        if (Meta.Base.ToString() != key.Url || Channel.Value() != key.Channel)
    //        {
    //            return null;
    //        }

    //        return Packages[key.Id];
    //    }

    //    [CanBeNull]
    //    public PackageStatusResponse PackageStatus(AbsolutePackageKey key)
    //    {
    //        return Statuses.Get(key, null);
    //    }

    //    [Obsolete("Use PackageStatus(AbsolutePackageKey)")]
    //    [CanBeNull]
    //    public PackageStatusResponse PackageStatus(Package package)
    //    {
    //        var key = Statuses.Keys.FirstOrDefault((x) => x.Id == package.Id);
    //        return key != null ? PackageStatus(key) : null;
    //    }

    //    public AbsolutePackageKey AbsoluteKeyFor(Package package)
    //    {
    //        var builder = new UriBuilder(Meta.Base);
    //        builder.Path += $"packages/{package.Id}";
    //        builder.Fragment = Channel.Value();
    //        return new AbsolutePackageKey(builder.Uri);
    //    }

    //    public bool Equals(RepositoryIndex other)
    //    {
    //        return other != null && Meta.Equals(other.Meta) &&
    //               PackagesMeta.Equals(other.PackagesMeta) &&
    //               VirtualsMeta.Equals(other.VirtualsMeta) &&
    //               Channel == other.Channel;
    //    }

    //    public int CompareTo(RepositoryIndex other)
    //    {
    //        return Meta.NativeName.CompareTo(other.Meta.NativeName);
    //    }
    //}
}

