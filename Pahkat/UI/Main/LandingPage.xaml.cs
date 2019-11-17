using Microsoft.Toolkit.Wpf.UI.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pahkat.Extensions;
using Pahkat.Sdk;
using Pahkat.UI.Shared;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pahkat.UI.Main
{
    struct RpcRequest
    {
        [JsonProperty("id")]
        public uint Id;

        [JsonProperty("method")]
        public string Method;

        [JsonProperty("args")]
        public object[] Args;
    }

    class WebViewChannel
    {

    }


    class Bridge
    {
        private WebView webView;
        private Page pageView;

        internal Bridge(WebView webView, Page pageView)
        {
            this.webView = webView;
            this.pageView = pageView;
        }

        private void SendResponse(uint id, object message)
        {
            var payload = JsonConvert.SerializeObject(message);
            try
            {
                var script = $"window.pahkatResponders[\"callback-{id}\"]({payload})";
                Console.WriteLine($"Running script: {script}");
                webView.InvokeScript(script);
                //webView.Navigate($"http://localhost:5000/#{script}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void SendError(uint id, string error)
        {
            dynamic errorPayload = new ExpandoObject();
            errorPayload.error = error;
            SendResponse(id, errorPayload);
        }

        private bool AssertValidRequest(RpcRequest request, Type[] expectedTypes)
        {
            if (request.Args.Length != expectedTypes.Length)
            {
                SendError(request.Id, "Invalid number of arguments");
                return false;
            }

            return true;
        }

        private struct LanguageResponse
        {
            [JsonProperty("languageName", Required = Required.Default)]
            public string LanguageName;

            [JsonProperty("packages", Required = Required.Default)]
            public Dictionary<PackageKey, Package> Packages;
        }

        private Dictionary<string, LanguageResponse> SearchByLanguage(object[] args)
        {
            var query = args.First().ToString();
            var app = ((PahkatApp)Application.Current);
            var indexes = app.PackageStore.RepoIndexes();

            var results = new Dictionary<string, LanguageResponse>();

            // TODO: this does the most naive possible search using only the language codes
            foreach (var index in indexes)
            {
                var tuples = index.Packages.Values
                    .Where(pkg => pkg.Languages.Any(lang => lang.StartsWith(query)))
                    .Select(pkg => (pkg.Languages.First(l => l.StartsWith(query)), index.PackageKeyFor(pkg), pkg));

                foreach (var t in tuples)
                {
                    var lang = t.Item1;
                    var packageKey = t.Item2;
                    var package = t.pkg;

                    if (!results.ContainsKey(lang))
                    {
                        results[lang] = new LanguageResponse
                        {
                            LanguageName = lang, // TODO
                            Packages = new Dictionary<PackageKey, Package>()
                        };
                    }

                    results[lang].Packages.Add(packageKey, package);
                }
            }

            return results;
        }

        private PackageKey[] PackageKeyArgs(object[] args)
        {
            var packageKeys = args.Select(x =>
            {
                if (x is string s)
                {
                    return PackageKey.New(new Uri(s));
                }
                return null;
            }).ToArray();


            if (packageKeys.Any(x => x == null))
            {
                throw new Exception("Install arguments must all be of type PackageKey");
            }

            return packageKeys;
        }

        private void Install(object[] args)
        {
            var packageKeys = PackageKeyArgs(args);
            var app = ((PahkatApp)Application.Current);

            var actions = packageKeys.Select(k => TransactionAction.Install(k, PackageTarget.System));
            var tx = Transaction.New(app.PackageStore, actions.ToList());

            // TODO modal popup security request
            app.UserSelection.Dispatch(new Models.SelectionEvent.SetPackages
            {
                Transaction = tx
            });

            pageView.ReplacePageWith(new DownloadPage(DownloadPagePresenter.Default));
        }

        private void Uninstall(object[] args)
        {
            var packageKeys = PackageKeyArgs(args);
            var app = ((PahkatApp)Application.Current);

            var actions = packageKeys.Select(k => TransactionAction.Install(k, PackageTarget.System));
            var tx = Transaction.New(app.PackageStore, actions.ToList());

            // TODO modal popup security request
            app.UserSelection.Dispatch(new Models.SelectionEvent.SetPackages
            {
                Transaction = tx
            });

            pageView.ReplacePageWith(new DownloadPage(DownloadPagePresenter.Default));
        }

        private Dictionary<PackageKey, Package> Packages(object[] args)
        {
            var packageKeys = PackageKeyArgs(args);
            var app = ((PahkatApp)Application.Current);
            return packageKeys
                .Select(k => (k, app.PackageStore.ResolvePackage(k)))
                .ToDictionary(t => t.k, t => t.Item2);
        }

        private string String(object[] args)
        {
            if (args.Length == 0)
            {
                throw new Exception("Localised strings require a key argument");
            }
            var argsList = args.ToList();
            var key = argsList[0] as string;

            if (key == null)
            {
                throw new Exception("Localised strings require a key argument");
            }
            argsList.RemoveAt(0);

            return Strings.ResourceManager.GetString(key, Strings.Culture);
        }

        public void HandleRequest(RpcRequest request)
        {
            try
            {
                object response = null;

                switch (request.Method)
                {
                    case "searchByLanguage":
                        response = SearchByLanguage(request.Args);
                        break;
                    case "install":
                        Install(request.Args);
                        break;
                    case "uninstall":
                        Uninstall(request.Args);
                        break;
                    case "packages":
                        response = Packages(request.Args);
                        break;
                    case "string":
                        response = String(request.Args);
                        break;
                    default:
                        break;
                }

                if (response != null)
                {
                    SendResponse(request.Id, response);
                }
            }
            catch (Exception e)
            {
                SendError(request.Id, e.Message);
            }
            
        }
    }

    internal class WebViewPolyfill
    {
        private Bridge bridge;

        internal WebViewPolyfill(Bridge bridge)
        {
            this.bridge = bridge;
        }

        void notify(string value)
        {
            var request = JsonConvert.DeserializeObject<RpcRequest>(value);
            this.bridge.HandleRequest(request);
        }
    }

    public class LegacyLandingPage : Page, IPageView
    {
        private WebBrowser webView;
        private Bridge bridge;

        LegacyLandingPage()
        {
            //InitializeComponent();
            webView = new WebBrowser();
            //grid.Children.Add(webView);
            //bridge = new Bridge(webView);

            webView.ObjectForScripting = new WebViewPolyfill(bridge);
        }
    }

    /// <summary>
    /// Interaction logic for LandingPage.xaml
    /// </summary>
    public partial class LandingPage : Page, IPageView
    {
        private WebView webView;
        private Bridge bridge;

        private void ProcessRequest(string rawRequest)
        {
            var rpcRequest = JsonConvert.DeserializeObject<RpcRequest>(rawRequest);
            bridge.HandleRequest(rpcRequest);
        }

        public LandingPage()
        {
            InitializeComponent();
            webView = new WebView();
            grid.Children.Add(webView);
            bridge = new Bridge(webView, this);

            webView.IsScriptNotifyAllowed = true;

            webView.ScriptNotify += (sender, args) =>
            {
                // Check args.Uri for something we want to actually act upon, for security.
            };

            this.Loaded += (sender, e) =>
            {
                var payload = Uri.EscapeUriString("{\"id\": 1, \"method\": \"searchByLanguage\", \"args\": [\"se\"]}");
                webView.NavigateToString($"<a href=\"about:pahkat:{payload}\">Install packages</a>");
                //webView.Navigate("http://127.0.0.1:5000/#whatsup");
            };

            webView.NavigationCompleted += (sender, args) =>
            {
                return;
            };

            webView.NavigationStarting += (sender, args) =>
            {
                if (args.Uri == null)
                {
                    return;
                }

                if (args.Uri.Scheme == "about")
                {
                    if (Uri.TryCreate(args.Uri.AbsolutePath, UriKind.Absolute, out var pahkatUri) && pahkatUri.Scheme == "pahkat")
                    {
                        args.Cancel = true;
                        DispatcherScheduler.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var payload = Uri.UnescapeDataString(pahkatUri.AbsolutePath);
                            ProcessRequest(payload);
                        });
                    }
                }
            };
        }
    }
}
