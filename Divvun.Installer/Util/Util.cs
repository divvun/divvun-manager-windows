using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Divvun.Installer.Util {

public static class Util {
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async IAsyncEnumerable<U> Map<T, U>(IEnumerable<T> enumerable, Func<T, Task<U>> selector) {
        foreach (var element in enumerable) {
            yield return await selector(element);
        }
    }

    public static async IAsyncEnumerable<T> Filter<T>(IEnumerable<T> enumerable, Func<T, Task<bool>> selector) {
        foreach (var element in enumerable) {
            if (await selector(element)) {
                yield return element;
            }
        }
    }

    public static bool IsAdministrator() {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static string BytesToString(ulong bytes) {
        return BytesToString((long)bytes);
    }

    public static string BytesToString(long bytes) {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        if (bytes == 0) {
            return "0 " + suf[0];
        }

        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        if (place >= suf.Length) {
            return "--";
        }

        var num = Math.Round(bytes / Math.Pow(1024, place), 2);
        return num.ToString(CultureInfo.CurrentCulture) + " " + suf[place];
    }

    public static CultureInfo GetCulture(string tag) {
        try {
            return new CultureInfo(tag);
        }
        catch (Exception) {
            return CultureInfo.CurrentCulture;
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
        
        // Love the new culture bugs.
        if (langCode == "") {
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
}

}