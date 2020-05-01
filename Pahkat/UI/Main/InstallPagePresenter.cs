using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;
using Pahkat.Sdk;

namespace Pahkat.UI.Main
{
    public struct InstallSaveState
    {
        public bool IsCancelled;
        public bool RequiresReboot;
    }

    public class InstallPagePresenter
    {
        private readonly IInstallPageView _view;
        private readonly IScheduler _scheduler;
        private readonly CancellationTokenSource _cancelSource;

        public InstallPagePresenter(IInstallPageView view,
            // Transaction transaction,
            IScheduler scheduler) {
            _view = view;
            // _transaction = transaction;
            _scheduler = scheduler;

            _cancelSource = new CancellationTokenSource();
        }
        
        public IDisposable Start() {
            if (!Util.Util.IsAdministrator()) {
              
                // _view.RequestAdmin(jsonPath);

                return _view.OnCancelClicked().Subscribe(_ => {
                    _cancelSource.Cancel();
                    _view.ProcessCancelled();
                    _view.ShowCompletion(true, false);
                });
            }
            else {
                return Disposable.Empty;
                // return PrivilegedStart();
            }
        }
    }
}