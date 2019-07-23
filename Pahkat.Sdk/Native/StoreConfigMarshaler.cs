using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class StoreConfigMarshaler : BoxMarshaler<StoreConfig>
    {
        static ICustomMarshaler GetInstance(string cookie) => new StoreConfigMarshaler();
        public StoreConfigMarshaler() : base((handle) => new StoreConfig(handle))
        {
        }
    }
}
