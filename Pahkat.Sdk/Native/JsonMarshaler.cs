using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pahkat.Sdk.Native
{
    abstract class JsonMarshaler<T> : ICustomMarshaler
    {
        public int GetNativeDataSize() => IntPtr.Size;

        public void CleanUpManagedData(object obj)
        {
            Log.Verbose("[MARSHAL] JsonMarshaler {Type} DROP MANAGED", typeof(T).FullName);
        }

        public void CleanUpNativeData(IntPtr ptr)
        {
            Log.Verbose("[MARSHAL] JsonMarshaler {Type} DROP NATIVE", typeof(T).FullName);
            Marshal.FreeHGlobal(ptr);
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            var objData = JsonConvert.SerializeObject(obj);
            Log.Verbose("[MARSHAL] JsonMarshaler {Type} -> {Json}", typeof(T).FullName, objData);
            return MarshalUtf8.StringToHGlobalUtf8(objData);
        }

        public object? MarshalNativeToManaged(IntPtr ptr)
        {
            var objData = MarshalUtf8.PtrToStringUtf8(ptr);
            Log.Verbose("[MARSHAL] JsonMarshaler {Type} <- {Json}", typeof(T).FullName, objData);
            return JsonConvert.DeserializeObject<T>(objData);
        }
    }
}
