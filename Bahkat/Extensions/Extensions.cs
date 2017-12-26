using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Bahkat.Extensions
{
    public static class Extensions
    {
        public static IObservable<T> NotNull<T>(this IObservable<T> thing)
        {
            return thing.Where(x => x != null);
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
