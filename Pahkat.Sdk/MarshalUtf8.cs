using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Pahkat.Sdk
{
    public static class MarshalUtf8
    {
        public static unsafe string PtrToStringUtf8(IntPtr utf8Ptr)
        {
            var bytes = (byte*)utf8Ptr.ToPointer();
            var size = 0;
            while (bytes[size] != 0)
            {
                ++size;
            }
            var buffer = new byte[size];
            Marshal.Copy(utf8Ptr, buffer, 0, size);
            return Encoding.UTF8.GetString(buffer);
        }


        public static IntPtr StringToHGlobalUtf8(string str)
        {
            var buffer = Encoding.UTF8.GetBytes(str);
            Array.Resize(ref buffer, buffer.Length + 1);
            buffer[buffer.Length - 1] = 0;
            var ptr = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, ptr, buffer.Length);
            return ptr;
        }
    }
}
