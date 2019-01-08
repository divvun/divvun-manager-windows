using Pahkat.Sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Pahkat.Models;
using Pahkat.Properties;
using Pahkat.UI.Main;
using Pahkat.Util;

namespace Pahkat.UI.SelfUpdate
{
    class SelfUpdatePresenter
    {
        private ISelfUpdateView _view;
        
        private PahkatClient _client;
        private RepositoryIndex _repo;
        private Package _package;
        private AbsolutePackageKey _key;
        private PackageStatusResponse _status;
        private bool _isInstalling;

//        private string _stateDir;
        
        public SelfUpdatePresenter(ISelfUpdateView view, PahkatClient client, bool isInstalling)
        {
            _view = view;
            _client = client;
            _repo = client.Repos()[0];
            _package = _repo.Packages[Constants.PackageId];
            _key = _repo.AbsoluteKeyFor(_package);
            _status = _repo.PackageStatus(_key);
            _isInstalling = isInstalling;
            
//            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//            _stateDir = Path.Combine(appdata, "Pahkat", "state");
        }

        public void Start(CompositeDisposable bag)
        {
            if (_isInstalling)
            {
                Install(bag);
            }
            else
            {
                Download(bag);
            }
        }

        private void Download(CompositeDisposable bag)
        {
            _client.Download(_key, _status.Target)
                .SubscribeOn(DispatcherScheduler.Current)
                .ObserveOn(DispatcherScheduler.Current)
                .Subscribe((progress) =>
                {
                    switch (progress.Status)
                    {
                        case PackageDownloadStatus.Progress:
                            _view.SetProgress(progress.Downloaded, progress.Total);
                            _view.SetSubtitle(
                                $"{Strings.Downloading} {Util.Util.BytesToString(progress.Downloaded)} / {Util.Util.BytesToString(progress.Total)}");
                            break;
                        case PackageDownloadStatus.Completed:
                            _view.IndeterminateProgress();
                            break;
                        case PackageDownloadStatus.Error:
                            _view.SetSubtitle(Strings.DownloadError);
                            break;
                        case PackageDownloadStatus.Starting:
                            _view.SetSubtitle(Strings.Starting);
                            break;
                    }
                }, (error) =>
                {
                    _view.SetSubtitle(Strings.DownloadError);
                    _view.HandleError(error);
                }, () => this.Install(bag)).DisposedBy(bag);
        }
        
        private void RequestAdmin()
        {
            var app = (PahkatApp)Application.Current;

            var args = $"-u";
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var process = new Process
            {
                StartInfo =
                {
                    FileName = path,
                    Arguments = args,
                    Verb = "runas"
                }
            };

            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                _view.HandleError(ex);
            }
        }

        private void Install(CompositeDisposable bag)
        {
            var path = _client.PackagePath(_key);
            Process.Start(path);
            Application.Current.Shutdown();
        }
    }
}
