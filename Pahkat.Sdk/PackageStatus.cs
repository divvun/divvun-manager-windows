namespace Pahkat.Sdk
{
    public enum PackageStatus
    {
        NotInstalled,
        UpToDate,
        RequiresUpdate,
        VersionSkipped,
        ErrorNoPackage,
        ErrorNoInstaller,
        ErrorWrongInstallerType,
        ErrorInvalidVersion,
        ErrorInvalidInstallPath,
        ErrorInvalidMetadata,
        Unknown
    }

    public static class PackageStatusExt
    {
        public static PackageStatus FromInt(int value)
        {
            switch (value) {
                case 0:
                    return PackageStatus.NotInstalled;
                case 1:
                    return PackageStatus.UpToDate;
                case 2:
                    return PackageStatus.RequiresUpdate;
                case 3:
                    return PackageStatus.VersionSkipped;
                case -1:
                    return PackageStatus.ErrorNoPackage;
                case -2:
                    return PackageStatus.ErrorNoInstaller;
                case -3:
                    return PackageStatus.ErrorWrongInstallerType;
                case -4:
                    return PackageStatus.ErrorInvalidVersion;
                case -5:
                    return PackageStatus.ErrorInvalidInstallPath;
                case -6:
                    return PackageStatus.ErrorInvalidMetadata;
                default:
                    return PackageStatus.Unknown;
            }
        }
    }
}
