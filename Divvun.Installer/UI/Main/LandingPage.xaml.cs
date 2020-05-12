using Microsoft.Toolkit.Wpf.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Linq;
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
using Pahkat.Sdk.Rpc;
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
            _functions = new WebBridgeService.Functions(repo, webView);
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

        public async Task HandleRequest(WebBridgeRequest request) {
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

        private WebView _webView;
        private WebBridge _webBridge = null!;

        private IObservable<Notification> OnReposChanged() {
            var app = (PahkatApp) Application.Current;
            using var guard = app.PackageStore.Lock();
            return guard.Value.Notifications()
                .Where(x => x == Notification.RepositoriesChanged)
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                .StartWith(Notification.RepositoriesChanged);
        }

        private void BindRepoDropdown() {
            var app = (PahkatApp) Application.Current;
            OnReposChanged()
                .CombineLatest(app.Settings.SelectedRepository, (a, b) => b)
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                .Subscribe(SetRepository)
                .DisposedBy(_bag);
        }

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

        private void SetRepository(Uri? url) {
            var app = (PahkatApp) Application.Current;
            using var guard = app.PackageStore.Lock();
            
            RefreshFlyoutItems();
            
            var repos = guard.Value.RepoIndexes();
            LoadedRepository? repo = null;
            if (url == null) {
                if (repos.IsNullOrEmpty()) {
                    ShowNoLandingPage();
                    return;
                }

                repo = repos.Values.First();
            } else if (url.Scheme == "divvun-installer") {
                if (url.AbsolutePath == "detailed") {
                    ShowNoLandingPage();
                    return;
                }
            } else {
                repo = repos.Values.First(x => x.Index.Url == url);
                repo ??= repos.Values.First();
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
                Console.WriteLine(args.Uri);
                Console.WriteLine(args.Value);
        
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
                        DispatcherScheduler.Current.Dispatcher.InvokeAsync(async () => {
                            var payload = Uri.UnescapeDataString(pahkatUri.AbsolutePath);
                            await ProcessRequest(payload);
                        });
                    }
                }
            };
        }

        private void RefreshFlyoutItems() {
            TitleBarHandler.RefreshFlyoutItems(TitleBarReposFlyout);
        }

        void OnLoaded(object sender, RoutedEventArgs e) {
            BindRepoDropdown();
            
            var app = (PahkatApp) Application.Current;
            using var guard = app.PackageStore.Lock();
            guard.Value.Notifications()
                .Where(x => x == Notification.RepositoriesChanged)
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                .StartWith(Notification.RepositoriesChanged)
                .Subscribe(x => {
                    ConfigureWebView();
                })
                .DisposedBy(_bag);
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
    }
}