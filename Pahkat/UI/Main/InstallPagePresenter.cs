using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
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
        private readonly IPahkatTransaction _transaction;
        private readonly IScheduler _scheduler;
        private readonly CancellationTokenSource _cancelSource;
        private readonly string _stateDir;
        
        public InstallPagePresenter(IInstallPageView view,
            IPahkatTransaction transaction,
            IScheduler scheduler)
        {
            _view = view;
            _transaction = transaction;
            _scheduler = scheduler;

            _cancelSource = new CancellationTokenSource();

            var tmpPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            _stateDir = Path.Combine(tmpPath, "Pahkat", "state");
        }

        public void SaveResultsState(InstallSaveState state)
        {
            Directory.CreateDirectory(_stateDir);
            File.WriteAllText(ResultsPath, JsonConvert.SerializeObject(state));
        }

        public string ResultsPath => Path.Combine(_stateDir, "results.json");

        public InstallSaveState ReadResultsState()
        {
            return JsonConvert.DeserializeObject<InstallSaveState>(File.ReadAllText(ResultsPath));
        }

        private IDisposable PrivilegedStart()
        {
            var app = (IPahkatApp) Application.Current;
            _view.SetTotalPackages(_transaction.Actions.Length);

            var keys = new HashSet<AbsolutePackageKey>(_transaction.Actions.Select((x) => x.Id));
            var packages = new Dictionary<AbsolutePackageKey, Package>();
            
            // Cache the packages in advance
            foreach (var repo in app.Client.Repos())
            {
                var copiedKeys = new HashSet<AbsolutePackageKey>(keys);
                foreach (var key in copiedKeys)
                {
                    var package = repo.Package(key);
                    if (package != null)
                    {
                        keys.Remove(key);
                        packages[key] = package;
                    }
                }
            }

            var requiresReboot = false;

            return _transaction.Process()
                .Delay(TimeSpan.FromSeconds(0.5))
                .SubscribeOn(NewThreadScheduler.Default)
                .ObserveOn(_scheduler)
                .Subscribe((evt) =>
            {
                var action = _transaction.Actions.First((x) => x.Id.Equals(evt.PackageKey));
                var package = packages[evt.PackageKey];
                
                switch (evt.Event)
                {
                    case PackageEventType.Installing:
                        _view.SetStarting(action.Action, package);
                        if (package.WindowsInstaller.RequiresReboot)
                        {
                            requiresReboot = true;
                        }
                        break;
                    case PackageEventType.Uninstalling:
                        _view.SetStarting(action.Action, package);
                        if (package.WindowsInstaller.RequiresUninstallReboot)
                        {
                            requiresReboot = true;
                        }
                        break;
                    case PackageEventType.Completed:
                        _view.SetEnding();
                        break;
                    case PackageEventType.Error:
                        MessageBox.Show(Strings.ErrorDuringInstallation, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            },
            _view.HandleError,
            () => {
                if (_cancelSource.IsCancellationRequested)
                {
                    this._view.ProcessCancelled();
                }
                else
                {
                    _view.ShowCompletion(false, requiresReboot);
                }
            });
        }

        public IDisposable Start()
        {
            if (!Util.Util.IsAdministrator())
            {
                Directory.CreateDirectory(_stateDir);
                var jsonPath = Path.Combine(_stateDir, "install.json");
                var resultsPath = Path.Combine(_stateDir, "results.json");
                try
                {
                    File.Delete(resultsPath);
                }
                catch (Exception e)
                {
                    // ignored
                }

                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(_transaction.Actions.Select(x => x.ToJson())));
                _view.RequestAdmin(jsonPath);

                return _view.OnCancelClicked().Subscribe(_ =>
                {
                    _cancelSource.Cancel();
                    _view.ProcessCancelled();
                    _view.ShowCompletion(true, false);
                });
            }
            else
            {
                return PrivilegedStart();
            }
        }
    }
}