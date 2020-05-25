namespace Pahkat.Sdk.Rpc
{
    public enum Notification: int
    {
        RebootRequired = 0,
        RepositoriesChanged,
        RpcStopping,
        TransactionLocked,
        TransactionUnlocked,
    }
}