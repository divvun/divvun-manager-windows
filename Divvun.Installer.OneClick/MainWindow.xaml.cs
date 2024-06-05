using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
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
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Iterable;
using Newtonsoft.Json.Linq;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Flurl;
using Serilog;
using Sentry;
using System.Threading;
using System.Reactive.Linq;

namespace Divvun.Installer.OneClick {

internal static class Util {
    public class Iso639Data {
        public string? Tag1;
        public string? Tag3;
        public string? Name;
        public string? Autonym;
        public string? Source;
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
                var csv = new CsvHelper.CsvReader(reader);
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.RegisterClassMap<Iso639DataMap>();
                _data = csv.GetRecords<Iso639Data>().ToArray();
            }

            return _data.First(x => x.Tag1 == tag) ?? _data.First(y => y.Tag3 == tag);
        }
    }

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

        if (langCode == string.Empty) {
            langCode = tag;
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
}

public class OneClickMeta {
    public string InstallerUrl { get; set; }
    public List<OneClickLanguageMeta> Languages { get; set; }
}

public class OneClickLanguageMeta {
    public string Tag { get; set; }
    public List<OneClickLayoutMeta> Layouts { get; set; }
}

public class OneClickLayoutMeta {
    public string Uuid { get; set; }
    public string Name { get; set; }
}

public class LanguageItem : IComparable<LanguageItem> {
    public string Name { get; set; }
    public string Tag { get; set; }

    public int CompareTo(LanguageItem other) {
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}

public enum Route {
    Landing,
    Download,
    Finalizing,
    Completion,
    Cancel,
    Error,
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private CancellationTokenSource? installCancellationToken = null;
    private Page? _currentPage = null;

    public MainWindow() {
        InitializeComponent();
    }

    public void ShowPage(Page pageView) {
        Application.Current.Dispatcher.Invoke(() => {
            ShowContent();
            FrmContainer.Navigate(pageView);
            _currentPage = pageView;

            JournalEntry page;
            while ((page = FrmContainer.RemoveBackEntry()) != null) {
                // page.
                Log.Verbose("Murdered a view. {page}", page);
                // Clean up everything
            }
        });
    }

    public void ShowContent() {
        FrmContainer.Visibility = Visibility.Visible;
    }

    private static IObservable<Route> MakeRouter() {
        var app = (App)Application.Current;
        return app.CurrentTransaction.AsObservable()
            .DistinctUntilChanged()
            .ObserveOn(app.Dispatcher)
            .SubscribeOn(app.Dispatcher)
            .Select(evt => evt.Match(
                notStarted => Route.Landing,
                inProgress => inProgress.State.Match(
                    start => Route.Download,
                    downloading => Route.Download,
                    installing => Route.Download,
                    finalizing => Route.Finalizing,
                    complete => Route.Completion),
                error => Route.Error,
                cancel => Route.Cancel))
            .DistinctUntilChanged();
    }

    public IObservable<Route> Router = MakeRouter();

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs args) {
        Router
            .Subscribe(route => {
                switch (route) {
                case Route.Landing:
                    ShowPage(new LandingPage());
                    return;
                case Route.Download:
                    ShowPage(new DownloadPage());
                    return;
                case Route.Completion:
                    ShowPage(new CompletionPage());
                    return;
                case Route.Cancel:
                    ShowPage(new CancelPage());
                    return;
                }
            });
    }

    private void MainWindow_OnClosing(object sender, CancelEventArgs e) {
        if (_currentPage is DownloadPage) {
            e.Cancel = true;
            ((Window)sender).WindowState = WindowState.Minimized;
        }
    }
}

// Code from https://ithoughthecamewithyou.com/post/reboot-computer-in-c-net
// License: public domain
public static class ShutdownExtensions {
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
        public uint LowPart;
        public int HighPart;
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

    private const uint TOKEN_QUERY = 0x0008;
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

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
}

}