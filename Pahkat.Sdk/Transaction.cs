using Pahkat.Sdk.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using static Pahkat.Sdk.PahkatClientException;

namespace Pahkat.Sdk
{
    public class Transaction : Boxed
    {
        internal Transaction(IntPtr handle) : base(handle)
        {
        }

        public static Transaction New(PackageStore store, List<TransactionAction> actions)
        {
            var result = pahkat_client.pahkat_windows_transaction_new(store, actions.ToArray(), out var exception);
            Try(exception);
            return result;
        }

        public List<TransactionAction> Actions()
        {
            var results = pahkat_client.pahkat_windows_transaction_actions(this, out var exception);
            Try(exception);
            return results.ToList();
        }

        public IObservable<PackageEvent> Process()
        {
            return Observable.Create<PackageEvent>((observer) =>
            {
                void Callback(uint tag, IntPtr rawPackageKey, uint eventCode)
                {
                    var packageKey = PackageKey.FromPtr(rawPackageKey);
                    var evt = PackageEvent.FromCode(packageKey, eventCode);
                    observer.OnNext(evt);
                }

                unsafe
                {
                    pahkat_client.pahkat_windows_transaction_process(this, Callback, 0, out var exception);

                    try
                    {
                        Try(exception);
                        observer.OnCompleted();
                    }
                    catch (PahkatClientException e)
                    {
                        observer.OnError(e);
                    }
                }

                return Disposable.Empty;
            });
        }
    }
}
