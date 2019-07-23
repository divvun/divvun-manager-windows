using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class TransactionActionMarshaler : JsonMarshaler<TransactionAction>
    {
        static ICustomMarshaler GetInstance(string cookie) => new TransactionActionMarshaler();
    }
}
