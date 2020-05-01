using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Divvun.Installer.Service;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Windows;
using Divvun.Installer.Models;
using Divvun.Installer.Sdk;

namespace Divvun.Installer.UI.Main
{
    public class DownloadPagePresenter
    {
        public static DownloadPagePresenter Default(IDownloadPageView view) {
            var app = (PahkatApp) Application.Current;
            return new DownloadPagePresenter(view, app.UserSelection);
        }

        private ObservableCollection<DownloadListItem> _listItems =
            new ObservableCollection<DownloadListItem>();

        private readonly IDownloadPageView _view;
        private readonly IUserPackageSelectionStore _userSelection;
        private readonly CancellationTokenSource _cancelSource;

        public DownloadPagePresenter(IDownloadPageView view, IUserPackageSelectionStore userSelection) {
            _view = view;
            _userSelection = userSelection;
            _cancelSource = new CancellationTokenSource();
        }

        public IDisposable Start() {
            _view.InitProgressList(_listItems);

            var cancel = _view.OnCancelClicked()
                .Subscribe(_ => {
                    _cancelSource.Cancel();
                    _view.DownloadCancelled();
                });

            return cancel;
        }
    }
}