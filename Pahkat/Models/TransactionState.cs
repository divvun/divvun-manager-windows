using Pahkat.Models.Transaction;

namespace Pahkat.Models
{
    public enum TransactionStateTag
    {
        NotStarted,
        TransactionStarting,
        
    }

    namespace Transaction
    {
        struct NotStarted
        { }

        struct TransactionStarting
        {
            
        }

        struct TransactionError
        {
            
        }

        struct TransactionComplete
        {
            
        }
    }
    
    public struct TransactionState
    {
        public TransactionStateTag Tag;
        public object Value;

        void Foo() {
            var lol = Value switch {
                NotStarted notStarted => {}
            
            };
        }
    }
    
    
}