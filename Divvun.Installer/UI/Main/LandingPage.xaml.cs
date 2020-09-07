using Microsoft.Toolkit.Wpf.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Iterable;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using Castle.Core.Internal;
using Divvun.Installer.Extensions;
using Divvun.Installer.Service;
using Divvun.Installer.UI.Shared;
using Divvun.Installer.Util;
using Flurl;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Models;
using Serilog;

using Iter = Iterable.Iterable;

namespace Divvun.Installer.UI.Main
{
    public struct WebBridgeRequest
    {
        [JsonProperty("id")] public uint Id;

        [JsonProperty("method")] public string Method;

        [JsonProperty("args")] public JArray Args;

        public override string ToString() {
            var args = Iter.ToArray(
                string.Join(", ", Args.Map(x => x.ToString())));
            return $"Id: {Id}, Method: {Method}, Args: {args}";
        }
    }

    class WebBridge
    {
        private WebView webView;
        private WebBridgeService.Functions? _functions;
        
        internal WebBridge(WebView webView) {
            this.webView = webView;
        }

        internal void SetRepository(ILoadedRepository repo) {
            _functions = new WebBridgeService.Functions(repo, webView);
        }
        
        private void SendResponse(uint id, object message) {
            string payload = HttpUtility.JavaScriptStringEncode(
                JsonConvert.SerializeObject(message, Json.Settings.Value), true);
            try {
                var script = $"window.pahkatResponders[\"callback-{id}\"]({payload})";
                Log.Debug($"Running script: {script}");
                webView.InvokeScript("eval", new string[] {script});
            }
            catch (Exception e) {
                Log.Debug(e, "error sending response");
            }
        }

        public async Task HandleRequest(WebBridgeRequest request) {
            Log.Debug(request.ToString());

            if (_functions == null) {
                Log.Error("No functions defined for WebBridge");
                return;
            }

            await PahkatApp.Current.Dispatcher.InvokeAsync(async () => {
                try {
                    var response = await _functions.Process(request);
                    SendResponse(request.Id, response);
                }
                catch (WebBridgeException e) {
                    if (Debugger.IsAttached) {
                        throw;
                    }
                    SendResponse(request.Id, e);
                }
                catch (Exception e) {
                    if (Debugger.IsAttached) {
                        throw;
                    }
                    SendResponse(request.Id, new WebBridgeException("Internal error"));
                }
            });
        }
    }

    /// <summary>
    /// Interaction logic for LandingPage.xaml
    /// </summary>
    public partial class LandingPage : Page, IPageView, IDisposable
    {
        private CompositeDisposable _bag = new CompositeDisposable();

        private WebView _webView;
        private WebBridge _webBridge = null!;

        public LandingPage() {
            InitializeComponent();
            _webView = new WebView();
            WebViewGrid.Children.Add(_webView);
        }

        private void ShowNoLandingPage() {
            Log.Warning("No landing page");
            
            var app = (PahkatApp) Application.Current;
            app.WindowService.Show<MainWindow>(new MainPage());
        }

        private async void SetRepository(Uri? url) {
            var app = (PahkatApp) Application.Current;

            var pahkat = app.PackageStore;
            var repos = await pahkat.RepoIndexes();
            var records = await pahkat.GetRepoRecords();

            TitleBarHandler.RefreshFlyoutItems(TitleBarReposButton, TitleBarReposFlyout, 
                Iter.ToArray(repos.Values), records);


            await app.Dispatcher.InvokeAsync(() => {
                ILoadedRepository? repo = null;
                if (url == null) {
                    if (records.IsNullOrEmpty()) {
                        ShowNoLandingPage();
                        return;
                    }

                    if (!repos.Values.IsNullOrEmpty()) {
                        repo = Iter.First(repos.Values, r => records.ContainsKey(r.Index.Url));
                    }

                    if (repo == null) {
                        ShowNoLandingPage();
                        return;
                    }
                } else if (url.Scheme == "divvun-installer") {
                    if (url.AbsolutePath == "detailed") {
                        ShowNoLandingPage();
                        return;
                    }
                } else {
                    if (!repos.Values.IsNullOrEmpty()) {
                        repo = Iter.First(repos.Values, r => r.Index.Url == url);
                        repo ??= Iter.First(repos.Values, r => records.ContainsKey(r.Index.Url));
                    }
                    
                    if (repo == null) {
                        ShowNoLandingPage();
                        return;
                    }
                }
                
                if (repo == null) {
                    app.Settings.Mutate(file => {
                        Log.Warning("No repository found, setting selected repo to null");
                        file.SelectedRepository = null;
                    });
                    return;
                }

                if (repo.Index.LandingUrl == null) {
                    ShowNoLandingPage();
                    return;
                }

                TitleBarReposButton.Content = repo.Index.NativeName();
                _webBridge.SetRepository(repo);
                _webView.Navigate(repo.Index.LandingUrl.SetQueryParam("ts", DateTimeOffset.UtcNow));
            });
        }

        public void Dispose() {
            _bag.Dispose();
            _webView.Dispose();
        }
        
        private async Task ProcessRequest(string rawRequest) {
            var rpcRequest = JsonConvert.DeserializeObject<WebBridgeRequest>(rawRequest);
            await _webBridge.HandleRequest(rpcRequest);
        }
        
        private void ConfigureWebView() {
            _webBridge = new WebBridge(_webView);
        
            _webView.IsScriptNotifyAllowed = true;
        
            _webView.ScriptNotify += async (sender, args) => {
                // Check args.Uri for something we want to actually act upon, for security.
                Log.Debug("{uri}", args.Uri);
                Log.Debug(args.Value);
        
                await ProcessRequest(args.Value);
            };
        
            _webView.NavigationCompleted += (sender, args) => { return; };
        
            _webView.NavigationStarting += (sender, args) => {
                if (args.Uri == null) {
                    return;
                }
        
                if (args.Uri.Scheme == "about") {
                    if (Uri.TryCreate(args.Uri.AbsolutePath, UriKind.Absolute, out var pahkatUri) &&
                        pahkatUri.Scheme == "pahkat") {
                        args.Cancel = true;
                        PahkatApp.Current.Dispatcher.InvokeAsync(async () => {
                            var payload = Uri.UnescapeDataString(pahkatUri.AbsolutePath);
                            await ProcessRequest(payload);
                        });
                    }
                }
            };
        }

        void OnLoaded(object sender, RoutedEventArgs e) {
            _bag = new CompositeDisposable();
            TitleBarHandler.BindRepoDropdown(_bag, SetRepository);
            
            var app = (PahkatApp) Application.Current;
            app.PackageStore.Notifications()
                .Filter(x => x == Notification.RepositoriesChanged)
                .ObserveOn(app.Dispatcher)
                .SubscribeOn(app.Dispatcher)
                .StartWith(Notification.RepositoriesChanged)
                .Subscribe(x => {
                    ConfigureWebView();
                })
                .DisposedBy(_bag);
        }

        void OnUnloaded(object sender, RoutedEventArgs e) {
            _bag.Dispose();
        }
        
        private void OnClickBtnMenu(object sender, RoutedEventArgs e) {
            if (BtnMenu.ContextMenu.IsOpen) {
                BtnMenu.ContextMenu.IsOpen = false;
                return;
            }

            BtnMenu.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            BtnMenu.ContextMenu.PlacementTarget = BtnMenu;
            BtnMenu.ContextMenu.IsOpen = true;
        }

        private void OnClickAboutMenuItem(object sender, RoutedEventArgs e) {
            TitleBarHandler.OnClickAboutMenuItem(sender, e);
        }
        
        private void OnClickSettingsMenuItem(object sender, RoutedEventArgs e) {
            TitleBarHandler.OnClickSettingsMenuItem(sender, e);
        }

        private void OnClickExitMenuItem(object sender, RoutedEventArgs e) {
            TitleBarHandler.OnClickExitMenuItem(sender, e);
        }

        private void OnClickBundleLogsItem(object sender, RoutedEventArgs e) {
            TitleBarHandler.OnClickBundleLogsItem(sender, e);
        }
    }
}