using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class ActionListMarshaler : JsonMarshaler<TransactionAction[]>
    {
        private static ActionListMarshaler instance = new ActionListMarshaler();
        static ICustomMarshaler GetInstance(string cookie) => instance;
    }
}
