using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class RepositoryIndexListMarshaler : JsonMarshaler<RepositoryIndex[]>
    {
        private static RepositoryIndexListMarshaler instance = new RepositoryIndexListMarshaler();
        static ICustomMarshaler GetInstance(string cookie) => instance;
    }
}
