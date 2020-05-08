using Pahkat.Sdk;

namespace Divvun.Installer.Extensions
{
    public static class PackageStatusExt
    {
        public static string? Description(this PackageStatus status) {
            switch (status) {
                case PackageStatus.ErrorNoPayload:
                    return Strings.ErrorNoInstaller;
                case PackageStatus.ErrorInvalidVersion:
                    return Strings.ErrorInvalidVersion;
                case PackageStatus.RequiresUpdate:
                    return Strings.UpdateAvailable;
                case PackageStatus.NotInstalled:
                    return Strings.NotInstalled;
                case PackageStatus.UpToDate:
                    return Strings.Installed;
            }

            return null;
        }
    }
}