using System;
using Pahkat.Sdk;
using System.Windows;
using Pahkat.Models;

namespace Pahkat.Extensions
{
    static class PackageKeyExt
    {
        static private PackageStatus InstallStatus(PackageKey packageKey) {
            throw new NotImplementedException();
            // var app = (PahkatApp)Application.Current;
            // return app.PackageStore.Status(packageKey).Item1;
        }

        static internal (PackageStatus, PackageTarget) Status(PackageKey packageKey) {
            throw new NotImplementedException();
        }

        public static bool RequiresUpdate(this PackageKey package) {
            return InstallStatus(package) == PackageStatus.RequiresUpdate;
        }

        public static bool IsUpToDate(this PackageKey package) {
            return InstallStatus(package) == PackageStatus.UpToDate;
        }

        public static bool IsUninstallable(this PackageKey package) {
            switch (InstallStatus(package)) {
                case PackageStatus.UpToDate:
                case PackageStatus.RequiresUpdate:
                case PackageStatus.VersionSkipped:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsInstallable(this PackageKey package) {
            switch (InstallStatus(package)) {
                case PackageStatus.NotInstalled:
                case PackageStatus.RequiresUpdate:
                case PackageStatus.VersionSkipped:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValidAction(this PackageKey package, PackageAction action) {
            switch (action) {
                case PackageAction.Install:
                    return IsInstallable(package);
                case PackageAction.Uninstall:
                    return IsUninstallable(package);
            }

            throw new ArgumentException("PackageAction switch exhausted unexpectedly.");
        }

        public static PackageAction DefaultPackageAction(this PackageKey package) {
            return package.IsUpToDate()
                ? PackageAction.Uninstall
                : PackageAction.Install;
        }
    }
}