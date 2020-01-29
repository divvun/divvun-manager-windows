using Pahkat.Sdk.Native;
using System;
using System.Globalization;
using System.Linq;

namespace Pahkat.Sdk
{
    public static class LogLevelExt
    {
        public static LogLevel From(byte value)
        {
            if (value > 5)
            {
                value = 5;
            }

            return (LogLevel)value;
        }
    }

    public enum LogLevel : byte
    {
        Disabled = 0,
        Error,
        Warn,
        Info,
        Debug,
        Trace
    }

    public static class Settings
    {
        public static void EnableLogging(LogLevel level = LogLevel.Debug)
        {
            pahkat_client.pahkat_enable_logging((byte)level);
        }

        public static void SetLoggingCallback(LoggingCallback callback)
        {
            pahkat_client.pahkat_set_logging_callback(callback);
        }
    }
}

