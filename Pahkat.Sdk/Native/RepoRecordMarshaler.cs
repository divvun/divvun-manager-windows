using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class RepoRecordMarshaler : JsonMarshaler<RepoRecord>
    {
        private static RepoRecordMarshaler instance = new RepoRecordMarshaler();
        static ICustomMarshaler GetInstance(string cookie) => instance;
    }
}
