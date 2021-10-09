using System;
using System.Runtime.InteropServices;

namespace DotNet.Extensions
{
    internal static class DateTimeExtensions
    {
        public static DateTime ConvertToChinaTimeFromUtc(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Utc)
            {
                string timeZoneName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "China Standard Time" : "Asia/Shanghai";
                TimeZoneInfo chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneName);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, chinaTimeZone);
            }

            return utcDateTime;
        }
    }
}
