using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MergeCastWithTypeCheck

namespace Bahkat.Models
{
    public class RepoIndex : IEquatable<RepoIndex>
    {
        public Uri Base;
        public Dictionary<string, string> Name;
        public Dictionary<string, string> Description;
        public string PrimaryFilter;
        public List<string> Channels;

        public RepoIndex(Uri @base, Dictionary<string, string> name, Dictionary<string, string> description,
            string primaryFilter, List<string> channels)
        {
            Base = @base;
            Name = name;
            Description = description;
            PrimaryFilter = primaryFilter;
            Channels = channels;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public bool Equals(RepoIndex other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Base, other.Base) && Equals(Name, other.Name) && Equals(Description, other.Description) &&
                   string.Equals(PrimaryFilter, other.PrimaryFilter) && Equals(Channels, other.Channels);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RepoIndex) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Base != null ? Base.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PrimaryFilter != null ? PrimaryFilter.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Channels != null ? Channels.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class PackageIndexInstallerSignature
    {
    }

    public class PackageInstaller : IEquatable<PackageInstaller>
    {
        public Uri Url;
        public string SilentArgs;
        public string ProductCode;
        public bool RequiresReboot;
        public long Size;
        public long InstalledSize;
        public PackageIndexInstallerSignature Signature;

        public bool Equals(PackageInstaller other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Url, other.Url) && string.Equals(SilentArgs, other.SilentArgs) &&
                   string.Equals(ProductCode, other.ProductCode) && RequiresReboot == other.RequiresReboot &&
                   Size == other.Size && InstalledSize == other.InstalledSize && Equals(Signature, other.Signature);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackageInstaller) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SilentArgs != null ? SilentArgs.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProductCode != null ? ProductCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ RequiresReboot.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ InstalledSize.GetHashCode();
                hashCode = (hashCode * 397) ^ (Signature != null ? Signature.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Package : IEquatable<Package>
    {
        public string Id;
        public Dictionary<string, string> Name;
        public Dictionary<string, string> Description;
        public string Version;
        public string Category;
        public List<string> Languages;
        public Dictionary<string, string> Os;
        public Dictionary<string, string> Dependencies;
        public Dictionary<string, string> VirtualDependencies;
        public PackageInstaller Installer;

        public string NativeName
        {
            get
            {
                var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
                return Name.ContainsKey(tag) ? Name[tag] : Name["en"];
            }
        }

        public bool Equals(Package other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            // This is bad but will do. Everything can be different if ID is the same
            return string.Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Package) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (Category != null ? Category.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (Languages != null ? Languages.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (Os != null ? Os.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (Dependencies != null ? Dependencies.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (VirtualDependencies != null ? VirtualDependencies.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (Installer != null ? Installer.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class VirtualIndexTarget
    {
    }

    public class VirtualIndex : IEquatable<VirtualIndex>
    {
        public string Id;
        public Dictionary<string, string> Name;
        public Dictionary<string, string> Description;
        public string Version;
        public Uri Url;
        public VirtualIndexTarget Target;

        public bool Equals(VirtualIndex other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && Equals(Name, other.Name) && Equals(Description, other.Description) &&
                   string.Equals(Version, other.Version) && Equals(Url, other.Url) && Equals(Target, other.Target);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VirtualIndex) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Target != null ? Target.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Repository : IEquatable<Repository>
    {
        public RepoIndex Meta;
        public Dictionary<string, Package> PackagesIndex;
        public Dictionary<string, List<string>> VirtualsIndex;

        public Repository(RepoIndex meta, Dictionary<string, Package> packagesIndex,
            Dictionary<string, List<string>> virtualsIndex)
        {
            Meta = meta;
            PackagesIndex = packagesIndex;
            VirtualsIndex = virtualsIndex;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public bool Equals(Repository other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Meta, other.Meta) && Equals(PackagesIndex, other.PackagesIndex) &&
                   Equals(VirtualsIndex, other.VirtualsIndex);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Repository) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Meta != null ? Meta.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PackagesIndex != null ? PackagesIndex.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (VirtualsIndex != null ? VirtualsIndex.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}