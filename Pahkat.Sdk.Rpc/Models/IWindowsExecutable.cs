using System.Collections.Generic;
using FlatBuffers;
using Pahkat.Sdk.Rpc.Fbs;
using Pahkat.Sdk.Rpc.Models;

namespace Pahkat.Sdk.Rpc.Models
{
    public interface ILoadedRepository
    {
        LoadedRepository.IndexValue Index { get; }
        LoadedRepository.MetaValue Meta { get; }
        IPackages Packages { get; }

        PackageKey PackageKey(IDescriptor descriptor);
    }
    
    public interface IWindowsExecutable
    {
        ulong Size { get; }
    }

    public interface ITarget
    {
        string Platform { get; }
        Payload PayloadType { get; }
        
    }

    public interface IPackages
    {
        IReadOnlyDictionary<string, IDescriptor?> Packages { get; }
    }

    public interface IDescriptor
    {
        string Id { get; }
        IReadOnlyDictionary<string, string> Name { get; }
        IReadOnlyDictionary<string, string> Description { get; }
        IReadOnlyList<string> Tags { get; }
        IReadOnlyList<IRelease?> Release { get; }
    }

    public interface IRelease
    {
        string? Channel { get; }
        string? Version { get; }
        IReadOnlyList<string> Authors { get; }
        IReadOnlyList<ITarget?> Target { get; }
        // ITarget? WindowsTarget { get; }
        // IWindowsExecutable? WindowsExecutable { get; }
    }
}

namespace Pahkat.Sdk.Rpc.Fbs
{
    public partial struct Packages : IPackages
    {
        IReadOnlyDictionary<string, IDescriptor?> IPackages.Packages {
            get {
                var self = this;
                return new RefMap<string, IDescriptor?>(
                    PackagesValuesLength, i => self.PackagesValues(i) as IDescriptor, PackagesKeys);
            }
        }
    }

    public partial struct Descriptor : IDescriptor
    {
        public IReadOnlyDictionary<string, string> Name => 
            new RefMap<string, string>(
                NameValuesLength, 
                NameValues, 
                NameKeys);
        public IReadOnlyDictionary<string, string> Description => 
            new RefMap<string, string>(
                DescriptionValuesLength, 
                DescriptionValues, 
                DescriptionKeys);

        IReadOnlyList<string> IDescriptor.Tags => new RefList<string>(TagsLength, Tags);

        IReadOnlyList<IRelease?> IDescriptor.Release {
            get {
                var self = this;
                return new RefList<IRelease?>(ReleaseLength, i => self.Release(i) as IRelease);
            }
        }
    }

    public partial struct Release : IRelease
    {
        IReadOnlyList<string> IRelease.Authors => new RefList<string>(AuthorsLength, Authors);

        IReadOnlyList<ITarget?> IRelease.Target {
            get {
                var self = this;
                return new RefList<ITarget?>(TargetLength, i => self.Target(i) as ITarget);
            }
        }
    }
    
    public partial struct Target : ITarget
    {
        
    }

    public partial struct WindowsExecutable : IWindowsExecutable
    {
        
    }
}