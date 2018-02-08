using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Pahkat.Extensions
{
    public static class Extensions
    {
        public static IObservable<T> NotNull<T>(this IObservable<T> thing)
        {
            return thing.Where(x => x != null);
        }

        public static IObservable<T> DoIfNull<T>(this IObservable<T> thing, Action thingDoer)
        {
            return thing.Do(x =>
            {
                if (x == null)
                {
                    thingDoer();
                }
            });
        }

        public static IObservable<EventPattern<RoutedEventArgs>>
            ReactiveClick(this Button btn)
        {
            return Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                h => btn.Click += h,
                h => btn.Click -= h);
        }

        public static IObservable<EventPattern<TextChangedEventArgs>>
            ReactiveTextChanged(this TextBox txt)
        {
            return Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
                h => txt.TextChanged += h,
                h => txt.TextChanged -= h);
        }

        public static IObservable<EventPattern<KeyEventArgs>>
            ReactiveKeyDown(this UIElement element)
        {
            return Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(
                h => element.KeyDown += h,
                h => element.KeyDown -= h);
        }

        public static IObservable<EventPattern<MouseButtonEventArgs>>
            ReactiveDoubleClick(this ItemsControl control)
        {
            return Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                h => control.MouseDoubleClick += h,
                h => control.MouseDoubleClick -= h);
        }

        public static IObservable<EventPattern<DownloadProgressChangedEventArgs>>
            ReactiveDownloadProgressChange(this WebClient client)
        {
            return Observable.FromEventPattern<DownloadProgressChangedEventHandler, DownloadProgressChangedEventArgs>(
                h => client.DownloadProgressChanged += h,
                h => client.DownloadProgressChanged -= h);
        }

        public static IObservable<EventPattern<DownloadDataCompletedEventArgs>>
            ReactiveDownloadDataCompleted(this WebClient client)
        {
            return Observable.FromEventPattern<DownloadDataCompletedEventHandler, DownloadDataCompletedEventArgs>(
                h => client.DownloadDataCompleted += h,
                h => client.DownloadDataCompleted -= h);
        }

        public static IObservable<EventPattern<EventArrivedEventArgs>>
            ReactiveEventArrived(this ManagementEventWatcher watcher)
        {
            return Observable.FromEventPattern<EventArrivedEventHandler, EventArrivedEventArgs>(
                x => watcher.EventArrived += x,
                x => watcher.EventArrived -= x);
        }

        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = new TValue();
            }

            return dict[key];
        }

        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue fallback)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }

            return fallback;
        }

        public static void ReplacePageWith(this Page page, object destination)
        {
            void Handler()
            {
                var n = page.NavigationService;
                n.Navigate(destination);
                n.RemoveBackEntry();
            }

            if (page.IsLoaded)
            {
                Handler();
            }
            else
            {
                page.Loaded += (sender, args) => Handler();
            }
        }

        public static string GetGuid(this Assembly assembly)
        {
            var guidAttr = (GuidAttribute) assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
            return guidAttr.Value;
        }

        public static void SafeStart(this ManagementEventWatcher watcher)
        {
            try
            {
                watcher.Start();
            }
            catch (COMException ex)
            {
                switch ((uint) ex.HResult)
                {
                    case 0x80042001: // WBEMESS_E_REGISTRATION_TOO_BROAD
                        throw new ManagementException("Provider registration overlaps with the system event domain.",
                            ex);
                }

                throw;
            }
        }
        
        public static void DisableCloseButton(this Window window)
        {
            var windowHandle = new WindowInteropHelper(window).Handle;
            
            var menuHandle = Native.GetSystemMenu(windowHandle, false);
            if (menuHandle != IntPtr.Zero) {
                Native.EnableMenuItem(menuHandle, Native.SC_CLOSE, Native.MF_GRAYED);
                Native.DestroyMenu(menuHandle);
            }
        }
  
        public static void EnableCloseButton(this Window window) {
            var windowHandle = new WindowInteropHelper(window).Handle;
            
            var menuHandle = Native.GetSystemMenu(windowHandle, false);
            if (menuHandle != IntPtr.Zero)
            {
                Native.EnableMenuItem(menuHandle, Native.SC_CLOSE, 0);
                Native.DestroyMenu(menuHandle);
            }
        }  
        
        public static T[] EmptyArray<T>()
        {
            return InternalEmptyArray<T>.Value;
        }
        
        internal static class InternalEmptyArray<T>
        {
            public static readonly T[] Value = new T[0];
        }
    }
    
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeSeconds(this DateTimeOffset d)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long) (d.ToUniversalTime() - epoch).TotalSeconds;
        }

        public static DateTimeOffset FromUnixTimeSeconds(long seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.Add(timeSpan).ToLocalTime();
        }
    }

    public static class Native
    {
        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.LPWStr)] string commandLineArgs, int Flags);
        [DllImport("winlangdb.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetLanguageNames(string Language, StringBuilder Autonym, StringBuilder EnglishName, StringBuilder LocalName, StringBuilder ScriptName);
        [DllImport("user32.dll")]
        internal static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        internal static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        [DllImport("user32.dll")]
        internal static extern IntPtr DestroyMenu(IntPtr hWnd);
  
        internal const uint MF_GRAYED = 0x00000001;
        internal const uint SC_CLOSE = 0xF060;
    }
    
    // Code from https://ithoughthecamewithyou.com/post/reboot-computer-in-c-net
    // License: public domain
    public static class ShutdownExtensions
    {
        public static void Reboot()
        {
            IntPtr tokenHandle = IntPtr.Zero;
    
            try
            {
                // get process token
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle,
                    TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES,
                    out tokenHandle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to open process token handle");
                }
    
                // lookup the shutdown privilege
                TOKEN_PRIVILEGES tokenPrivs = new TOKEN_PRIVILEGES();
                tokenPrivs.PrivilegeCount = 1;
                tokenPrivs.Privileges = new LUID_AND_ATTRIBUTES[1];
                tokenPrivs.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
    
                if (!LookupPrivilegeValue(null,
                    SE_SHUTDOWN_NAME,
                    out tokenPrivs.Privileges[0].Luid))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to open lookup shutdown privilege");
                }
    
                // add the shutdown privilege to the process token
                if (!AdjustTokenPrivileges(tokenHandle,
                    false,
                    ref tokenPrivs,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to adjust process token privileges");
                }
    
                // reboot
                if (!ExitWindowsEx(ExitWindows.Reboot,
                        ShutdownReason.MajorApplication | 
                ShutdownReason.MinorInstallation | 
                ShutdownReason.FlagPlanned))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to reboot system");
                }
            }
            finally
            {
                // close the process token
                if (tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(tokenHandle);
                }
            }
        }
    
        // everything from here on is from pinvoke.net
    
        [Flags]
        private enum ExitWindows : uint
        {
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
        private enum ShutdownReason : uint
        {
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
            FlagPlanned = 0x80000000
        }
    
        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }
    
        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public UInt32 Attributes;
        }
    
        private struct TOKEN_PRIVILEGES
        {
            public UInt32 PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }
    
        private const UInt32 TOKEN_QUERY = 0x0008;
        private const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
    
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ExitWindowsEx(ExitWindows uFlags, 
            ShutdownReason dwReason);
    
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, 
            UInt32 DesiredAccess, 
            out IntPtr TokenHandle);
    
        [DllImport("advapi32.dll", SetLastError = true, CharSet=CharSet.Unicode)]
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
            [MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            UInt32 Zero,
            IntPtr Null1,
            IntPtr Null2);
    }

    // From https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp
    // License: CC BY-SA 3.0
    public static class CommandLineParsingExtensions {
        public static IEnumerable<string> ParseCommandLine(this string commandLine)
        {
            var inQuotes = false;

            return commandLine.Split(c =>
                {
                    if (c == '\"')
                    {
                        inQuotes = !inQuotes;
                    }

                    return !inQuotes && c == ' ';
                })
                .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
                .Where(arg => !string.IsNullOrEmpty(arg));
        }

        public static Tuple<string, string[]> ParseFileNameAndArgs(this string commandLine)
        {
            var items = commandLine.ParseCommandLine().ToArray();
            var fileName = items.Length == 0 ? null : items[0];
            var args = items.Length < 2 ? null : items.Skip(1).ToArray();
            return new Tuple<string, string[]>(fileName, args);
        }
        
        public static IEnumerable<string> Split(this string str, 
            Func<char, bool> controller)
        {
            var nextPiece = 0;

            for (var c = 0; c < str.Length; c++)
            {
                if (!controller(str[c]))
                {
                    continue;
                }
                yield return str.Substring(nextPiece, c - nextPiece);
                nextPiece = c + 1;
            }

            yield return str.Substring(nextPiece);
        }
        
        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if ((input.Length >= 2) && 
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }
    }
}
