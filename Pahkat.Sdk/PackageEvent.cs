namespace Pahkat.Sdk
{
    public struct PackageEvent
    {
        public readonly AbsolutePackageKey PackageKey;
        public readonly PackageEventType Event;

        private PackageEvent(AbsolutePackageKey key, PackageEventType evt)
        {
            PackageKey = key;
            Event = evt;
        }

        public static PackageEvent FromCode(AbsolutePackageKey key, uint code)
        {
            PackageEventType evt = PackageEventType.Error;
            switch (code)
            {
                case 0:
                    evt = PackageEventType.NotStarted;
                    break;
                case 1:
                    evt = PackageEventType.Uninstalling;
                    break;
                case 2:
                    evt = PackageEventType.Installing;
                    break;
                case 3:
                    evt = PackageEventType.Completed;
                    break;
                case 4:
                    evt = PackageEventType.Error;
                    break;
            }

            return new PackageEvent(key, evt);
        }

        public static PackageEvent Completed(AbsolutePackageKey key) => FromCode(key, 3);
        public static PackageEvent Error(AbsolutePackageKey key) => FromCode(key, 4);
    }
}

