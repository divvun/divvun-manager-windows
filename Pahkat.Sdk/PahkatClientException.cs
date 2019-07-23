using Pahkat.Sdk.Native;
using System;

namespace Pahkat.Sdk
{
    public class PahkatClientException : Exception
    {
        private PahkatClientException(string message) : base(message) { }

        private static PahkatClientException Create(IntPtr exceptionPtr)
        {
            string str = MarshalUtf8.PtrToStringUtf8(exceptionPtr);
            pahkat_client.pahkat_exception_release(exceptionPtr);
            return new PahkatClientException(str);
        }

        internal static void Try(IntPtr exceptionPtr)
        {
            if (exceptionPtr != IntPtr.Zero)
            {
                throw Create(exceptionPtr);
            }
        }
    }
}
