using Microsoft.Toolkit.Wpf.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using Divvun.Installer.Service;
using Divvun.Installer.UI.Shared;
using Divvun.Installer.Util;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;
using Serilog;

namespace Divvun.Installer.UI.Main
{
    public struct WebBridgeRequest
    {
        [JsonProperty("id")] public uint Id;

        [JsonProperty("method")] public string Method;

        [JsonProperty("args")] public JArray Args;

        public override string ToString() {
            return $"Id: {Id}, Method: {Method}, Args: {string.Join(", ", Args.Select(x => x.ToString()).ToArray())}";
        }
    }

    class WebBridge
    {
        private WebView webView;
        private WebBridgeService.Functions? _functions;
        
        internal WebBridge(WebView webView) {
            this.webView = webView;
        }

        internal void SetRepository(LoadedRepository repo) {
            _functions = new WebBridgeService.Functions(repo);
        }
        
        private void SendResponse(uint id, object message) {
            string payload = HttpUtility.JavaScriptStringEncode(
                JsonConvert.SerializeObject(message, Json.Settings.Value), true);
            try {
                var script = $"window.pahkatResponders[\"callback-{id}\"]({payload})";
                Console.WriteLine($"Running script: {script}");
                webView.InvokeScript("eval", new string[] {script});
                //webView.Navigate($"http://localhost:5000/#{script}");
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        public async void HandleRequest(WebBridgeRequest request) {
            Console.WriteLine(request.ToString());

            if (_functions == null) {
                Log.Error("No functions defined for WebBridge");
                return;
            }
            
            try {
                var response = await _functions.Process(request);
                SendResponse(request.Id, response);
            }
            catch (WebBridgeException e) {
                SendResponse(request.Id, e);
            }
            catch (Exception e) {
                SendResponse(request.Id, new WebBridgeException("Internal error"));
            }
        }
    }

    // internal class WebViewPolyfill
    // {
    //     private WebBridge _webBridge;
    //
    //     internal WebViewPolyfill(WebBridge webBridge) {
    //         this._webBridge = webBridge;
    //     }
    //
    //     void notify(string value) {
    //         var request = JsonConvert.DeserializeObject<WebBridgeRequest>(value);
    //         this._webBridge.HandleRequest(request);
    //     }
    // }

    // public class LegacyLandingPage : Page, IPageView
    // {
    //     private WebBrowser webView;
    //     private WebBridge _webBridge;
    //
    //     LegacyLandingPage() {
    //         //InitializeComponent();
    //         webView = new WebBrowser();
    //         //grid.Children.Add(webView);
    //         //bridge = new Bridge(webView);
    //
    //         webView.ObjectForScripting = new WebViewPolyfill(_webBridge);
    //     }
    // }

    /// <summary>
    /// Interaction logic for LandingPage.xaml
    /// </summary>
    public partial class LandingPage : Page, IPageView, IDisposable
    {
        private CompositeDisposable _bag = new CompositeDisposable();
        
        private WebView _webView = null!;
        private WebBridge _webBridge = null!;

        IObservable<LoadedRepository?> OnRepoSelectionChanged {
            get {
                var app = (PahkatApp) Application.Current;
                return app.Settings.SelectedRepository
                    .Select(url => {
                        using var guard = app.PackageStore.Lock();
                        var repos = guard.Value.RepoIndexes();

                        if (url == null) {
                            return repos.Values.First();
                        }

                        var repo = repos.Values.First(x => x.Index.Url == url);
                        return repo ?? repos.Values.First();
                    });
            }
        }

        private void BindRepoDropdown() {
            OnRepoSelectionChanged
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                .Subscribe(SetRepository)
                .DisposedBy(_bag);
        }

        public LandingPage() {
            InitializeComponent();
        }

        private void ShowNoLandingPage() {
            Log.Warning("No landing page");
            
        }

        private void SetRepository(LoadedRepository? repo) {
            var app = (PahkatApp) Application.Current;
            
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

            _webBridge.SetRepository(repo);
            _webView.Navigate(repo.Index.LandingUrl);
        }

        public void Dispose() {
            _bag.Dispose();
            _webView.Dispose();
        }
        
        private void ProcessRequest(string rawRequest) {
            var rpcRequest = JsonConvert.DeserializeObject<WebBridgeRequest>(rawRequest);
            _webBridge.HandleRequest(rpcRequest);
        }
        
        private void ConfigureWebView() {
            _webView = new WebView();
            grid.Children.Add(_webView);
            _webBridge = new WebBridge(_webView);
        
            _webView.IsScriptNotifyAllowed = true;
        
            _webView.ScriptNotify += (sender, args) => {
                // Check args.Uri for something we want to actually act upon, for security.
                Console.WriteLine(args.Uri);
                Console.WriteLine(args.Value);
        
                ProcessRequest(args.Value);
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
                        DispatcherScheduler.Current.Dispatcher.InvokeAsync(() => {
                            var payload = Uri.UnescapeDataString(pahkatUri.AbsolutePath);
                            ProcessRequest(payload);
                        });
                    }
                }
            };
        }

        void OnLoaded(object sender, RoutedEventArgs e) {
            ConfigureWebView();
            BindRepoDropdown();
        }
    }
}