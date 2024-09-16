using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Castle.Core.Internal;
using CefSharp;
using CefSharp.Wpf;
using Divvun.Installer.Extensions;
using Divvun.Installer.Service;
using Divvun.Installer.UI.Shared;
using Flurl;
using Iterable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Models;
using Serilog;
using Iter = Iterable.Iterable;

namespace Divvun.Installer.UI.Main {

public struct WebBridgeRequest {
    [JsonProperty("id")] public uint Id;

    [JsonProperty("method")] public string Method;

    [JsonProperty("args")] public JArray Args;

    public override string ToString() {
        var args = string.Join(", ", Args.Map(x => x.ToString())).ToArray();
        return $"Id: {Id}, Method: {Method}, Args: {args}";
    }
}

internal class WebBridge {
    private WebBridgeService.Functions? _functions;
    private readonly ChromiumWebBrowser webView;

    internal WebBridge(ChromiumWebBrowser webView) {
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
            webView.ExecuteScriptAsync("eval", script);
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
///     Interaction logic for LandingPage.xaml
/// </summary>
public partial class LandingPage : Page, IPageView, IDisposable {
    private CompositeDisposable _bag = new CompositeDisposable();
    private WebBridge _webBridge = null!;

    private readonly ChromiumWebBrowser _webView;

    public LandingPage() {
        InitializeComponent();
        _webView = new ChromiumWebBrowser();
        WebViewGrid.Children.Add(_webView);
    }

    public void Dispose() {
        _bag.Dispose();
        _webView.Dispose();
    }

    public void HideWebview() {
        _webView.Visibility = Visibility.Hidden;
    }

    public void ShowWebview() {
        _webView.Visibility = Visibility.Visible;
    }

    private void ShowNoLandingPage() {
        Log.Warning("No landing page");

        var app = (PahkatApp)Application.Current;
        app.WindowService.Show<MainWindow>(new MainPage());
    }

    private async void SetRepository(Uri? url) {
        var app = (PahkatApp)Application.Current;

        var pahkat = app.PackageStore;

        try
        {
            var repos = await pahkat.RepoIndexes();
            var records = await pahkat.GetRepoRecords();

            TitleBarHandler.RefreshFlyoutItems(TitleBarReposButton, TitleBarReposFlyout,
                repos.Values.ToArray(), records);

            await app.Dispatcher.InvokeAsync(() =>
            {
                ILoadedRepository? repo = null;
                if (url == null)
                {
                    if (records.IsNullOrEmpty())
                    {
                        ShowNoLandingPage();
                        return;
                    }

                    if (!repos.Values.IsNullOrEmpty())
                    {
                        repo = repos.Values.First(r => records.ContainsKey(r.Index.Url));
                    }

                    if (repo == null)
                    {
                        ShowNoLandingPage();
                        return;
                    }
                }
                else if (url.Scheme == "divvun-installer")
                {
                    if (url.AbsolutePath == "detailed")
                    {
                        ShowNoLandingPage();
                        return;
                    }
                }
                else
                {
                    if (!repos.Values.IsNullOrEmpty())
                    {
                        repo = repos.Values.First(r => r.Index.Url == url);
                        repo ??= repos.Values.First(r => records.ContainsKey(r.Index.Url));
                    }

                    if (repo == null)
                    {
                        ShowNoLandingPage();
                        return;
                    }
                }

                if (repo == null)
                {
                    app.Settings.Mutate(file =>
                    {
                        Log.Warning("No repository found, setting selected repo to null");
                        file.SelectedRepository = null;
                    });
                    return;
                }

                if (repo.Index.LandingUrl == null)
                {
                    ShowNoLandingPage();
                    return;
                }

                TitleBarReposButton.Content = repo.Index.NativeName();
                _webBridge.SetRepository(repo);
                _webView.Load(repo.Index.LandingUrl.SetQueryParam("ts", DateTimeOffset.UtcNow));
            });
        }
        catch (PahkatServiceException ex)
        {
            var current = (PahkatApp)Application.Current;

            if (!current.IsShutdown) {
                current.IsShutdown = true;
                    switch (ex)
                    {
                        case PahkatServiceConnectionException _:
                            MessageBox.Show(Strings.PahkatServiceConnectionException);
                            break;
                        case PahkatServiceNotRunningException _:
                            MessageBox.Show(Strings.PahkatServiceNotRunningException);
                            break;
                    }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown(1);
                    }
                );
            }
        }
    }

    private async Task ProcessRequest(string rawRequest) {
        var rpcRequest = JsonConvert.DeserializeObject<WebBridgeRequest>(rawRequest);
        await _webBridge.HandleRequest(rpcRequest);
    }

    private void ConfigureWebView() {
        _webBridge = new WebBridge(_webView);

        _webView.JavascriptMessageReceived += async (sender, args) => {
            // Check args.Uri for something we want to actually act upon, for security.
            // Log.Debug("{uri}", args.Frame.Url);
            // Log.Debug(args.Frame.Url);

            await ProcessRequest(args.Message as string);
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        _bag = new CompositeDisposable();
        TitleBarHandler.BindRepoDropdown(_bag, SetRepository);

        var app = (PahkatApp)Application.Current;
        app.PackageStore.Notifications()
            .Filter(x => x == Notification.RepositoriesChanged)
            .ObserveOn(app.Dispatcher)
            .SubscribeOn(app.Dispatcher)
            .StartWith(Notification.RepositoriesChanged)
            .Subscribe(x => { ConfigureWebView(); })
            .DisposedBy(_bag);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        _bag.Dispose();
    }

    private void OnClickBtnMenu(object sender, RoutedEventArgs e) {
        if (BtnMenu.ContextMenu.IsOpen) {
            BtnMenu.ContextMenu.IsOpen = false;
            return;
        }

        BtnMenu.ContextMenu.Placement = PlacementMode.Bottom;
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