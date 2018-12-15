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
        private readonly bool _requiresReboot;

        public CompletionPagePresenter(ICompletionPageView view, bool requiresReboot)
        {
            _view = view;
            _requiresReboot = requiresReboot;
        }

//        private void ErrorCheck()
//        {
//            var errors = _results.Where(r => !r.IsSuccess).ToArray();
//            
//            if (errors.Length > 0)
//            {
//                _view.ShowErrors(errors);
//            }
//        }

        private void RebootCheck()
        {
            _view.RequiresReboot(_requiresReboot);
        }

        public IDisposable Start()
        {
            RebootCheck();

            return new CompositeDisposable(
                _view.OnFinishButtonClicked().Subscribe(_ => _view.ShowMain()),
                _view.OnRestartButtonClicked().Subscribe(_ => _view.RebootSystem()));
        }
    }
}