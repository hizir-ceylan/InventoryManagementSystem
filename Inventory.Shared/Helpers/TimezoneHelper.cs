using System;

namespace Inventory.Shared.Helpers
{
    public static class TimezoneHelper
    {
        private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Turkey Standard Time", 
            TimeSpan.FromHours(3), 
            "Turkey Standard Time", 
            "Turkey Standard Time");

        /// <summary>
        /// Converts UTC DateTime to Turkey time (+3)
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime</param>
        /// <returns>DateTime in Turkey timezone</returns>
        public static DateTime ConvertToTurkeyTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                // If not UTC, assume it's UTC for conversion
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TurkeyTimeZone);
        }

        /// <summary>
        /// Converts Turkey time to UTC
        /// </summary>
        /// <param name="turkeyDateTime">DateTime in Turkey timezone</param>
        /// <returns>UTC DateTime</returns>
        public static DateTime ConvertToUtc(DateTime turkeyDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(turkeyDateTime, TurkeyTimeZone);
        }

        /// <summary>
        /// Gets current Turkey time
        /// </summary>
        /// <returns>Current DateTime in Turkey timezone</returns>
        public static DateTime GetTurkeyNow()
        {
            return ConvertToTurkeyTime(DateTime.UtcNow);
        }

        /// <summary>
        /// Formats DateTime to Turkish format string with Turkey timezone
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime</param>
        /// <param name="format">Optional format string</param>
        /// <returns>Formatted string in Turkey time</returns>
        public static string FormatInTurkeyTime(DateTime utcDateTime, string format = "dd.MM.yyyy HH:mm:ss")
        {
            var turkeyTime = ConvertToTurkeyTime(utcDateTime);
            return turkeyTime.ToString(format);
        }

        /// <summary>
        /// Checks if a device was seen in the last 12 hours (Turkey time)
        /// </summary>
        /// <param name="lastSeenUtc">Last seen time in UTC</param>
        /// <returns>True if seen in last 12 hours</returns>
        public static bool IsActiveInLast12Hours(DateTime? lastSeenUtc)
        {
            if (!lastSeenUtc.HasValue)
                return false;

            var currentTurkeyTime = GetTurkeyNow();
            var lastSeenTurkeyTime = ConvertToTurkeyTime(lastSeenUtc.Value);
            
            return (currentTurkeyTime - lastSeenTurkeyTime).TotalHours <= 12;
        }
    }
}