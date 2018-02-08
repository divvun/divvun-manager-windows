using Pahkat.Models;
using System;

namespace Pahkat.Properties
{
    public static class Constants
    {
        public const string RegistryId = "Pahkat";
        public const string Repository = "https://x.brendan.so/test-repo/";
        public const PeriodInterval UpdateCheckInterval = PeriodInterval.Daily;
        public const string Channel = "Stable";

        public const string SentryDsn =
            "https://dd5eed16a4674ba883f002d93d067f65:9412c45254514e4d8a10dba63607d2fc@sentry.io/260424";

        public static Uri TaskbarIcon = new Uri("pack://application:,,,/UI/TaskbarIcon.ico");
        public static Uri PahkatUpdateUri = new Uri("https://repo.bahkat.org/windows");
    }
}