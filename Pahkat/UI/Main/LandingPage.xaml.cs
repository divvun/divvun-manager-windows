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

    class Bridge
    {
        private WebViewCompatible webView;

        internal Bridge(WebViewCompatible webView)
        {
            this.webView = webView;
        }

        private void SendResponse(uint id, object message)
        {
            var payload = JsonConvert.SerializeObject(message);
            webView.InvokeScript($"window.pahkatResponders[\"callback-{id}\"]({payload});");
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

            //var hasValidTypes = request.Args.Zip(expectedTypes, (a, b) => (a, b)).All(tuple =>
            //{
            //    var (arg, type) = tuple;
            //    return arg.GetType() == type;
            //});

            //if (!hasValidTypes)
            //{
            //    SendError(request.Id, "Invalid type for one of the arguments");
            //    return false;
            //}

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

        public void HandleRequest(RpcRequest request)
        {
            try
            {
                switch (request.Method)
                {
                    case "requestTransaction":
                        RequestTransaction(request.Args);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                this.SendError(request.Id, e.Message);
            }
            
        }
    }

    /// <summary>
    /// Interaction logic for LandingPage.xaml
    /// </summary>
    public partial class LandingPage : Page, IPageView
    {
        private WebViewCompatible webView;
        private Bridge bridge;

        private void ProcessRequest(string rawRequest)
        {
            var rpcRequest = JsonConvert.DeserializeObject<RpcRequest>(rawRequest);
            bridge.HandleRequest(rpcRequest);
        }

        public LandingPage()
        {
            InitializeComponent();
            webView = new WebViewCompatible();
            grid.Children.Add(webView);
            bridge = new Bridge(webView);

            this.Loaded += (sender, e) =>
            {
                var payload = "%7B%22method%22%3A%22requestTransaction%22%2C%22args%22%3A%5B%5B%7B%22action%22%3A%22install%22%2C%22target%22%3A%22system%22%2C%22package%22%3A%22https%3A%2F%2Fderp.tld%2Fsomething%2Fpackages%2Fahaha2%22%7D%5D%5D%2C%22id%22%3A42%7D";
                webView.NavigateToString($"<a href=\"about:pahkat:{payload}\">Install packages</a>");
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
