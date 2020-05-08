using System;

namespace Pahkat.Sdk
{
    public enum PackageStatus: int
    {
        NotInstalled = 0,
        UpToDate = 1,
        RequiresUpdate = 2,
        ErrorNoConcretePackage = -1,
        ErrorNoPayload = -2,
        ErrorWrongPayloadType = -3,
        ErrorInvalidVersion = -4,
        ErrorCriteriaUnmet = -5,
        Unknown = Int32.MinValue
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
                case -1:
                    return PackageStatus.ErrorNoConcretePackage;
                case -2:
                    return PackageStatus.ErrorNoPayload;
                case -3:
                    return PackageStatus.ErrorWrongPayloadType;
                case -4:
                    return PackageStatus.ErrorInvalidVersion;
                case -5:
                    return PackageStatus.ErrorCriteriaUnmet;
                default:
                    return PackageStatus.Unknown;
            }
        }
    }
}