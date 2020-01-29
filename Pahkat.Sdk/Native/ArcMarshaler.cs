using Serilog;
using System;
using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    abstract class ArcMarshaler<T> : ICustomMarshaler where T : Arced
    {
        private Func<IntPtr, T> Create;

        protected ArcMarshaler(Func<IntPtr, T> creator)
        {
            Create = creator;
        }

        public int GetNativeDataSize() => IntPtr.Size;

        public IntPtr MarshalManagedToNative(object obj)
        {
            if (obj is Arced ptrHolder)
            {
                var handle = ptrHolder.handle;
                Log.Verbose("[MARSHAL] ArcMarshaler {Type} -> {Ptr}", typeof(T).FullName, handle);
                return handle;
            }
            return IntPtr.Zero;
        }

        public void CleanUpNativeData(IntPtr ptr)
        {
            // TODO: review Arc does what we expect.
        }

        public object MarshalNativeToManaged(IntPtr ptr)
        {
            Log.Verbose("[MARSHAL] ArcMarshaler {Type} <- {Ptr}", typeof(T).FullName, ptr);
            return Create(ptr);
        }

        public void CleanUpManagedData(object obj)
        {
            // TODO: review Arc does what we expect.
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
