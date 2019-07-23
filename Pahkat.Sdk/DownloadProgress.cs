namespace Pahkat.Sdk
{
    public struct DownloadProgress
    {
        public AbsolutePackageKey PackageId;
        public PackageDownloadStatus Status;
        public ulong Downloaded;
        public ulong Total;
        public string ErrorMessage;

        public static DownloadProgress Progress(AbsolutePackageKey packageId, ulong cur, ulong max)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Progress,
                Downloaded = cur,
                Total = max
            };
        }

        public static DownloadProgress Completed(AbsolutePackageKey packageId)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Completed
            };
        }

        public static DownloadProgress NotStarted(AbsolutePackageKey packageId)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.NotStarted
            };
        }

        public static DownloadProgress Error(AbsolutePackageKey packageId, string error)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Error,
                ErrorMessage = error
            };
        }

        public static DownloadProgress Starting(AbsolutePackageKey packageId)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Starting
            };
        }
    }
}

