using System;
using System.Globalization;
using System.Text;
using System.Security.Principal;
using Divvun.Installer.Extensions;

namespace Divvun.Installer.Util
{
    public static class Util
    {
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string BytesToString(ulong bytes)
        {
            return BytesToString((long) bytes);
        }

        public static string BytesToString(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (bytes == 0)
            {
                return "0 " + suf[0];
            }
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            if (place >= suf.Length)
            {
                return "--";
            }
            var num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return num.ToString(CultureInfo.CurrentCulture) + " " + suf[place];
        }

        public static CultureInfo GetCulture(string tag)
        {
            try
            {
                return new CultureInfo(tag);
            }
            catch (Exception)
            {
                return CultureInfo.CurrentCulture;
            }
        }

        public static string GetCultureDisplayName(string tag)
        {
            if (tag == "zxx" || tag == "")
            {
                return "---";
            }
            
            string langCode;
            CultureInfo? culture = null;
            try
            {
                culture = new CultureInfo(tag);
                langCode = culture.ThreeLetterISOLanguageName;
            }
            catch (Exception)
            {
                // Best attempt
                langCode = tag.Split('_', '-')[0];
            }

            var data = Iso639.GetTag(langCode);
            if (data?.Autonym != null && data.Autonym != "")
            {
                return data.Autonym;
            }

            if (culture != null && culture.DisplayName != "" && culture.DisplayName != culture.EnglishName)
            {
                return culture.DisplayName;
            }

            if (data?.Name != null && data.Name != "")
            {
                return data.Name;
            }

            return tag;
        }
    }
}