using System;
using System.Threading.Tasks;
using System.Windows;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Serilog;

namespace Divvun.Installer.Extensions
{
    static class PackageKeyExt
    {
        private static async Task<PackageStatus> InstallStatus(PackageKey packageKey) {
            // var app = (PahkatApp) Application.Current;
            // using var x = ((PahkatApp) Application.Current).PackageStore.Lock();
            // var what = 32;
            Log.Verbose("install status");
            return await PahkatApp.Current.PackageStore.Status(packageKey);
        }

        internal static PackageStatus Status(PackageKey packageKey) {
            throw new NotImplementedException();
        }

        public static async Task<bool> RequiresUpdate(this PackageKey package) {
            return await InstallStatus(package) == PackageStatus.RequiresUpdate;
        }

        private static async Task<bool> IsUpToDate(this PackageKey package) {
            return await InstallStatus(package) == PackageStatus.UpToDate;
        }

        public static async Task<bool> IsUninstallable(this PackageKey package) {
            switch (await InstallStatus(package)) {
                case PackageStatus.UpToDate:
                case PackageStatus.RequiresUpdate:
                    return true;
                default:
                    return false;
            }
        }

        public static async Task<bool> IsInstallable(this PackageKey package) {
            switch (await InstallStatus(package)) {
                case PackageStatus.NotInstalled:
                case PackageStatus.RequiresUpdate:
                    return true;
                default:
                    return false;
            }
        }

        public static Task<bool> IsValidAction(this PackageKey package, PackageAction action) {
            switch (action.Action) {
                case 0:
                    return IsInstallable(package);
                default:
                    return IsUninstallable(package);
            }
        }
        
        public static async Task<InstallAction> DefaultInstallAction(this PackageKey package) {
            return await package.IsUpToDate()
                ? InstallAction.Uninstall
                : InstallAction.Install;
        }
        
        public static async Task<PackageAction> DefaultPackageAction(this PackageKey package) {
            return new PackageAction(package, await DefaultInstallAction(package));
        }
    }
}