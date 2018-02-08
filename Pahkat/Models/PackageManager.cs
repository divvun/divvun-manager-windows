using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MergeCastWithTypeCheck

namespace Pahkat.Models
{
    public class RepoAgent : IEquatable<RepoAgent>
    {
        public string Name;
        public string Version;
        public string Url;

        public bool Equals(RepoAgent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && string.Equals(Version, other.Version) && string.Equals(Url, other.Url);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RepoAgent) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Url != null ? Url.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
    
    public interface IValidatable<out T>
    {
        T Validate();
    }
    
    public class RepoIndex : IEquatable<RepoIndex>, IValidatable<RepoIndex>
    {
        public RepoAgent Agent;

        [JsonProperty(Required = Required.Always)]
        public Uri Base;

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, string> Name;

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, string> Description;

        [JsonProperty(Required = Required.Always)]
        public string PrimaryFilter;

        [JsonProperty(Required = Required.Always)]
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

        public string NativeName
        {
            get
            {
                var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
                return Name.ContainsKey(tag) ? Name[tag] : Name["en"];
            }
        }

        public bool Equals(RepoIndex other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Base, other.Base) && Equals(Name, other.Name) && Equals(Description, other.Description) &&
                   string.Equals(PrimaryFilter, other.PrimaryFilter) && Equals(Channels, other.Channels);
        }

        public RepoIndex Validate()
        {
            if (Base == null || Name == null || Description == null || PrimaryFilter == null || Channels == null)
            {
                throw new NullReferenceException();
            }

            return this;
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
        public string Type;
        public string Args;
        public string UninstallArgs;
        public string ProductCode;
        public bool RequiresReboot;
        public bool RequiresUninstallReboot;
        public long Size;
        public long InstalledSize;
        public PackageIndexInstallerSignature Signature;

        public bool Equals(PackageInstaller other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Url, other.Url) && string.Equals(Type, other.Type) && string.Equals(Args, other.Args) &&
                   string.Equals(UninstallArgs, other.UninstallArgs) && string.Equals(ProductCode, other.ProductCode) &&
                   RequiresReboot == other.RequiresReboot && RequiresUninstallReboot == other.RequiresUninstallReboot &&
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
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Args != null ? Args.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (UninstallArgs != null ? UninstallArgs.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProductCode != null ? ProductCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ RequiresReboot.GetHashCode();
                hashCode = (hashCode * 397) ^ RequiresUninstallReboot.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ InstalledSize.GetHashCode();
                hashCode = (hashCode * 397) ^ (Signature != null ? Signature.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class VirtualPackage : IEquatable<VirtualPackage>
    {
        // TODO: finish
        public string Id;
        
        public bool Equals(VirtualPackage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VirtualPackage) obj);
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }

    public class Package : IEquatable<Package>
    {
        [JsonProperty(Required = Required.Always)]
        public string Id;

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, string> Name;

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, string> Description;

        [JsonProperty(Required = Required.Always)]
        public string Version;

        [JsonProperty(Required = Required.Always)]
        public string Category;

        [JsonProperty(Required = Required.Always)]
        public List<string> Languages;

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, string> Platform;

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, string> Dependencies;

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, string> VirtualDependencies;

        [JsonProperty(Required = Required.Always)]
        public PackageInstaller Installer;

        public virtual string NativeName
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

    public class PackagesIndex : IEquatable<PackagesIndex>, IValidatable<PackagesIndex>
    {
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, Package> Packages;

        public bool Equals(PackagesIndex other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Packages, other.Packages);
        }

        public PackagesIndex Validate()
        {
            if (Packages == null)
            {
                throw new NullReferenceException();
            }

            return this;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PackagesIndex) obj);
        }

        public override int GetHashCode()
        {
            return (Packages != null ? Packages.GetHashCode() : 0);
        }
    }

    public class VirtualsIndex : IEquatable<VirtualsIndex>, IValidatable<VirtualsIndex>
    {
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, List<string>> Virtuals;

        public bool Equals(VirtualsIndex other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Virtuals, other.Virtuals);
        }

        public VirtualsIndex Validate()
        {
            if (Virtuals == null)
            {
                throw new ArgumentNullException();
            }

            return this;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VirtualsIndex) obj);
        }

        public override int GetHashCode()
        {
            return (Virtuals != null ? Virtuals.GetHashCode() : 0);
        }
    }

    public class Repository : IEquatable<Repository>, IValidatable<Repository>
    {
        public RepoIndex Meta;
        public PackagesIndex PackagesIndex;
        public VirtualsIndex VirtualsIndex;

        public virtual Dictionary<string, Package> Packages => PackagesIndex.Packages;
        public virtual Dictionary<string, List<string>> Virtuals => VirtualsIndex.Virtuals;

        public Repository(RepoIndex meta, PackagesIndex packagesIndex,
            VirtualsIndex virtualsIndex)
        {
            Meta = meta;
            PackagesIndex = packagesIndex;
            VirtualsIndex = virtualsIndex;
        }

        public bool Equals(Repository other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Meta, other.Meta) && Equals(PackagesIndex, other.PackagesIndex) &&
                   Equals(VirtualsIndex, other.VirtualsIndex);
        }

        public Repository Validate()
        {
            if (Meta == null || PackagesIndex == null || VirtualsIndex == null)
            {
                throw new NullReferenceException();
            }

            Meta.Validate();
            PackagesIndex.Validate();
            VirtualsIndex.Validate();

            return this;
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