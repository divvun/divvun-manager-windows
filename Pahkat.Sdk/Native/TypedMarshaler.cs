using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pahkat.Sdk.Native
{
    abstract class TypedMarshaler<T> : ICustomMarshaler
    {
        public abstract void CleanUpManagedData(T obj);
        public abstract void CleanUpNativeData(IntPtr ptr);
        public abstract IntPtr MarshalManagedToNative(T obj);
        public abstract T MarshalNativeToTypedManaged(IntPtr ptr);

        public abstract int GetNativeDataSize();


        public void CleanUpManagedData(object ManagedObj)
        {
            if (ManagedObj is T obj)
            {
                MarshalManagedToNative(obj);
                return;
            }

            // This should not be reachable, so we make a warning.
            Log.Error("[MARSHAL] TypedMarshaler {Type} CleanUp: managed object failed type check: {Obj}", typeof(T).FullName, ManagedObj);
            return;
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (ManagedObj is T obj)
            {
                return MarshalManagedToNative(obj);
            }

            // This should not be reachable, so we make a warning.
            Log.Error("[MARSHAL] TypedMarshaler {Type} ToNative: managed object failed type check: {Obj}", typeof(T).FullName, ManagedObj);
            return IntPtr.Zero;
        }

        public object? MarshalNativeToManaged(IntPtr pNativeData)
        {
            return MarshalNativeToTypedManaged(pNativeData);
        }
    }
}
