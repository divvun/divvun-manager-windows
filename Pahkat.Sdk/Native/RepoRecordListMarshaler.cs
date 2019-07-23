using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class RepoRecordListMarshaler : JsonMarshaler<RepoRecord[]>
    {
        private static RepoRecordListMarshaler instance = new RepoRecordListMarshaler();
        static ICustomMarshaler GetInstance(string cookie) => instance;
    }
}
