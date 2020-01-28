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

        public static int ToInt(this PackageStatus value)
        {
            switch (value)
            {
                case PackageStatus.NotInstalled:
                    return 0;
                case PackageStatus.UpToDate:
                    return 1;
                case PackageStatus.RequiresUpdate:
                    return 2;
                case PackageStatus.VersionSkipped:
                    return 3;
                case PackageStatus.ErrorNoPackage:
                    return -1;
                case PackageStatus.ErrorNoInstaller:
                    return -2;
                case PackageStatus.ErrorWrongInstallerType:
                    return -3;
                case PackageStatus.ErrorInvalidVersion:
                    return -4;
                case PackageStatus.ErrorInvalidInstallPath:
                    return -5;
                case PackageStatus.ErrorInvalidMetadata:
                    return -6;
                default:
                    return -128;
            }
        }
    }
}
