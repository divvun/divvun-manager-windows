using Newtonsoft.Json;
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
        }

        public void CleanUpNativeData(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            var objData = JsonConvert.SerializeObject(obj);
            return MarshalUtf8.StringToHGlobalUtf8(objData);
        }

        public object MarshalNativeToManaged(IntPtr ptr)
        {
            var objData = MarshalUtf8.PtrToStringUtf8(ptr);
            return JsonConvert.DeserializeObject<T>(objData);
        }
    }
}
