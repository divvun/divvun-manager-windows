using System.Collections.Generic;
using Iterable;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;

namespace Divvun.Installer.Extensions
{
    public static class DescriptorExt
    {
        private const string TagPrefixCategory = "cat:";
        private const string TagPrefixLanguage = "lang:";
        
        public static IEnumerable<string> Categories(this Descriptor descriptor) {
            return descriptor.Tags()
                .Filter(t => t.StartsWith(TagPrefixCategory));
        }
        
        public static IEnumerable<string> Languages(this Descriptor descriptor) {
            return descriptor.Tags()
                .Filter(t => t.StartsWith(TagPrefixLanguage));
        }
    }

    public static class LoadedRepositoryExt
    {
        public static Release? Release(this LoadedRepository repo, PackageKey packageKey) {
            if (packageKey.RepositoryUrl != repo.Index.Url) {
                return null;
            }
            
            if (!repo.Packages.Packages().TryGetValue(packageKey.Id, out var descriptor) || descriptor == null) {
                return null;
            }

            var release = descriptor.Value.Release().First((r) => {
                // Check if version even has a Windows target
                var target = r?.WindowsTarget();

                if (target == null) {
                    return false;
                }

                // Check the channel is valid
                var channel = r.Value.Channel == "" ? null : r.Value.Channel;
                if (repo.Meta.Channel != null) {
                    // If a defined repo channel, find a package with no channel or this channel 
                    return channel == null || repo.Meta.Channel == r.Value.Channel;
                }

                // Free pass, no channels on either side :D
                return repo.Meta.Channel == null;
            });

            return release;
        }

        public static WindowsExecutable? Payload(this LoadedRepository repo, PackageKey packageKey) {
            return repo.Release(packageKey)?.WindowsTarget()?.WindowsExecutable();
        }
    }
}