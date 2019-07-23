using System;
using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class AbsolutePackageKeyMarshaler : ICustomMarshaler
    {
        private Utf8CStrMarshaler marshaler = new Utf8CStrMarshaler();

        static ICustomMarshaler GetInstance(string cookie) => new AbsolutePackageKeyMarshaler();

        public object MarshalNativeToManaged(IntPtr ptr)
        {
            var rawString = (string)marshaler.MarshalNativeToManaged(ptr);
            return AbsolutePackageKey.New(rawString);
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            return marshaler.MarshalManagedToNative(obj.ToString());
        }

        public void CleanUpNativeData(IntPtr ptr)
        {
            marshaler.CleanUpNativeData(ptr);
        }

        public void CleanUpManagedData(object obj)
        {
            marshaler.CleanUpManagedData(obj);
        }

        public int GetNativeDataSize() => IntPtr.Size;
    }
#pragma warning restore IDE1006 // Naming Styles
}
