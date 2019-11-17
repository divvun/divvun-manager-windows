using Pahkat.Sdk.Native;
using System;
using System.Globalization;
using System.Linq;

namespace Pahkat.Sdk
{
    public static class Settings
    {
        public static void EnableLogging()
        {
            pahkat_client.pahkat_enable_logging();
        }
    }
}

