using System;
using System.ComponentModel;
using System.IO;
using Bahkat.Models.PackageManager;
using Bahkat.Util;
using Microsoft.Win32;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using Bahkat.Models;

namespace Bahkat.Service
{
    public enum PackageInstallStatus
    {
        NotInstalled,
        UpToDate,
        NeedsUpdate,
        ErrorNoInstaller,
        ErrorParsingVersion
    }

    public struct PackageProgress
    {
        public Package Package;
        public DownloadProgressChangedEventHandler Progress;
    }

    public struct PackagePath
    {
        public Package Package;
        public string Path;
    }

    public interface IPackageService
    {
        PackageInstallStatus GetInstallStatus(Package package);
        IObservable<PackagePath> Download(PackageProgress[] packages, int maxConcurrent);
    }
    
    public class PackageService : IPackageService
    {
        public static readonly string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        
        private readonly IWindowsRegistry _registry;
        
        public PackageService(IWindowsRegistry registry)
        {
            _registry = registry;
        }

        private PackageInstallStatus CompareVersion<T>(Func<string, T> creator, string packageVersion, string registryVersion) where T: IComparable<T>
        {
            var ver = creator(packageVersion);
            if (ver == null)
            {
                return PackageInstallStatus.ErrorParsingVersion;
            }
            
            var parsedDispVer = creator(registryVersion);
            if (parsedDispVer == null)
            {
                return PackageInstallStatus.ErrorParsingVersion;
            }

            if (ver.CompareTo(parsedDispVer) > 0)
            {
                return PackageInstallStatus.NeedsUpdate;
            }
            else
            {
                return PackageInstallStatus.UpToDate;
            }
        }

        private IObservable<string> DownloadFileTaskAsync(Uri uri, string dest, DownloadProgressChangedEventHandler onProgress)
        {
            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                if (onProgress != null)
                {
                    client.DownloadProgressChanged += onProgress;
                }

                client.DownloadFileTaskAsync(uri, dest);

                return Observable.FromEventPattern<AsyncCompletedEventHandler, AsyncCompletedEventArgs>(
                        x => client.DownloadFileCompleted += x,
                        x => client.DownloadFileCompleted -= x)
                    .Select(_ => dest);
            }
        }

        // TODO integration testing lol
        private IObservable<PackagePath> Download(PackageProgress pd)
        {
            var inst = pd.Package.Installer.Value;
            
            // Get file ending from URL
            var ext = Path.GetExtension(inst.Url.AbsoluteUri);
            
            // Make name package name + version
            var fileName = $"{pd.Package.Id}-{pd.Package.Version}.{ext}";
            var path = Path.Combine(Path.GetTempPath(), fileName);

            return DownloadFileTaskAsync(inst.Url, path, pd.Progress)
                .Select(x => new PackagePath { Package = pd.Package, Path = x });
        }
        
        /// <summary>
        /// Checks the registry for the installed package. Uses the "DisplayVersion" value and parses that using
        /// either the Assembly versioning technique or the Semantic versioning technique. Attempts Assembly first
        /// as this tends to be more common on Windows than other platforms.
        /// </summary>
        /// <param name="package"></param>
        /// <returns>The package install status</returns>
        public PackageInstallStatus GetInstallStatus(Package package)
        {
            if (!package.Installer.HasValue)
            {
                return PackageInstallStatus.ErrorNoInstaller;
            }

            var installer = package.Installer.Value;
            var hklm = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            var path = UninstallKeyPath + @"\" + installer.ProductCode;
            var instKey = hklm.OpenSubKey(path);

            if (instKey == null)
            {
                return PackageInstallStatus.NotInstalled;
            }
            
            var displayVersion = instKey.Get("DisplayVersion", "");
            if (displayVersion == "")
            {
                return PackageInstallStatus.ErrorParsingVersion;
            }

            var comp = CompareVersion(AssemblyVersion.Create, package.Version, displayVersion);
            if (comp != PackageInstallStatus.ErrorParsingVersion)
            {
                return comp;
            }
                
            comp = CompareVersion(SemanticVersion.Create, package.Version, displayVersion);
            if (comp != PackageInstallStatus.ErrorParsingVersion)
            {
                return comp;
            }

            return PackageInstallStatus.ErrorParsingVersion;
        }

        /// <summary>
        /// Downloads the supplied packages. Each object should contain a unique progress handler so the UI can be
        /// updated effectively.
        /// </summary>
        /// <param name="packages"></param>
        /// <param name="maxConcurrent"></param>
        /// <returns></returns>
        public IObservable<PackagePath> Download(PackageProgress[] packages, int maxConcurrent)
        {
            return packages.Select(Download).Merge(maxConcurrent);
        }
    }
}