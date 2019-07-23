using System;
using Pahkat.Sdk;
using System.Windows;
using Pahkat.Models;

namespace Pahkat.Extensions
{
    static class AbsolutePackageKeyExt
    {
        static private PackageStatus InstallStatus(AbsolutePackageKey packageKey)
        {
            var app = (PahkatApp)Application.Current;
            return app.PackageStore.Status(packageKey).Item1;
        }

        static internal (PackageStatus, PackageTarget) Status(AbsolutePackageKey packageKey)
        {
            var app = (PahkatApp)Application.Current;
            return app.PackageStore.Status(packageKey);
        }

        public static bool RequiresUpdate(this AbsolutePackageKey package)
        {
            return InstallStatus(package) == PackageStatus.RequiresUpdate;
        }

        public static bool IsUpToDate(this AbsolutePackageKey package)
        {
            return InstallStatus(package) == PackageStatus.UpToDate;
        }

        public static bool IsError(this AbsolutePackageKey package)
        {
            switch (InstallStatus(package))
            {
                case PackageStatus.ErrorNoInstaller:
                case PackageStatus.ErrorInvalidVersion:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsUninstallable(this AbsolutePackageKey package)
        {
            switch (InstallStatus(package))
            {
                case PackageStatus.UpToDate:
                case PackageStatus.RequiresUpdate:
                case PackageStatus.VersionSkipped:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsInstallable(this AbsolutePackageKey package)
        {
            switch (InstallStatus(package))
            {
                case PackageStatus.NotInstalled:
                case PackageStatus.RequiresUpdate:
                case PackageStatus.VersionSkipped:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValidAction(this AbsolutePackageKey package, PackageAction action)
        {
            switch (action)
            {
                case PackageAction.Install:
                    return IsInstallable(package);
                case PackageAction.Uninstall:
                    return IsUninstallable(package);
            }

            throw new ArgumentException("PackageAction switch exhausted unexpectedly.");
        }

        public static PackageAction DefaultPackageAction(this AbsolutePackageKey package)
        {
            return package.IsUpToDate()
                ? PackageAction.Uninstall
                : PackageAction.Install;
        }
    }
}