using System;
using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class Utf8CStrMarshaler : ICustomMarshaler
    {
        internal static ICustomMarshaler GetInstance(string cookie) => new Utf8CStrMarshaler();

        public void CleanUpManagedData(object obj)
        {
        }

        public void CleanUpNativeData(IntPtr ptr)
        {
            //Marshal.FreeHGlobal(ptr);
        }

        public int GetNativeDataSize() => IntPtr.Size;

        public IntPtr MarshalManagedToNative(object obj)
        {
            if (obj is string str)
            {
                return MarshalUtf8.StringToHGlobalUtf8(str);
            }
            return IntPtr.Zero;
        }

        public object MarshalNativeToManaged(IntPtr ptr)
        {
            var result = MarshalUtf8.PtrToStringUtf8(ptr);
            //ArcPtrHandle.ReleaseHandle(ptr);
            return result;
        }
    }
}
