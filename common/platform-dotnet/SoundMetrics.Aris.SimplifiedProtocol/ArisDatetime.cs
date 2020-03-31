using System;

namespace SoundMetrics.Aris.SimplifiedProtocol
{
    public static class ArisDatetime
    {
        public static string GetTimestamp()
        {
            var now = DateTime.Now;
            // use this form:  2020-Jan-10 15:01:25
            return $"{now.Year}-{MonthAbbreviations[now.Month - 1]:D2}-{now.Day:D2} "
                + $"{now.Hour:D2}:{now.Minute:D2}:{now.Second:D2}";
        }

        private static readonly string[] MonthAbbreviations = new[]
        {
            "Jan",
            "Feb",
            "Mar",
            "Apr",
            "May",
            "Jun",
            "Jul",
            "Aug",
            "Sep",
            "Oct",
            "Nov",
            "Dec",
        };
    }
}
