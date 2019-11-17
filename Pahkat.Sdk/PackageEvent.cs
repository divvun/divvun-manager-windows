namespace Pahkat.Sdk
{
    public struct PackageEvent
    {
        public readonly PackageKey PackageKey;
        public readonly PackageEventType Event;

        private PackageEvent(PackageKey key, PackageEventType evt)
        {
            PackageKey = key;
            Event = evt;
        }

        public static PackageEvent FromCode(PackageKey key, uint code)
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

        public static PackageEvent Completed(PackageKey key) => FromCode(key, 3);
        public static PackageEvent Error(PackageKey key) => FromCode(key, 4);
    }
}

