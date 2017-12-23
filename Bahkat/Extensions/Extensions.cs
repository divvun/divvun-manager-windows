using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

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

        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : new()
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
    }
}
