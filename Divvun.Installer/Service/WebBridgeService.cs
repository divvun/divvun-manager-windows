using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Divvun.Installer.UI.Main;
using Divvun.Installer.UI.Main.Dialog;
using Microsoft.Toolkit.Wpf.UI.Controls;
using ModernWpf.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Pahkat.Sdk.Rpc.Fbs;

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
            public LoadedRepository Repo;
            public WebView WebView;
            
            public Functions(LoadedRepository repo, WebView webView) {
                Repo = repo;
                WebView = webView;
            }

            public async Task<object> Process(WebBridgeRequest request) {
                switch (request.Method)
                {
                case "env":
                    return Env(request.Args);
                case "string":
                    return String(request.Args);
                case "packages":
                    return Packages(request.Args);
                case "status":
                    return Status(request.Args);
                case "transaction":
                    return await Transaction(request.Args);
                }
                
                throw new WebBridgeException($"Unhandled method: {request.Method}");
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

            private object Status(JArray args) {
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

                using (var guard = app.PackageStore.Lock()) {
                    foreach (var packageKey in keys) {
                        var status = guard.Value.Status(packageKey);
                        var obj = new JObject();
                        obj["status"] = (int) status;
                        obj["target"] = "system";
                        map[packageKey.ToString()] = obj;
                    }
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
                
                var dialog = new ConfirmationDialog(
                    "Confirm Selection", 
                    "Do you wish to do the following actions:",
                    string.Join("\n", actions.Select(x => x.PackageKey.ToString())),
                    primaryButton);

                try {
                    WebView.Visibility = Visibility.Hidden;
                    var result = await dialog.ShowAsync();
                    WebView.Visibility = Visibility.Visible;

                    if (result == ContentDialogResult.Primary) {
                        var app = (PahkatApp) Application.Current;
                        app.StartTransaction(actions.ToArray());
                        return true;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(("wat"));
                }
                return false;
            }

            private object Packages(JArray args) {
                if (args.Count > 0) {
                    PackageQuery? query = null;
                    
                    try {
                        query = JsonConvert.DeserializeObject<PackageQuery>(args[0].ToString(),
                            Json.Settings.Value);
                    }
                    catch { }

                    if (query.HasValue) {
                        try {
                            var app = (PahkatApp) Application.Current;
                            using var guard = app.PackageStore.Lock();
                            var s = guard.Value.ResolvePackageQuery(query.Value);
                            return JObject.Parse(s);
                        }
                        catch {
                            return JObject.Parse("[]");
                        }
                    }
                    
                }
                var map = new Dictionary<PackageKey, Descriptor>();
                
                foreach (var keyValuePair in Repo.Packages.Packages()) {
                    var value = keyValuePair.Value!;
                    if (value.HasValue) {
                        var key = Repo.PackageKey(value.Value);
                        map.Add(key, value.Value);
                    }
                    
                }

                return map;
            }
        }
    }
}