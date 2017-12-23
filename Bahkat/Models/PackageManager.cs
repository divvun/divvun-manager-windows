using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bahkat.Models.PackageManager
{
    public struct RepoIndex
    {
        public Uri Base { get; set; }
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> Description { get; set; }
        public string PrimaryFilter { get; set; }
        public List<string> Channels { get; set; }
    }

    public struct PackageIndexInstallerSignature
    {

    }

    public struct PackageInstaller
    {
        public Uri Url;
        public string SilentArgs;
        public string ProductCode;
        public bool RequiresReboot;
        public long Size;
        public long InstalledSize;
        public PackageIndexInstallerSignature? Signature;
    }

    public class Package
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
        public PackageInstaller? Installer;

        public string NativeName
        {
            get
            {
                var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
                return Name.ContainsKey(tag) ? Name[tag] : Name["en"];
            }
        }
    }

    public struct VirtualIndexTarget
    {

    }

    public struct VirtualIndex
    {
        public string Id;
        public Dictionary<string, string> Name;
        public Dictionary<string, string> Description;
        public string Version;
        public Uri Url;
        public VirtualIndexTarget Target;
    }

    public class Repository
    {
        protected bool Equals(Repository other)
        {
            return Meta.Equals(other.Meta) && Equals(PackagesIndex, other.PackagesIndex) && Equals(VirtualsIndex, other.VirtualsIndex);
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
                var hashCode = Meta.GetHashCode();
                hashCode = (hashCode * 397) ^ (PackagesIndex != null ? PackagesIndex.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (VirtualsIndex != null ? VirtualsIndex.GetHashCode() : 0);
                return hashCode;
            }
        }

        public RepoIndex Meta { get; private set; }
        public Dictionary<string, Package> PackagesIndex { get; private set; }
        public Dictionary<string, List<string>> VirtualsIndex { get; private set; }

        public Repository(RepoIndex meta, Dictionary<string, Package> packagesIndex,
            Dictionary<string, List<string>> virtualsIndex)
        {
            Meta = meta;
            PackagesIndex = packagesIndex;
            VirtualsIndex = virtualsIndex;
        }
    }
}