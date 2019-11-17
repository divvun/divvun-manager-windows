using Pahkat.Sdk.Native;
using System;

namespace Pahkat.Sdk
{
    public class PahkatClientException : Exception
    {
        static internal string lastError = null;

        static internal pahkat_client.ErrCallback Callback = (ptr) =>
        {
            lastError = MarshalUtf8.PtrToStringUtf8(ptr);
        };
        static internal void AssertNoError()
        {
            if (lastError != null)
            {
                var err = lastError;
                lastError = null;
                throw new PahkatClientException(err);
            }
        }

        private PahkatClientException(string message) : base(message) { }
    }
}
