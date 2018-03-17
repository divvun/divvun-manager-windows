using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Newtonsoft.Json;
using Pahkat.Service;

namespace Pahkat.UI.Main
{
    public struct InstallSaveState
    {
        public bool IsCancelled;
        public ProcessResult[] Results;
    }

    public class InstallPagePresenter
    {
        private readonly IInstallPageView _view;
        private readonly IInstallService _instServ;
        private readonly PackageProcessInfo _pkgInfo;
        private readonly IScheduler _scheduler;
        private readonly CancellationTokenSource _cancelSource;
        private readonly string _stateDir;
        
        public InstallPagePresenter(IInstallPageView view,
            PackageProcessInfo pkgInfo,
            IInstallService instServ, 
            IScheduler scheduler)
        {
            _view = view;
            _pkgInfo = pkgInfo;
            _instServ = instServ;
            _scheduler = scheduler;
            
            _cancelSource = new CancellationTokenSource();


            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _stateDir = Path.Combine(appdata, "Pahkat", "state");
        }

        public void SaveResultsState(InstallSaveState state)
        {
            Directory.CreateDirectory(_stateDir);
            var jsonPath = Path.Combine(_stateDir, "results.json");
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(state));
        }

        public InstallSaveState ReadResultsState()
        {
            var jsonPath = Path.Combine(_stateDir, "results.json");
            return JsonConvert.DeserializeObject<InstallSaveState>(File.ReadAllText(jsonPath));
        }

        public IDisposable Start()
        {
            if (!Util.Util.IsAdministrator())
            {
                Directory.CreateDirectory(_stateDir);
                var jsonPath = Path.Combine(_stateDir, "install.json");
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(_pkgInfo));
                _view.RequestAdmin(jsonPath);

                return _view.OnCancelClicked().Subscribe(_ =>
                {
                    _cancelSource.Cancel();
                    _view.ProcessCancelled();
                    _view.ShowCompletion(true, null);
                });
            }

            var onStartPackageSubject = new Subject<OnStartPackageInfo>();
            _view.SetTotalPackages(_pkgInfo.ToInstall.LongLength + _pkgInfo.ToUninstall.LongLength);

            return new CompositeDisposable(
                // Handles forwarding progress status to the UI
                onStartPackageSubject
                    .ObserveOn(_scheduler)
                    .SubscribeOn(_scheduler)
                    .Subscribe(_view.SetCurrentPackage, _view.HandleError),
                // Processes the packages (install and uninstall)
                _instServ.Process(_pkgInfo, onStartPackageSubject, _cancelSource.Token)
                    .ToArray()
                    .SubscribeOn(_scheduler)
                    .ObserveOn(_scheduler)
                    .Subscribe(results =>
                    {
                        _view.ShowCompletion(_cancelSource.IsCancellationRequested, results);
                    }, _view.HandleError),
                // Cancel button binding
                _view.OnCancelClicked().Subscribe(_ =>
                {
                    _cancelSource.Cancel();
                    _view.ProcessCancelled();
                }),
                // Dispose the subject itself
                onStartPackageSubject
            );
        }
    }
}