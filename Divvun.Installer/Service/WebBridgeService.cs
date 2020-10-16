using System;
using System.Collections.Generic;
using Iterable;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Divvun.Installer.Extensions;
using Divvun.Installer.UI.Main;
using Divvun.Installer.UI.Main.Dialog;
using ModernWpf.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;
using Pahkat.Sdk.Rpc.Models;
using Serilog;
using CefSharp.Wpf;

namespace Divvun.Installer.Service
{
    public class WebBridgeException : Exception
    {
        public string Error;
        
        public WebBridgeException(string error) : base(error) {
            Error = error;
        }
    }
    
    public class WebBridgeService
    {
        public class Functions
        {
            public ILoadedRepository Repo;
            public ChromiumWebBrowser WebView;
            
            public Functions(ILoadedRepository repo, ChromiumWebBrowser webView) {
                Repo = repo;
                WebView = webView;
            }

            public async Task<object> Process(WebBridgeRequest request) {
                return await Task.Run(async () => {
                    Log.Verbose("Request {request}", request);
                    switch (request.Method) {
                        case "env":
                            return Env(request.Args);
                        case "string":
                            return String(request.Args);
                        case "packages":
                            return await Packages(request.Args);
                        case "status":
                            return await Status(request.Args);
                        case "transaction":
                            return await Transaction(request.Args);
                    }

                    throw new WebBridgeException($"Unhandled method: {request.Method}");
                });
            }

            private object Env(JArray args) {
                var obj = new JObject();
                obj["platform"] = "windows";
                obj["osVersion"] = Environment.OSVersion.VersionString;

                var arch = RuntimeInformation.ProcessArchitecture;
                switch (arch)
                { 
                    case Architecture.X64:
                        obj["arch"] = "x86_64";
                        break;
                    case Architecture.X86:
                        obj["arch"] = "i686";
                        break;
                    case Architecture.Arm:
                        obj["arch"] = "arm7a";
                        break;
                    case Architecture.Arm64:
                        obj["arch"] = "aarch64";
                        break;
                }

                return obj;
            }

            private object String(JArray args) {
                throw new WebBridgeException($"No string for the identifier");
            }

            private async Task<object> Status(JArray args) {
                var app = (PahkatApp) Application.Current;
                var keys = new List<PackageKey>();
                
                foreach (var arg in args) {
                    var rawKey = arg.ToObject<string>();
                    if (rawKey != null) {
                        try {
                            keys.Add(PackageKey.From(rawKey));
                        } catch {
                            continue;
                        }
                    }
                }

                var map = new JObject();
                var pahkat = app.PackageStore;

                foreach (var packageKey in keys) {
                    var status = await pahkat.Status(packageKey);
                    var obj = new JObject();
                    obj["status"] = (int) status;
                    obj["target"] = "system";
                    map[packageKey.ToString()] = obj;
                }

                return map;
            }

            private async Task<bool> Transaction(JArray args) {
                var actions = new List<PackageAction>();

                foreach (var arg in args) {
                    if (arg.Type != JTokenType.Object) {
                        continue;
                    }

                    JObject obj = (JObject) arg;
                    var rawKey = obj.Value<string>("key");
                    var rawAction = obj.Value<string>("action");
                    // var target = obj.Value<string>("target");

                    try {
                        var key = PackageKey.From(rawKey);
                        InstallAction action = InstallAction.Uninstall;
                        if (rawAction == "install") {
                            action = InstallAction.Install;
                        }
                        
                        actions.Add(new PackageAction(key, action));
                    } catch {
                        continue;
                    }
                }

                var primaryButton = string.Format(Strings.InstallUninstallNPackages, actions.Count);
                
                // Resolve the names for the package keys
                var strings = actions.Map(x => {
                    var package = Repo.Packages.Packages[x.PackageKey.Id];
                    var release = Repo.Release(x.PackageKey);
                    if (release == null || package == null) {
                        return null;
                    }
                    return $"{x.Action.NativeName()}: {package.NativeName()} {release.Version}";
                });

                return await await PahkatApp.Current.Dispatcher.InvokeAsync(async () => {
                    var dialog = new ConfirmationDialog(
                        "Confirm Selection", 
                        "Do you wish to do the following actions:",
                        string.Join("\n", strings),
                        primaryButton);

                    try {
                        WebView.Visibility = Visibility.Hidden;
                        var result = await dialog.ShowAsync();
                        WebView.Visibility = Visibility.Visible;

                        if (result == ContentDialogResult.Primary) {
                            var app = (PahkatApp) Application.Current;
                            await app.StartTransaction(actions.ToArray());
                            return true;
                        }
                    }
                    catch (Exception e) {
                        Log.Debug(e, "wat");
                    }
                    return false;
                });
            }

            private async Task<object> Packages(JArray args) {
                if (args.Count > 0) {
                    PackageQuery? query = null;
                    
                    try {
                        query = JsonConvert.DeserializeObject<PackageQuery>(args[0].ToString(),
                            Json.Settings.Value);
                    }
                    catch {
                        // ignored
                    }

                    if (query.HasValue) {
                        try
                        {
                            var app = PahkatApp.Current;
                            var s = await app.PackageStore.ResolvePackageQuery(query.Value);
                            return JObject.Parse(s);
                        }
                        catch {
                            return JObject.Parse("[]");
                        }
                    }
                    
                }
                
                var map = new Dictionary<PackageKey, IDescriptor>();
                
                foreach (var keyValuePair in Repo.Packages.Packages) {
                    var value = keyValuePair.Value!;
                    if (value != null) {
                        var key = Repo.PackageKey(value);
                        map.Add(key, value);
                    }
                    
                }

                return map;
            }
        }
    }
}