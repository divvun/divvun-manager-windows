using Pahkat.Sdk.Native;
using System;
using System.Globalization;
using System.Linq;

namespace Pahkat.Sdk
{
    public static class Settings
    {
        public enum LogLevel : byte
        {
            Disabled = 0,
            Error,
            Warn,
            Info,
            Debug,
            Trace
        }

        public static void EnableLogging(LogLevel level = LogLevel.Debug)
        {
            pahkat_client.pahkat_enable_logging((byte)level);
        }
    }
}

