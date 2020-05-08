using System;
using System.Windows;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.Extensions
{
    static class PackageKeyExt
    {
        static private PackageStatus InstallStatus(PackageKey packageKey) {
            // var app = (PahkatApp) Application.Current;
            using var x = ((PahkatApp) Application.Current).PackageStore.Lock();
            return x.Value.Status(packageKey);
        }

        static internal PackageStatus Status(PackageKey packageKey) {
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
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsInstallable(this PackageKey package) {
            switch (InstallStatus(package)) {
                case PackageStatus.NotInstalled:
                case PackageStatus.RequiresUpdate:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValidAction(this PackageKey package, PackageAction action) {
            switch (action.Action) {
                case 0:
                    return IsInstallable(package);
                default:
                    return IsUninstallable(package);
            }
        }
        
        public static InstallAction DefaultInstallAction(this PackageKey package) {
            return package.IsUpToDate()
                ? InstallAction.Uninstall
                : InstallAction.Install;
        }
        
        public static PackageAction DefaultPackageAction(this PackageKey package) {
            return new PackageAction(package, DefaultInstallAction(package));
        }
    }
}