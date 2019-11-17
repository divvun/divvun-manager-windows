using Microsoft.Toolkit.Wpf.UI.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk;
using Pahkat.UI.Shared;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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

        internal Bridge(WebView webView)
        {
            this.webView = webView;
        }

        private void SendResponse(uint id, object message)
        {
            var payload = JsonConvert.SerializeObject(message);
            try
            {
                var script = $"window.pahkatResponders[\"callback-{id}\"]({payload})";
                Console.WriteLine($"Running script: {script}");
                //webView.InvokeScript(script);
                webView.Navigate($"http://localhost:5000/#{script}");
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

        private void RequestTransaction(object[] args)
        {
            var app = ((PahkatApp)Application.Current);

            JArray actionsJson = args[0] as JArray;
            var actions = actionsJson.Select((actionJson) =>
            {
                return actionJson.ToObject<TransactionAction>();
            }).ToList();

            var transaction = Transaction.New(app.PackageStore, actions);

            // TODO: prompt user for permission to do actions.
        }

        private string Repos()
        {
            var app = ((PahkatApp)Application.Current);
            var repos = JsonConvert.SerializeObject(app.PackageStore.RepoIndexes());
            return repos;
        }

        public void HandleRequest(RpcRequest request)
        {
            try
            {
                string response = null;

                switch (request.Method)
                {
                    case "requestTransaction":
                        RequestTransaction(request.Args);
                        break;
                    case "repos":
                        response = Repos();
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
            bridge = new Bridge(webView);

            webView.IsScriptNotifyAllowed = true;

            webView.ScriptNotify += (sender, args) =>
            {
                // Check args.Uri for something we want to actually act upon, for security.
            };

            //this.Loaded += (sender, e) =>
            //{
            //    var payload = "%7B%22method%22:%22requestTransaction%22,%22args%22:%5B%5B%7B%22action%22:%22install%22,%22target%22:%22system%22,%22package%22:%22https://derp.tld/something/packages/ahaha2%22%7D%5D%5D,%22id%22:42%7D";
            //    //webView.NavigateToString($"<a href=\"about:pahkat:{payload}\">Install packages</a>");
            //    webView.Navigate("http://127.0.0.1:5000/#whatsup");
            //};

            //webView.NavigationCompleted += (sender, args) =>
            //{
            //    return;
            //};

            //webView.NavigationStarting += (sender, args) =>
            //{
            //    if (args.Uri == null)
            //    {
            //        return;
            //    }

            //    if (args.Uri.Scheme == "about")
            //    {
            //        if (Uri.TryCreate(args.Uri.AbsolutePath, UriKind.Absolute, out var pahkatUri) && pahkatUri.Scheme == "pahkat")
            //        {
            //            args.Cancel = true;
            //            DispatcherScheduler.Current.Dispatcher.InvokeAsync(() =>
            //            {
            //                var payload = Uri.UnescapeDataString(pahkatUri.AbsolutePath);
            //                ProcessRequest(payload);
            //            });
            //        }
            //    }
            //};
        }
    }
}
