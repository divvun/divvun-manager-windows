using Bahkat.Models;
using System;

namespace Bahkat.Properties
{
    public static class Constants
    {
        public const string RegistryId = "Bahkat";
        public const string Repository = "http://localhost:8000/";
        public const PeriodInterval UpdateCheckInterval = PeriodInterval.Daily;
        public const string Channel = "Stable";

        public const string SentryDsn =
            "";

        public static Uri TaskbarIcon = new Uri("pack://application:,,,/UI/TaskbarIcon.ico");
        public static Uri BahkatUpdateUri = new Uri("https://repo.bahkat.org");
    }
}