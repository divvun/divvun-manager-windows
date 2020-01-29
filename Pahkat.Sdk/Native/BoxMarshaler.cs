using Serilog;
using System;
using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    abstract class BoxMarshaler<T> : ICustomMarshaler where T : Boxed
    {
        private Func<IntPtr, T> Create;

        protected BoxMarshaler(Func<IntPtr, T> creator)
        {
            Create = creator;
        }

        public int GetNativeDataSize() => IntPtr.Size;

        public IntPtr MarshalManagedToNative(object obj)
        {

            if (obj is Boxed ptrHolder)
            {
                var handle = ptrHolder.handle;
                Log.Verbose("[MARSHAL] BoxMarshaler {Type} -> {Ptr}", typeof(T).FullName, handle);
                return handle;
            }
            return IntPtr.Zero;
        }

        public void CleanUpNativeData(IntPtr ptr)
        {
            // TODO: review Box does what we expect.
        }

        public object MarshalNativeToManaged(IntPtr ptr)
        {
            Log.Verbose("[MARSHAL] BoxMarshaler {Type} NativeToManaged <- {Ptr}", typeof(T).FullName, ptr);
            return Create(ptr);
        }

        public void CleanUpManagedData(object obj)
        {
            // TODO: review Box does what we expect.
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
