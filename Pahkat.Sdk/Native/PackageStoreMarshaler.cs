using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class PackageStoreMarshaler : ArcMarshaler<PackageStore>
    {
        private static PackageStoreMarshaler instance = new PackageStoreMarshaler();
        static ICustomMarshaler GetInstance(string cookie) => instance;
        public PackageStoreMarshaler() : base((handle) => new PackageStore(handle))
        {
        }
    }
}
