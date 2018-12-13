using JsonSubTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MergeCastWithTypeCheck

namespace Pahkat.Models
{
    public enum LinkedDataType
    {
        [EnumMember(Value = "Repository")]
        Repository,
        [EnumMember(Value = "RepositoryAgent")]
        RepositoryAgent,
        [EnumMember(Value = "Packages")]
        Packages,
        [EnumMember(Value = "Package")]
        Package,
        [EnumMember(Value = "Virtuals")]
        Virtuals,
        [EnumMember(Value = "Virtual")]
        Virtual,
        [EnumMember(Value = "WindowsInstaller")]
        WindowsInstaller,
        [EnumMember(Value = "MacOSInstaller")]
        MacOsInstaller,
        [EnumMember(Value = "TarballInstaller")]
        TarballInstaller
    }

    public static class LinkedDataTypeExtensions
    {
        public static string Value(this LinkedDataType instance)
        {
            switch (instance)
            {
                case LinkedDataType.Repository:
                    return "Repository";
                case LinkedDataType.RepositoryAgent:
                    return "RepositoryAgent";
                case LinkedDataType.Packages:
                    return "Packages";
                case LinkedDataType.Package:
                    return "Package";
                case LinkedDataType.Virtuals:
                    return "Virtuals";
                case LinkedDataType.Virtual:
                    return "Virtual";
                case LinkedDataType.WindowsInstaller:
                    return "WindowsInstaller";
                case LinkedDataType.MacOsInstaller:
                    return "MacOSInstaller";
                case LinkedDataType.TarballInstaller:
                    return "TarballInstaller";
                default:
                    return null;
            }
        }
    }

    public partial class RepositoryMeta : IEquatable<RepositoryMeta>
    {
        [JsonProperty("@type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LinkedDataType _Type { get; private set; }
        [JsonProperty("agent")]
        public RepositoryAgent Agent { get; private set; }
        [JsonProperty("base", Required = Required.Always)]
        public Uri Base { get; private set; }
        [JsonProperty("name", Required = Required.Always)]
        public Dictionary<string, string> Name { get; private set; }
        [JsonProperty("description", Required = Required.Always)]
        public Dictionary<string, string> Description { get; private set; }
        [JsonProperty("primaryFilter", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Filter PrimaryFilter { get; private set; }
        [JsonProperty("channels", Required = Required.Always)]
        public List<Channel> Channels { get; private set; }
        [JsonProperty("categories")]
        public Dictionary<string, Dictionary<string, string>> Categories { get; private set; }

        public enum Filter
        {
            [EnumMember(Value = "category")]
            Category,
            [EnumMember(Value = "language")]
            Language
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum Channel
        {
            [EnumMember(Value = "stable")]
            Stable,
            [EnumMember(Value = "beta")]
            Beta,
            [EnumMember(Value = "alpha")]
            Alpha,
            [EnumMember(Value = "nightly")]
            Nightly
        }

        public bool Equals(RepositoryMeta other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(_Type, other._Type)) return false;
            if (!Equals(Agent, other.Agent)) return false;
            if (!Equals(Base, other.Base)) return false;
            if (!Equals(Name, other.Name)) return false;
            if (!Equals(Description, other.Description)) return false;
            if (!Equals(PrimaryFilter, other.PrimaryFilter)) return false;
            if (!Equals(Channels, other.Channels)) return false;
            if (!Equals(Categories, other.Categories)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RepositoryMeta)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ _Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Agent != null ? Agent.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Base != null ? Base.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ PrimaryFilter.GetHashCode();
                hashCode = (hashCode * 397) ^ (Channels != null ? Channels.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Categories != null ? Categories.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public static class RepositoryMetaFilterExtensions
    {
        public static string Value(this RepositoryMeta.Filter instance)
        {
            switch (instance)
            {
                case RepositoryMeta.Filter.Category:
                    return "category";
                case RepositoryMeta.Filter.Language:
                    return "language";
                default:
                    return null;
            }
        }
    }

    public static class RepositoryMetaChannelExtensions
    {
        public static string Value(this RepositoryMeta.Channel instance)
        {
            switch (instance)
            {
                case RepositoryMeta.Channel.Stable:
                    return "stable";
                case RepositoryMeta.Channel.Beta:
                    return "beta";
                case RepositoryMeta.Channel.Alpha:
                    return "alpha";
                case RepositoryMeta.Channel.Nightly:
                    return "nightly";
                default:
                    return null;
            }
        }
    }

    public partial class RepositoryAgent : IEquatable<RepositoryAgent>
    {
        [JsonProperty("@type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LinkedDataType _Type { get; private set; }
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; private set; }
        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; private set; }
        [JsonProperty("url")]
        public Uri Url { get; private set; }

        public bool Equals(RepositoryAgent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(_Type, other._Type)) return false;
            if (!Equals(Name, other.Name)) return false;
            if (!Equals(Version, other.Version)) return false;
            if (!Equals(Url, other.Url)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RepositoryAgent)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ _Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Url != null ? Url.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public partial class PackagesMeta : IEquatable<PackagesMeta>
    {
        [JsonProperty("@type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LinkedDataType _Type { get; private set; }
        [JsonProperty("base", Required = Required.Always)]
        public Uri Base { get; private set; }
        [JsonProperty("packages", Required = Required.Always)]
        public Dictionary<string, Package> Packages { get; private set; }

        public bool Equals(PackagesMeta other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(_Type, other._Type)) return false;
            if (!Equals(Base, other.Base)) return false;
            if (!Equals(Packages, other.Packages)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackagesMeta)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ _Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Base != null ? Base.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Packages != null ? Packages.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public partial class Package : IEquatable<Package>
    {
        [JsonProperty("@type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LinkedDataType _Type { get; private set; }
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; private set; }
        [JsonProperty("name", Required = Required.Always)]
        public Dictionary<string, string> Name { get; private set; }
        [JsonProperty("description", Required = Required.Always)]
        public Dictionary<string, string> Description { get; private set; }
        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; private set; }
        [JsonProperty("category", Required = Required.Always)]
        public string Category { get; private set; }
        [JsonProperty("languages", Required = Required.Always)]
        public List<string> Languages { get; private set; }
        [JsonProperty("platform", Required = Required.Always)]
        public Dictionary<string, string> Platform { get; private set; }
        [JsonProperty("dependencies", Required = Required.Always)]
        public Dictionary<string, string> Dependencies { get; private set; }
        [JsonProperty("virtualDependencies", Required = Required.Always)]
        public Dictionary<string, string> VirtualDependencies { get; private set; }
        [JsonProperty("installer", Required = Required.Always)]
        public IInstaller Installer { get; private set; }

        [JsonConverter(typeof(JsonSubtypes), "@type")]
        [JsonSubtypes.KnownSubType(typeof(WindowsInstaller), "WindowsInstaller")]
        [JsonSubtypes.KnownSubType(typeof(MacOsInstaller), "MacOSInstaller")]
        [JsonSubtypes.KnownSubType(typeof(TarballInstaller), "TarballInstaller")]
        public interface IInstaller
        {
            LinkedDataType _Type { get; }
        }

        public bool Equals(Package other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(_Type, other._Type)) return false;
            if (!Equals(Id, other.Id)) return false;
            if (!Equals(Name, other.Name)) return false;
            if (!Equals(Description, other.Description)) return false;
            if (!Equals(Version, other.Version)) return false;
            if (!Equals(Category, other.Category)) return false;
            if (!Equals(Languages, other.Languages)) return false;
            if (!Equals(Platform, other.Platform)) return false;
            if (!Equals(Dependencies, other.Dependencies)) return false;
            if (!Equals(VirtualDependencies, other.VirtualDependencies)) return false;
            if (!Equals(Installer, other.Installer)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Package)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ _Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Category != null ? Category.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Languages != null ? Languages.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Platform != null ? Platform.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Dependencies != null ? Dependencies.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (VirtualDependencies != null ? VirtualDependencies.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Installer != null ? Installer.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public partial class TarballInstaller : IEquatable<TarballInstaller>, Package.IInstaller
    {
        [JsonProperty("@type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LinkedDataType _Type { get; private set; }
        [JsonProperty("url", Required = Required.Always)]
        public Uri Url { get; private set; }
        [JsonProperty("size", Required = Required.Always)]
        public long Size { get; private set; }
        [JsonProperty("installedSize", Required = Required.Always)]
        public long InstalledSize { get; private set; }

        public bool Equals(TarballInstaller other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(_Type, other._Type)) return false;
            if (!Equals(Url, other.Url)) return false;
            if (!Equals(Size, other.Size)) return false;
            if (!Equals(InstalledSize, other.InstalledSize)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TarballInstaller)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ _Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ InstalledSize.GetHashCode();
                return hashCode;
            }
        }
    }

    public partial class WindowsInstaller : IEquatable<WindowsInstaller>, Package.IInstaller
    {
        [JsonProperty("@type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LinkedDataType _Type { get; private set; }
        [JsonProperty("url", Required = Required.Always)]
        public Uri Url { get; private set; }
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public InstallerType Type { get; private set; }
        [JsonProperty("args")]
        public string Args { get; private set; }
        [JsonProperty("uninstallArgs")]
        public string UninstallArgs { get; private set; }
        [JsonProperty("productCode", Required = Required.Always)]
        public string ProductCode { get; private set; }
        [JsonProperty("requiresReboot", Required = Required.Always)]
        public bool RequiresReboot { get; private set; }
        [JsonProperty("requiresUninstallReboot", Required = Required.Always)]
        public bool RequiresUninstallReboot { get; private set; }
        [JsonProperty("size", Required = Required.Always)]
        public long Size { get; private set; }
        [JsonProperty("installedSize", Required = Required.Always)]
        public long InstalledSize { get; private set; }

        public enum InstallerType
        {
            [EnumMember(Value = "msi")]
            Msi,
            [EnumMember(Value = "inno")]
            Inno,
            [EnumMember(Value = "nsis")]
            Nsis
        }

        public bool Equals(WindowsInstaller other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(_Type, other._Type)) return false;
            if (!Equals(Url, other.Url)) return false;
            if (!Equals(Type, other.Type)) return false;
            if (!Equals(Args, other.Args)) return false;
            if (!Equals(UninstallArgs, other.UninstallArgs)) return false;
            if (!Equals(ProductCode, other.ProductCode)) return false;
            if (!Equals(RequiresReboot, other.RequiresReboot)) return false;
            if (!Equals(RequiresUninstallReboot, other.RequiresUninstallReboot)) return false;
            if (!Equals(Size, other.Size)) return false;
            if (!Equals(InstalledSize, other.InstalledSize)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WindowsInstaller)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ _Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Args != null ? Args.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (UninstallArgs != null ? UninstallArgs.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProductCode != null ? ProductCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ RequiresReboot.GetHashCode();
                hashCode = (hashCode * 397) ^ RequiresUninstallReboot.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ InstalledSize.GetHashCode();
                return hashCode;
            }
        }
    }

    public static class WindowsInstallerInstallerTypeExtensions
    {
        public static string Value(this WindowsInstaller.InstallerType instance)
        {
            switch (instance)
            {
                case WindowsInstaller.InstallerType.Msi:
                    return "msi";
                case WindowsInstaller.InstallerType.Inno:
                    return "inno";
                case WindowsInstaller.InstallerType.Nsis:
                    return "nsis";
                default:
                    return null;
            }
        }
    }

    public partial class MacOsInstaller : IEquatable<MacOsInstaller>, Package.IInstaller
    {
        [JsonProperty("@type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LinkedDataType _Type { get; private set; }
        [JsonProperty("url", Required = Required.Always)]
        public Uri Url { get; private set; }
        [JsonProperty("pkgId", Required = Required.Always)]
        public string PkgId { get; private set; }
        [JsonProperty("targets", Required = Required.Always)]
        public List<Target> Targets { get; private set; }
        [JsonProperty("requiresReboot", Required = Required.Always)]
        public bool RequiresReboot { get; private set; }
        [JsonProperty("requiresUninstallReboot", Required = Required.Always)]
        public bool RequiresUninstallReboot { get; private set; }
        [JsonProperty("size", Required = Required.Always)]
        public long Size { get; private set; }
        [JsonProperty("installedSize", Required = Required.Always)]
        public long InstalledSize { get; private set; }

        public enum Target
        {
            [EnumMember(Value = "system")]
            System,
            [EnumMember(Value = "user")]
            User
        }

        public bool Equals(MacOsInstaller other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(_Type, other._Type)) return false;
            if (!Equals(Url, other.Url)) return false;
            if (!Equals(PkgId, other.PkgId)) return false;
            if (!Equals(Targets, other.Targets)) return false;
            if (!Equals(RequiresReboot, other.RequiresReboot)) return false;
            if (!Equals(RequiresUninstallReboot, other.RequiresUninstallReboot)) return false;
            if (!Equals(Size, other.Size)) return false;
            if (!Equals(InstalledSize, other.InstalledSize)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MacOsInstaller)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ _Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PkgId != null ? PkgId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Targets != null ? Targets.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ RequiresReboot.GetHashCode();
                hashCode = (hashCode * 397) ^ RequiresUninstallReboot.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ InstalledSize.GetHashCode();
                return hashCode;
            }
        }
    }

    public static class MacOsInstallerTargetExtensions
    {
        public static string Value(this MacOsInstaller.Target instance)
        {
            switch (instance)
            {
                case MacOsInstaller.Target.System:
                    return "system";
                case MacOsInstaller.Target.User:
                    return "user";
                default:
                    return null;
            }
        }
    }

    public partial class VirtualsMeta : IEquatable<VirtualsMeta>
    {
        [JsonProperty("@type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LinkedDataType _Type { get; private set; }
        [JsonProperty("base", Required = Required.Always)]
        public Uri Base { get; private set; }
        [JsonProperty("virtuals", Required = Required.Always)]
        public Dictionary<string, string> Virtuals { get; private set; }

        public bool Equals(VirtualsMeta other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(_Type, other._Type)) return false;
            if (!Equals(Base, other.Base)) return false;
            if (!Equals(Virtuals, other.Virtuals)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VirtualsMeta)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ _Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (Base != null ? Base.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Virtuals != null ? Virtuals.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public partial class Virtual : IEquatable<Virtual>
    {
        [JsonProperty("@type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LinkedDataType _Type { get; private set; }
        [JsonProperty("virtual", Required = Required.Always)]
        public bool IsVirtual { get; private set; }
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; private set; }
        [JsonProperty("name", Required = Required.Always)]
        public Dictionary<string, string> Name { get; private set; }
        [JsonProperty("description", Required = Required.Always)]
        public Dictionary<string, string> Description { get; private set; }
        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; private set; }
        [JsonProperty("url", Required = Required.Always)]
        public Uri Url { get; private set; }
        [JsonProperty("target", Required = Required.Always)]
        public VirtualTarget Target { get; private set; }

        public bool Equals(Virtual other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(_Type, other._Type)) return false;
            if (!Equals(IsVirtual, other.IsVirtual)) return false;
            if (!Equals(Id, other.Id)) return false;
            if (!Equals(Name, other.Name)) return false;
            if (!Equals(Description, other.Description)) return false;
            if (!Equals(Version, other.Version)) return false;
            if (!Equals(Url, other.Url)) return false;
            if (!Equals(Target, other.Target)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Virtual)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ _Type.GetHashCode();
                hashCode = (hashCode * 397) ^ IsVirtual.GetHashCode();
                hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Target != null ? Target.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public partial class VirtualTarget : IEquatable<VirtualTarget>
    {
        [JsonProperty("registryKey", Required = Required.Always)]
        public RegistryKey RegistryKey { get; private set; }

        public bool Equals(VirtualTarget other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(RegistryKey, other.RegistryKey)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VirtualTarget)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ (RegistryKey != null ? RegistryKey.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public partial class RegistryKey : IEquatable<RegistryKey>
    {
        [JsonProperty("path", Required = Required.Always)]
        public string Path { get; private set; }
        [JsonProperty("name")]
        public string Name { get; private set; }
        [JsonProperty("value")]
        public string Value { get; private set; }
        [JsonProperty("kind")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ValueKind Kind { get; private set; }

        public enum ValueKind
        {
            [EnumMember(Value = "string")]
            String,
            [EnumMember(Value = "dword")]
            Dword,
            [EnumMember(Value = "qword")]
            Qword,
            [EnumMember(Value = "etc")]
            Etc
        }

        public bool Equals(RegistryKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(Path, other.Path)) return false;
            if (!Equals(Name, other.Name)) return false;
            if (!Equals(Value, other.Value)) return false;
            if (!Equals(Kind, other.Kind)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RegistryKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0;
                hashCode = (hashCode * 397) ^ (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Kind.GetHashCode();
                return hashCode;
            }
        }
    }

    public static class RegistryKeyValueKindExtensions
    {
        public static string Value(this RegistryKey.ValueKind instance)
        {
            switch (instance)
            {
                case RegistryKey.ValueKind.String:
                    return "string";
                case RegistryKey.ValueKind.Dword:
                    return "dword";
                case RegistryKey.ValueKind.Qword:
                    return "qword";
                case RegistryKey.ValueKind.Etc:
                    return "etc";
                default:
                    return null;
            }
        }
    }
}
