using Pahkat.Models;
using System;

namespace Pahkat.Extensions
{
    public static class PeriodIntervalExt
    {
        public static TimeSpan ToTimeSpan(this PeriodInterval periodInterval)
        {
            switch (periodInterval)
            {
                case PeriodInterval.Daily:
                    return TimeSpan.FromDays(1);
                case PeriodInterval.Weekly:
                    return TimeSpan.FromDays(7);
                case PeriodInterval.Fortnightly:
                    return TimeSpan.FromDays(14);
                case PeriodInterval.Monthly:
                    return TimeSpan.FromDays(28);
                case PeriodInterval.Never:
                    return TimeSpan.Zero;
                default:
                    return TimeSpan.Zero;
            }
        }

        public static string ToLocalisedName(this PeriodInterval periodInterval)
        {
            switch (periodInterval)
            {
                case PeriodInterval.Daily:
                    return Strings.Daily;
                case PeriodInterval.Weekly:
                    return Strings.Weekly;
                case PeriodInterval.Fortnightly:
                    return Strings.EveryTwoWeeks;
                case PeriodInterval.Monthly:
                    return Strings.EveryFourWeeks;
                case PeriodInterval.Never:
                    return Strings.Never;
                default:
                    return "";
            }
        }
    }
}