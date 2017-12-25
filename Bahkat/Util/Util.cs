using System;
using System.Globalization;

namespace Bahkat.Util
{
    public static class Util
    {
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
    }
}