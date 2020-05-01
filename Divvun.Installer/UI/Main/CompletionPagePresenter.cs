using System;
using System.Linq;
using System.Reactive.Disposables;
using Divvun.Installer.Models;
using Divvun.Installer.Service;

namespace Divvun.Installer.UI.Main
{
    public class CompletionPagePresenter
    {
        private readonly ICompletionPageView _view;
        private readonly bool _requiresReboot;

        public CompletionPagePresenter(ICompletionPageView view, bool requiresReboot) {
            _view = view;
            _requiresReboot = requiresReboot;
        }

        private void RebootCheck() {
            _view.RequiresReboot(_requiresReboot);
        }

        public IDisposable Start() {
            RebootCheck();

            return new CompositeDisposable(
                _view.OnFinishButtonClicked().Subscribe(_ => _view.ShowMain()),
                _view.OnRestartButtonClicked().Subscribe(_ => _view.RebootSystem()));
        }
    }
}