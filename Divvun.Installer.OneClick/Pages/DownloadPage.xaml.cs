using Divvun.Installer.OneClick.Models;
using Iterable;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
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
using Serilog;

namespace Divvun.Installer.OneClick {

/// <summary>
/// Interaction logic for DownloadPage.xaml
/// </summary>
public partial class DownloadPage : Page {
    private CancellationTokenSource cancellationToken = new CancellationTokenSource();
    private CancellationTokenSource? pahkatToken;
    private Dictionary<PackageKey, string>? installedPackages;

    public DownloadPage() {
        InitializeComponent();
        Application.Current.Dispatcher.InvokeAsync(RunInstallProcess);
    }

    private async Task RunInstallProcess() {
        Log.Debug("Running install process");
        try {
            using var client = new WebClient();
            client.Headers.Add("User-Agent", "divvun-oneclick-installer/" + ThisAssembly.AssemblyInformationalVersion);

            var selectedLanguage = ((App)Application.Current).SelectedLanguage!;
            var meta = ((App)Application.Current).Meta!;

            UpdateDownloadTitle(Strings.DivvunDownloadPrimary, Strings.DivvunDownloadSecondary);
            await InstallDivvunInstaller(meta, client);

            PahkatClient pahkat = new PahkatClient();
            var packageKeys = await ResolvePackageActions(pahkat, selectedLanguage);

            installedPackages = packageKeys.Map(tup => (tup.Item1, GetNativeResourceName(tup.Item2))).ToDict();
            UpdateDownloadTitle(string.Format(Strings.InstallingResources, selectedLanguage.Name),
                string.Join(", ", installedPackages.Values));
            await Task.WhenAll(InstallPackageKeys(pahkat, packageKeys.Map(tup => tup.Item1)),
                Task.Delay(TimeSpan.FromSeconds(2), cancellationToken.Token));

            SetProgress(Strings.Finalizing, 0, 0, true);
            await EnableKeyboards(meta, selectedLanguage.Tag);

            var state = new TransactionState.InProgress {
                Actions = null,
                IsRebootRequired = false,
                State = new TransactionState.InProgress.TransactionProcessState.CompleteState(),
            };
            var app = (App)Application.Current;
            app.CurrentTransaction.OnNext(state);
        }
        catch (TaskCanceledException) {
            var app = (App)Application.Current;
            app.CurrentTransaction.OnNext(new TransactionState.Cancel());
        }
        catch (Exception e) {
            Log.Error(e, "Error in RunInstallProcess");
            ((App)Application.Current).TerminateWithError(e);
        }
    }

    private void SetProgress(string message, long current, long total, bool indeterminite = false) {
        DownloadProgresBar.IsIndeterminate = indeterminite;
        DownloadProgresBar.Maximum = total;
        DownloadProgresBar.Value = current;

        ProgressText.Text = message;
    }

    private void UpdateDownloadTitle(string primaryText, string secondaryText) {
        DownloadTitleText.Text = primaryText;
        DownloadSubtitleText.Text = secondaryText;
    }

    private async Task EnableKeyboards(OneClickMeta meta, string tag) {
        Log.Debug("Enabling keyboards");
        
        var lang = meta.Languages.Find(lang => string.Equals(lang.Tag, tag));
        if (lang == null) {
            throw new Exception("No matching language found in meta.");
        }

        string kbdiFile;
        if (Environment.Is64BitOperatingSystem) {
            kbdiFile = System.IO.Path.Join(Directory.GetCurrentDirectory(), "kbdi-x64.exe");
        }
        else {
            kbdiFile = System.IO.Path.Join(Directory.GetCurrentDirectory(), "kbdi.exe");
        }

        var regionCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;

        var regionLayouts = lang.Layouts.Filter(layout => {
            var parts = layout.Name.Split("-");

            if (parts.Length != 3) {
                return false;
            }

            var layoutRegion = parts[2];

            return layoutRegion.Equals(regionCode);
        }).ToList();

        if (regionLayouts.Count > 0) {
            foreach (var layout in regionLayouts) {
                await App.RunProcess(kbdiFile, $"keyboard_enable -g \"{{{layout.Uuid}}}\" -t {layout.Name}",
                    cancellationToken.Token);
            }
        }
        else {
            foreach (var layout in lang.Layouts) {
                await App.RunProcess(kbdiFile, $"keyboard_enable -g \"{{{layout.Uuid}}}\" -t {layout.Name}",
                    cancellationToken.Token);
            }
        }
    }

    private Task InstallPackageKeys(PahkatClient pahkat, IEnumerable<PackageKey> packageKeys) {
        var source = new TaskCompletionSource<int>();

        var actions = packageKeys
            .Map(x => new PackageAction(x, InstallAction.Install, InstallTarget.System))
            .ToArray();

        Task.Run(async () => {
            try {
                Log.Debug("Starting install process");
                pahkatToken = await pahkat.ProcessTransaction(actions, (message) => {
                    if (cancellationToken.IsCancellationRequested) {
                        source.SetCanceled();
                    }

                    var app = (App)Application.Current;
                    var newState = app.CurrentTransaction.Value.Reduce(message);
                    app.CurrentTransaction.OnNext(newState);
                    Log.Debug($"{message}");
                    if (message.IsErrorState) {
                        Log.Debug("Ending install process with error");
                        source.SetException(new Exception(message.AsTransactionError?.Error ??
                            $"An unknown error occurred while installing package with key: {message.AsTransactionError?.PackageKey ?? "<no key>"}"));
                    }

                    if (message.IsCompletionState) {
                        Log.Debug("Ending install process");
                        source.SetResult(0);
                    }
                });
            }
            catch (TaskCanceledException) {
                source.SetCanceled();
            }
        });

        return source.Task;
    }

    private Task<List<(PackageKey, Dictionary<string, string>)>> ResolvePackageActions(PahkatClient pahkat,
        LanguageItem selectedLanguage) {
        return Task.Run(async () => {
            var repos = await pahkat.GetRepoRecords();
            var mainRepo = repos.Filter((repo) =>
            {
                return repo.Key.AbsoluteUri == "https://pahkat.uit.no/main/";
            });

            if (mainRepo == null)
            {
                await pahkat.SetRepo(new Uri("https://pahkat.uit.no/main/"), new RepoRecord());
            }

            var result = await pahkat.ResolvePackageQuery(new PackageQuery() {
                Tags = new[] { $"lang:{selectedLanguage.Tag}" },
            });
            Log.Debug(result);

            var obj = JObject.Parse(result);
            var descriptors = obj["descriptors"]?.ToObject<List<JObject>>() ?? new List<JObject>();
            var packageKeys = descriptors
                .FilterMap((o) => {
                    var key = o["key"]?.ToObject<string>();
                    var name = o["name"]?.ToObject<Dictionary<string, string>>();

                    if (key == null || name == null) {
                        return null;
                    }

                    ValueTuple<string, Dictionary<string, string>>? tup = ValueTuple.Create(key, name);

                    return tup;
                })
                .Map(tup => (PackageKey.From(tup.Item1), tup.Item2))
                .ToList();
            return packageKeys;
        });
    }

    private string GetNativeResourceName(Dictionary<string, string> resource) {
        var tag = CultureInfo.CurrentCulture.IetfLanguageTag;

        if (resource.TryGetValue(tag, out var name)) {
            return name;
        }

        if (resource.TryGetValue("en", out name)) {
            return name;
        }

        return string.Empty;
    }

    private async Task InstallDivvunInstaller(OneClickMeta meta, WebClient client) {
        Log.Debug("Installing Divvun Manager");
        SetProgress(Strings.DownloadingDivvunInstaller, 0, 1);
        client.DownloadProgressChanged += Client_DownloadProgressChanged;
        var tmpFile = System.IO.Path.GetTempFileName();
        await client.DownloadFileTaskAsync(meta.InstallerUrl, tmpFile);

        client.DownloadProgressChanged -= Client_DownloadProgressChanged;
        SetProgress(Strings.PreparingInstaller, 0, 0, true);
        Log.Debug($"Downloaded to {tmpFile}");

        var exeFile =
            $"{System.IO.Path.GetDirectoryName(tmpFile)}\\{System.IO.Path.GetFileNameWithoutExtension(tmpFile)}.exe";

        File.Move(tmpFile, exeFile);
        Log.Debug($"Renamed to executable: {exeFile}");

        Log.Debug("Running installer silently");
        await App.RunProcess(exeFile, "/VERYSILENT", cancellationToken.Token);
        Log.Debug("Done installing.");
    }

    private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
        if (cancellationToken.IsCancellationRequested) {
            (sender as WebClient)?.CancelAsync();
        }

        SetProgress(Strings.DownloadingDivvunInstaller, e.BytesReceived, e.TotalBytesToReceive);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) {
        var app = (App)Application.Current;

        app.CurrentTransaction.AsObservable()
            // Resolve down the events to Download-related ones only
            .Where(x => x.IsInProgressDownloading)
            .Select(x => x.AsInProgress!.State.AsDownloadState!.Progress)
            .ObserveOn(app.Dispatcher)
            .SubscribeOn(app.Dispatcher)
            .Subscribe(state => {
                foreach (var keyValuePair in state) {
                    SetProgress(installedPackages![keyValuePair.Key],
                        keyValuePair.Value.Item1,
                        keyValuePair.Value.Item2);
                }
            });

        app.CurrentTransaction.AsObservable()
            // Resolve down the events to Install-related ones only
            .Where(x => x.IsInProgressInstalling)
            .Select(x => x.AsInProgress!.State.AsInstallState!.CurrentItem)
            .ObserveOn(app.Dispatcher)
            .SubscribeOn(app.Dispatcher)
            .Subscribe(key => {
                SetProgress(installedPackages![key],
                    0,
                    0, true);
            });
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) {
        BtnCancel.IsEnabled = false;
        cancellationToken.Cancel();

        pahkatToken?.Cancel();
    }
}

}
