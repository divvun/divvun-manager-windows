using System;
using System.Linq;
using System.Reactive.Disposables;
using Pahkat.Models;
using Pahkat.Service;

namespace Pahkat.UI.Main
{
    public class CompletionPagePresenter
    {
        private readonly ICompletionPageView _view;
        private readonly ProcessResult[] _results;
        
        public CompletionPagePresenter(ICompletionPageView view, ProcessResult[] results)
        {
            _view = view;
            _results = results;
        }

        private void ErrorCheck()
        {
            var errors = _results.Where(r => !r.IsSuccess).ToArray();
            
            if (errors.Length > 0)
            {
                _view.ShowErrors(errors);
            }
        }

        private void RebootCheck()
        {
            _view.RequiresReboot(_results.Any(r =>
            {
                var inst = r.Package.WindowsInstaller;
                return r.Action == PackageAction.Install
                    ? inst.RequiresReboot
                    : inst.RequiresUninstallReboot;
            }));
        }

        public IDisposable Start()
        {
            ErrorCheck();
            RebootCheck();

            return new CompositeDisposable(
                _view.OnFinishButtonClicked().Subscribe(_ => _view.ShowMain()),
                _view.OnRestartButtonClicked().Subscribe(_ => _view.RebootSystem()));
        }
    }
}