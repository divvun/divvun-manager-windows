using System.Collections.Generic;
using Divvun.Installer.Service;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;
using Pahkat.Sdk.Rpc.Models;
using Iterable;

namespace Divvun.Installer.Extensions
{
    public static class DescriptorExt
    {
        private const string TagPrefixCategory = "cat:";
        private const string TagPrefixLanguage = "lang:";
        
        public static IEnumerable<string> Categories(this IDescriptor descriptor) {
            return descriptor.Tags
                .Filter(t => t.StartsWith(TagPrefixCategory));
        }
        
        public static IEnumerable<string> Languages(this IDescriptor descriptor) {
            return descriptor.Tags
                .Filter(t => t.StartsWith(TagPrefixLanguage));
        }
    }

    public static class LoadedRepositoryExt
    {
        public static IRelease? Release(this ILoadedRepository repo, PackageKey packageKey) {
            if (packageKey.RepositoryUrl != repo.Index.Url) {
                return null;
            }
            
            if (!repo.Packages.Packages.TryGetValue(packageKey.Id, out var descriptor) || descriptor == null) {
                return null;
            }

            var releases = descriptor.Release;

            var release = descriptor.Release.First((r) => {
                // Check if version even has a Windows target
                var target = r?.WindowsTarget();

                if (target == null) {
                    return false;
                }

                // Check the channel is valid
                var channel = r?.Channel == "" ? null : r?.Channel;
                if (repo.Meta.Channel != null) {
                    // If a defined repo channel, find a package with no channel or this channel 
                    return channel == null || repo.Meta.Channel == r?.Channel;
                }

                // Free pass, no channels on either side :D
                return repo.Meta.Channel == null;
            });

            return release;
        }

        public static IWindowsExecutable? Payload(this ILoadedRepository repo, PackageKey packageKey) {
            return repo.Release(packageKey)?.WindowsExecutable();
        }
    }

    public static class FbsExt
    {
        public static ITarget? WindowsTarget(this IRelease release) {
            return Iterable.Iterable.First(release.Target, x => x?.Platform == "windows");
        }

        public static IWindowsExecutable? WindowsExecutable(this IRelease release) {
            return release.WindowsTarget()?.WindowsExecutable();
        }

        public static IWindowsExecutable? WindowsExecutable(this ITarget target) {
            if (target is Target t) {
                
                if (target.PayloadType == Payload.WindowsExecutable) {
                    return t.Payload<WindowsExecutable>();
                }
            }

            if (target is MockTarget t2) {
                return t2.Payload;
            }
        
            return null;
        }
    }
}