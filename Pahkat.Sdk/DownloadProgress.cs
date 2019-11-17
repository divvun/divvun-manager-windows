namespace Pahkat.Sdk
{
    public struct DownloadProgress
    {
        public PackageKey PackageId;
        public PackageDownloadStatus Status;
        public ulong Downloaded;
        public ulong Total;
        public string ErrorMessage;

        public static DownloadProgress Progress(PackageKey packageId, ulong cur, ulong max)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Progress,
                Downloaded = cur,
                Total = max
            };
        }

        public static DownloadProgress Completed(PackageKey packageId)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Completed
            };
        }

        public static DownloadProgress NotStarted(PackageKey packageId)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.NotStarted
            };
        }

        public static DownloadProgress Error(PackageKey packageId, string error)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Error,
                ErrorMessage = error
            };
        }

        public static DownloadProgress Starting(PackageKey packageId)
        {
            return new DownloadProgress
            {
                PackageId = packageId,
                Status = PackageDownloadStatus.Starting
            };
        }
    }
}

