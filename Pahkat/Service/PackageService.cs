using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Pahkat.Util;
using Microsoft.Win32;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using Pahkat.Models;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Media.Animation;
using Pahkat.Extensions;
using Pahkat.Sdk;

namespace Pahkat.Service
{
    public struct PackageProgress
    {
        public AbsolutePackageKey Key;
        public Package Package;
        public Action<PackageProgress, ulong, ulong> Progress;
    }

    public struct PackageInstallInfo
    {
        public Package Package;
        public string Path;
    }
    
    public class PackageUninstallInfo
    {
        public Package Package;
        public string Path;
        public string Args;
    }

    public interface IPackageService
    {
        PackageStatusResponse InstallStatus(AbsolutePackageKey package);
        PackageActionType DefaultPackageAction(AbsolutePackageKey package);
//        IObservable<PackageInstallInfo> Download(PackageProgress[] packages, int maxConcurrent, CancellationToken cancelToken);
        PackageUninstallInfo UninstallInfo(AbsolutePackageKey package);
        void SkipVersion(AbsolutePackageKey package);
        bool RequiresUpdate(AbsolutePackageKey package);
        bool IsValidAction(AbsolutePackageKey package, PackageActionType action);
    }
    
    public class PackageService : IPackageService
    {
        public static class Keys
        {
            public const string UninstallPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            public const string DisplayVersion = "DisplayVersion";
            public const string SkipVersion = "SkipVersion";
            public const string QuietUninstallString = "QuietUninstallString";
            public const string UninstallString = "UninstallString";
        }
        
        private readonly IWindowsRegistry _registry;
        
        public PackageService(IWindowsRegistry registry)
        {
            _registry = registry;
        }
        
        public PackageActionType DefaultPackageAction(AbsolutePackageKey package)
        {
            return IsUpToDate(package)
                ? PackageActionType.Uninstall
                : PackageActionType.Install;
        }

        private PackageStatus CompareVersion<T>(Func<string, T> creator, string packageVersion, string registryVersion) where T: IComparable<T>
        {
            var ver = creator(packageVersion);
            if (ver == null)
            {
                return PackageStatus.ErrorParsingVersion;
            }
            
            var parsedDispVer = creator(registryVersion);
            if (parsedDispVer == null)
            {
                return PackageStatus.ErrorParsingVersion;
            }

            if (ver.CompareTo(parsedDispVer) > 0)
            {
                return PackageStatus.RequiresUpdate;
            }
            else
            {
                return PackageStatus.UpToDate;
            }
        }

        private IObservable<string> DownloadFileTaskAsync(Uri uri, string dest, DownloadProgressChangedEventHandler onProgress, CancellationToken cancelToken)
        {
            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                if (onProgress != null)
                {
                    client.DownloadProgressChanged += onProgress;
                }

                cancelToken.Register(() => client.CancelAsync());

                client.DownloadFileTaskAsync(uri, dest);

                return Observable.Create<string>(observer =>
                {
                    // TODO: turn this into reactive extension... extension
                    var watcher = Observable.FromEventPattern<AsyncCompletedEventHandler, AsyncCompletedEventArgs>(
                        x => client.DownloadFileCompleted += x,
                        x => client.DownloadFileCompleted -= x)
                    .Select(x => x.EventArgs)
                    .Subscribe(args =>
                    {
                        if (args.Error != null)
                        {
                            observer.OnError(args.Error);
                        }
                        else
                        {
                            observer.OnNext(dest);
                        }

                        observer.OnCompleted();
                    });

                    return new CompositeDisposable((IDisposable)observer, watcher);
                });
            }
        }

//        public bool IsValidAction(PackageActionInfo packageActionInfo)
//        {
//            return IsValidAction(packageActionInfo.Package, packageActionInfo.Action);
//        }

        public bool IsValidAction(AbsolutePackageKey package, PackageActionType action)
        {
            switch (action)
            {
                case PackageActionType.Install:
                    return IsInstallable(package);
                case PackageActionType.Uninstall:
                    return IsUninstallable(package);
            }
            
            throw new ArgumentException("PackageAction switch exhausted unexpectedly.");
        }

        public bool RequiresUpdate(AbsolutePackageKey package)
        {
            return InstallStatus(package).Status == PackageStatus.RequiresUpdate;
        }

        public bool IsUpToDate(AbsolutePackageKey package)
        {
            return InstallStatus(package).Status == PackageStatus.UpToDate;
        }

        public bool IsError(AbsolutePackageKey package)
        {
            switch (InstallStatus(package).Status)
            {
                case PackageStatus.ErrorNoInstaller:
                case PackageStatus.ErrorParsingVersion:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsUninstallable(AbsolutePackageKey package)
        {
            switch (InstallStatus(package).Status)
            {
                case PackageStatus.UpToDate:
                case PackageStatus.RequiresUpdate:
                case PackageStatus.VersionSkipped:
                    return true;
                default:
                    return false;
            }
        }
        
        public bool IsInstallable(AbsolutePackageKey package)
        {
            switch (InstallStatus(package).Status)
            {
                case PackageStatus.NotInstalled:
                case PackageStatus.RequiresUpdate:
                case PackageStatus.VersionSkipped:
                    return true;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Checks the registry for the installed package. Uses the "DisplayVersion" value and parses that using
        /// either the Assembly versioning technique or the Semantic versioning technique. Attempts Assembly first
        /// as this tends to be more common on Windows than other platforms.
        /// </summary>
        /// <param name="packageKey"></param>
        /// <returns>The package install status</returns>
        public PackageStatusResponse InstallStatus(AbsolutePackageKey packageKey)
        {
            var app = (IPahkatApp) Application.Current;
            foreach (var repo in app.Client.Repos())
            {
                var status = repo.PackageStatus(packageKey);
                if (status != null)
                {
                    return status;
                }
            }

            return new PackageStatusResponse(PackageStatus.ErrorNoInstaller, InstallerTarget.User);
        }

        [Obsolete("Use Rust implementation")]
        public PackageUninstallInfo UninstallInfo(AbsolutePackageKey packageKey)
        {
            var app = (IPahkatApp) Application.Current;
            Package package = null;
            foreach (var repo in app.Client.Repos())
            {
                package = repo.Package(packageKey);
                if (package != null)
                {
                    break;
                }
            }

            if (package == null)
            {
                return new PackageUninstallInfo();
            }
            
            var installer = package.WindowsInstaller;
            var hklm = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            var path = $@"{Keys.UninstallPath}\{installer.ProductCode}";
            var instKey = hklm.OpenSubKey(path);

            var uninstString = instKey.Get<string>(Keys.QuietUninstallString) ??
                               instKey.Get<string>(Keys.UninstallString);
            if (uninstString != null)
            {
                var chunks = uninstString.ParseFileNameAndArgs();
                var args = package.WindowsInstaller.UninstallArgs ?? string.Join(" ", chunks.Item2);
                
                return new PackageUninstallInfo
                {
                    Package = package,
                    Path = chunks.Item1,
                    Args = args
                };
            }

            return null;
        }

        public void SkipVersion(AbsolutePackageKey package)
        {
            // TODO: implement
            //var hklm = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            //var path = $@"{AppConfigState.Keys.SubkeyId}\{package.WindowsInstaller.ProductCode}";
            //var instKey = hklm.CreateSubKey(path);
            
            //instKey.Set(Keys.SkipVersion, package.Version, RegistryValueKind.String);
        }
//
//        private string SkippedVersion(Package package)
//        {
//            // TODO: implement
//            //var hklm = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
//            //var path = $@"{AppConfigState.Keys.SubkeyId}\{package.WindowsInstaller.ProductCode}";
//            //var instKey = hklm.OpenSubKey(path);
//
//            //return instKey?.Get<string>(Keys.SkipVersion);
//            return null;
//        }

        /// <summary>
        /// Downloads the supplied packages. Each object should contain a unique progress handler so the UI can be
        /// updated effectively.
        /// </summary>
        /// <param name="packages"></param>
        /// <param name="maxConcurrent"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
//        public IObservable<PackageInstallInfo> Download(PackageProgress[] packages, int maxConcurrent, CancellationToken cancelToken)
//        {
//            return packages
//                .Select(pkg => Download(pkg, cancelToken))
//                .Merge(maxConcurrent);
//        }
//
//        private IObservable<PackageInstallInfo> Download(PackageProgress pd, CancellationToken cancelToken)
//        {
//            var app = (IPahkatApp) Application.Current;
//            return app.Client.Download(pd.Key, InstallerTarget.System)
//                .Do((progress) =>
//                {
//                    pd.Progress.Invoke(pd, progress.Downloaded, progress.Total);
//                })
//        }
    }
}