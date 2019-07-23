using System.Runtime.InteropServices;

namespace Pahkat.Sdk.Native
{
    class TransactionMarshaler : BoxMarshaler<Transaction>
    {
        static ICustomMarshaler GetInstance(string cookie) => new TransactionMarshaler();

        public TransactionMarshaler() : base((handle) => new Transaction(handle))
        {
        }
    }
}
