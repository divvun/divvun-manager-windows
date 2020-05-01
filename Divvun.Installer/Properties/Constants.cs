using Divvun.Installer.Models;
using System;

namespace Divvun.Installer.Properties
{
    public static class Constants
    {
        public const string PackageId = "divvun-installer-windows";
        public const string SentryDsn =
            "https://4022315e99574f87bad3ef6600e0c846:2a4288fd131446f1a863b725bb3beda7@sentry.io/1357465";
        public static Uri TaskbarIcon = new Uri("pack://application:,,,/divvun-logo-512.ico");
    }
}