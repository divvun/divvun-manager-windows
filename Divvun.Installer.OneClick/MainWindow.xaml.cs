using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CsvHelper;
using CsvHelper.Configuration;
using Iterable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Sentry;
using Serilog;

namespace Divvun.Installer.OneClick {

internal static class Util {
    public static string GetCultureDisplayName(string tag) {
        if (tag == "zxx" || tag == "") {
            return "---";
        }

        string langCode;
        CultureInfo? culture = null;
        try {
            culture = new CultureInfo(tag);
            langCode = culture.ThreeLetterISOLanguageName;
        }
        catch (Exception) {
            // Best attempt
            langCode = tag.Split('_', '-')[0];
        }

        var data = Iso639.GetTag(langCode);
        if (data?.Autonym != null && data.Autonym != "") {
            return data.Autonym;
        }

        if (culture != null && culture.DisplayName != "" && culture.DisplayName != culture.EnglishName) {
            return culture.DisplayName;
        }

        if (data?.Name != null && data.Name != "") {
            return data.Name;
        }

        return tag;
    }

    public class Iso639Data {
        public string? Autonym;
        public string? Name;
        public string? Source;
        public string? Tag1;
        public string? Tag3;
    }

    public sealed class Iso639DataMap : ClassMap<Iso639Data> {
        public Iso639DataMap() {
            Map(m => m.Tag1).Name("tag1");
            Map(m => m.Tag3).Name("tag3");
            Map(m => m.Name).Name("name");
            Map(m => m.Autonym).Name("autonym");
            Map(m => m.Source).Name("source");
        }
    }

    public static class Iso639 {
        private static Iso639Data[] _data;

        public static Iso639Data? GetTag(string tag) {
            if (_data == null) {
                var uri = new Uri("pack://application:,,,/Resources/iso639-autonyms.tsv");
                var reader = new StreamReader(Application.GetResourceStream(uri)?.Stream ??
                    throw new NullReferenceException());
                var csv = new CsvReader(reader);
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.RegisterClassMap<Iso639DataMap>();
                _data = csv.GetRecords<Iso639Data>().ToArray();
            }

            return _data.First(x => x.Tag1 == tag) ?? _data.First(y => y.Tag3 == tag);
        }
    }
}

internal class OneClickMeta {
    public string InstallerUrl { get; set; }
    public List<string> LanguageTags { get; set; }
}

internal class LanguageItem : IComparable<LanguageItem> {
    public string Name { get; set; }
    public string Tag { get; set; }

    public int CompareTo(LanguageItem other) {
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private readonly ObservableCollection<LanguageItem> _dropDownData = new ObservableCollection<LanguageItem>();
    private OneClickMeta? _meta;

    public MainWindow() {
        InitializeComponent();

        Languages.ItemsSource = _dropDownData;
    }

    private async Task<OneClickMeta?> DownloadOneClickMetadata() {
        using var client = new WebClient();
        var jsonPayload = await client.DownloadStringTaskAsync(new Uri("https://pahkat.uit.no/main/oneclick.json"));
        return JsonConvert.DeserializeObject<OneClickMeta>(jsonPayload);
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs args) {
        try {
            _meta = await DownloadOneClickMetadata();
        }
        catch (Exception e) {
            TerminateWithError(e);
            return;
        }

        if (_meta == null) {
            TerminateWithError(new NullReferenceException("_meta"));
            return;
        }

        var items = _meta.LanguageTags.Map(tag => new LanguageItem {
            Tag = tag,
            Name = Util.GetCultureDisplayName(tag),
        }).ToList();
        items.Sort();

        foreach (var item in items) {
            _dropDownData.Add(item);
        }

        PageLoading.Visibility = Visibility.Hidden;
        PageHome.Visibility = Visibility.Visible;
    }

    private void UpdateDownloadProgress(string message) {
        Console.WriteLine(message);
        ProgressText.Text = message;
    }

    private Task<int> RunProcess(string filePath, string args) {
        var source = new TaskCompletionSource<int>();

        var process = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = filePath,
                Arguments = args,
            },
            EnableRaisingEvents = true,
        };

        process.Exited += (sender, args) => {
            source.SetResult(process.ExitCode);
            process.Dispose();
        };

        process.Start();

        return source.Task;
    }

    private Task<List<PackageKey>> ResolvePackageActions(PahkatClient pahkat, LanguageItem selectedLanguage) {
        return Task.Run(async () => {
            await pahkat.SetRepo(new Uri("https://pahkat.uit.no/main/"), new RepoRecord());
            var result = await pahkat.ResolvePackageQuery(new PackageQuery {
                Tags = new[] { $"lang:{selectedLanguage.Tag}" },
            });
            Console.WriteLine(result);

            var obj = JObject.Parse(result);
            var descriptors = obj["descriptors"]?.ToObject<List<JObject>>() ?? new List<JObject>();
            var packageKeys = descriptors
                .FilterMap(o => o["key"]?.ToObject<string>())
                .Map(PackageKey.From)
                .ToList();
            return packageKeys;
        });
    }

    private Task InstallPackageKeys(PahkatClient pahkat, List<PackageKey> packageKeys) {
        var source = new TaskCompletionSource<int>();

        var actions = packageKeys
            .Map(x => new PackageAction(x, InstallAction.Install))
            .ToArray();

        Task.Run(async () => {
            Console.WriteLine("Starting install process");
            await pahkat.ProcessTransaction(actions, message => {
                Console.WriteLine(message);
                if (message.IsErrorState) {
                    Console.WriteLine("Ending install process with error");
                    source.SetException(new Exception(message.AsTransactionError?.Error ??
                        $"An unknown error occurred while installing package with key: {message.AsTransactionError?.PackageKey ?? "<no key>"}"));
                }

                if (message.IsCompletionState) {
                    Console.WriteLine("Ending install process");
                    source.SetResult(0);
                }
            });
        });

        return source.Task;
    }

    private async Task<int> InstallDivvunInstaller(OneClickMeta meta, WebClient client) {
        var tmpFile = Path.GetTempFileName();
        await client.DownloadFileTaskAsync(meta.InstallerUrl, tmpFile);

        Console.WriteLine($"Downloaded to {tmpFile}");
        var exeFile = $"{Path.GetDirectoryName(tmpFile)}\\{Path.GetFileNameWithoutExtension(tmpFile)}.exe";

        File.Move(tmpFile, exeFile);
        Console.WriteLine($"Renamed to executable: {exeFile}");

        return await RunProcess(exeFile, "/VERYSILENT");
    }

    private async Task RunInstallProcess() {
        using var client = new WebClient();

        var meta = _meta;
        if (meta == null) {
            throw new Exception("The metadata necessary to download language files was not found.");
        }

        var selectedLanguage = Languages.SelectedItem as LanguageItem;
        if (selectedLanguage == null) {
            throw new Exception("No language was selected for installation.");
        }

        UpdateDownloadProgress("Preparing Divvun Installer...");
        await InstallDivvunInstaller(meta, client);

        UpdateDownloadProgress($"Downloading {selectedLanguage.Name} resources...");
        PahkatClient pahkat = new PahkatClient();
        var packageKeys = await ResolvePackageActions(pahkat, selectedLanguage);

        UpdateDownloadProgress($"Installing {selectedLanguage.Name} resources...");
        await InstallPackageKeys(pahkat, packageKeys);

        UpdateDownloadProgress("Finishing up...");
    }

    private void TerminateWithError(Exception e) {
        if (Debugger.IsAttached) {
            throw e;
        }

        var msg =
            $"An error has occurred:\n\n{e.Message}\n\nThis crash has been automatically reported to Divvun. You may try to re-run the installer, but if problems persist, please email us at feedback@divvun.no for assistance.";

        Log.Fatal(e, "Fatal error while running application");
        SentrySdk.CaptureException(e);

        MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Application.Current.Shutdown(1);
    }

    private async void InstallButton_OnClick(object sender, RoutedEventArgs args) {
        PageHome.Visibility = Visibility.Hidden;
        PageDownload.Visibility = Visibility.Visible;

        try {
            await RunInstallProcess();
        }
        catch (Exception e) {
            TerminateWithError(e);
        }

        PageDownload.Visibility = Visibility.Hidden;
        PageCompleted.Visibility = Visibility.Visible;
    }

    private void Languages_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
        Console.WriteLine($"Changed language selection: {Languages.SelectedValue}");
        InstallButton.Visibility = Visibility.Visible;
    }

    private void MainWindow_OnClosing(object sender, CancelEventArgs e) {
        if (PageDownload.Visibility == Visibility.Visible) {
            e.Cancel = true;
            ((Window)sender).WindowState = WindowState.Minimized;
        }
    }

    private void RebootButton_OnClick(object sender, RoutedEventArgs e) {
        ShutdownExtensions.Reboot();
    }

    private void RebootLaterButton_OnClick(object sender, RoutedEventArgs e) {
        Close();
    }
}

// Code from https://ithoughthecamewithyou.com/post/reboot-computer-in-c-net
// License: public domain
public static class ShutdownExtensions {
    private const uint TOKEN_QUERY = 0x0008;
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

    public static void Reboot() {
        var tokenHandle = IntPtr.Zero;

        try {
            // get process token
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle,
                TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES,
                out tokenHandle)) {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Failed to open process token handle");
            }

            // lookup the shutdown privilege
            var tokenPrivs = new TOKEN_PRIVILEGES();
            tokenPrivs.PrivilegeCount = 1;
            tokenPrivs.Privileges = new LUID_AND_ATTRIBUTES[1];
            tokenPrivs.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

            if (!LookupPrivilegeValue(null,
                SE_SHUTDOWN_NAME,
                out tokenPrivs.Privileges[0].Luid)) {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Failed to open lookup shutdown privilege");
            }

            // add the shutdown privilege to the process token
            if (!AdjustTokenPrivileges(tokenHandle,
                false,
                ref tokenPrivs,
                0,
                IntPtr.Zero,
                IntPtr.Zero)) {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Failed to adjust process token privileges");
            }

            // reboot
            if (!ExitWindowsEx(ExitWindows.Reboot,
                ShutdownReason.MajorApplication |
                ShutdownReason.MinorInstallation |
                ShutdownReason.FlagPlanned)) {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Failed to reboot system");
            }
        }
        finally {
            // close the process token
            if (tokenHandle != IntPtr.Zero) {
                CloseHandle(tokenHandle);
            }
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ExitWindowsEx(ExitWindows uFlags,
        ShutdownReason dwReason);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle,
        uint DesiredAccess,
        out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(string lpSystemName,
        string lpName,
        out LUID lpLuid);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState,
        uint Zero,
        IntPtr Null1,
        IntPtr Null2);

    // everything from here on is from pinvoke.net

    [Flags]
    private enum ExitWindows : uint {
        // ONE of the following five:
        LogOff = 0x00,
        ShutDown = 0x01,
        Reboot = 0x02,
        PowerOff = 0x08,
        RestartApps = 0x40,

        // plus AT MOST ONE of the following two:
        Force = 0x04,
        ForceIfHung = 0x10,
    }

    [Flags]
    private enum ShutdownReason : uint {
        MajorApplication = 0x00040000,
        MajorHardware = 0x00010000,
        MajorLegacyApi = 0x00070000,
        MajorOperatingSystem = 0x00020000,
        MajorOther = 0x00000000,
        MajorPower = 0x00060000,
        MajorSoftware = 0x00030000,
        MajorSystem = 0x00050000,

        MinorBlueScreen = 0x0000000F,
        MinorCordUnplugged = 0x0000000b,
        MinorDisk = 0x00000007,
        MinorEnvironment = 0x0000000c,
        MinorHardwareDriver = 0x0000000d,
        MinorHotfix = 0x00000011,
        MinorHung = 0x00000005,
        MinorInstallation = 0x00000002,
        MinorMaintenance = 0x00000001,
        MinorMMC = 0x00000019,
        MinorNetworkConnectivity = 0x00000014,
        MinorNetworkCard = 0x00000009,
        MinorOther = 0x00000000,
        MinorOtherDriver = 0x0000000e,
        MinorPowerSupply = 0x0000000a,
        MinorProcessor = 0x00000008,
        MinorReconfig = 0x00000004,
        MinorSecurity = 0x00000013,
        MinorSecurityFix = 0x00000012,
        MinorSecurityFixUninstall = 0x00000018,
        MinorServicePack = 0x00000010,
        MinorServicePackUninstall = 0x00000016,
        MinorTermSrv = 0x00000020,
        MinorUnstable = 0x00000006,
        MinorUpgrade = 0x00000003,
        MinorWMI = 0x00000015,

        FlagUserDefined = 0x40000000,
        FlagPlanned = 0x80000000,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID {
        public readonly uint LowPart;
        public readonly int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES {
        public LUID Luid;
        public uint Attributes;
    }

    private struct TOKEN_PRIVILEGES {
        public uint PrivilegeCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }
}

}