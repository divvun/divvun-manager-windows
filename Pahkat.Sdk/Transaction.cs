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
            var result = pahkat_client.pahkat_windows_transaction_new(store, actions.ToArray(), PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return result;
        }

        public List<TransactionAction> Actions()
        {
            var results = pahkat_client.pahkat_windows_transaction_actions(this, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
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
                    pahkat_client.pahkat_windows_transaction_process(this, 0, Callback, PahkatClientException.Callback);

                    try
                    {
                        PahkatClientException.AssertNoError();
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
