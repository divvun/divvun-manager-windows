using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Pahkat.Sdk;
using PahkatUpdater;
using PahkatUpdater.UI;

namespace PahkatUpdater.UI
{
    class SelfUpdatePresenter
    {
        private ISelfUpdateView _view;
        
        private PahkatClient _client;
        private RepositoryIndex _repo;
        private Package _package;
        private AbsolutePackageKey _key;
        private PackageStatusResponse _status;
        
        private string _installDir;
        
        public SelfUpdatePresenter(ISelfUpdateView view, PahkatClient client, string installDir)
        {
            _view = view;
            _client = client;
            _repo = client.Repos()[0];
            _package = _repo.Packages[Constants.PackageId];
            _key = _repo.AbsoluteKeyFor(_package);
            _status = _repo.PackageStatus(_key);
            _installDir = installDir;
        }

        public void Start(CompositeDisposable bag)
        {
            Download(bag);
        }

        private void Download(CompositeDisposable bag)
        {
            bag.Add(_client.Download(_key, _status.Target)
                .SubscribeOn(DispatcherScheduler.Current)
                .ObserveOn(DispatcherScheduler.Current)
                .Subscribe((progress) =>
                {
                    switch (progress.Status)
                    {
                        case PackageDownloadStatus.Progress:
                            _view.SetProgress(progress.Downloaded, progress.Total);
                            _view.SetSubtitle(
                                $"{Strings.Downloading} {BytesToString(progress.Downloaded)} / {BytesToString(progress.Total)}");
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
                    var app = ((App) Application.Current);
                    _view.SetSubtitle(Strings.DownloadError);
                    app.ShowError(error.Message);
                }, () => this.Install(bag)));
        }

        private void Install(CompositeDisposable bag)
        {
            var app = ((App) Application.Current);
            _view.SetSubtitle(string.Format(Strings.InstallingPackage, Strings.AppName, _package.Version));
            //var path = app.Client.PackagePath(_key);
            //try
            //{
            //    var p = Process.Start(path);
            //    p.WaitForExit();
            //}
            //catch (Exception ex)
            //{
            //    app.ShowError(ex.Message);
            //}

            //app.StartPahkat();
            //app.Shutdown();

            var tx = new TransactionAction(PackageActionType.Install, _key, InstallerTarget.System);
            bag.Add(_client.Transaction(new[] {tx}).Process()
                .SubscribeOn(DispatcherScheduler.Current)
                .ObserveOn(DispatcherScheduler.Current)
                .Subscribe((evt =>
                {
                    switch (evt.Event)
                    {
                        case PackageEventType.Installing:
                            _view.SetSubtitle(string.Format(Strings.InstallingPackage, Strings.AppName,
                                _package.Version));
                            break;
                        case PackageEventType.Error:
                            _view.SetSubtitle(Strings.Error);
                            app.ShowError("An error occurred installing the package.");
                            app.StartPahkat();
                            break;
                    }
                }), (error) =>
                {
                    app.ShowError(error.Message);
                    //app.StartPahkat();
                    //Application.Current.Shutdown();
                }, () =>
                {
                    app.StartPahkat();
                    Application.Current.Shutdown();
                }));
            
            // This is the only line that will stop the installer from blocking and make you start to cry deeply.
            app.SendReady();
        }
        
        public static string BytesToString(ulong bytes)
        {
            return BytesToString((long) bytes);
        }

        public static string BytesToString(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (bytes == 0)
            {
                return "0 " + suf[0];
            }
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            if (place >= suf.Length)
            {
                return "--";
            }
            var num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return num.ToString(CultureInfo.CurrentCulture) + " " + suf[place];
        }
    }
}
