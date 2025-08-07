using System;

namespace Inventory.Shared.Utils
{
    /// <summary>
    /// Helper class for handling Turkey timezone conversions
    /// Turkey timezone is UTC+3 (TRT - Turkey Time)
    /// </summary>
    public static class TimeZoneHelper
    {
        /// <summary>
        /// Turkey timezone info (UTC+3)
        /// </summary>
        public static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        
        /// <summary>
        /// Gets current time in Turkey timezone
        /// </summary>
        /// <returns>Current Turkey time</returns>
        public static DateTime GetTurkeyTime()
        {
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TurkeyTimeZone);
        }
        
        /// <summary>
        /// Converts UTC time to Turkey time
        /// </summary>
        /// <param name="utcTime">UTC time to convert</param>
        /// <returns>Turkey time</returns>
        public static DateTime ConvertToTurkeyTime(DateTime utcTime)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTime(utcTime, TurkeyTimeZone);
        }
        
        /// <summary>
        /// Converts Turkey time to UTC
        /// </summary>
        /// <param name="turkeyTime">Turkey time to convert</param>
        /// <returns>UTC time</returns>
        public static DateTime ConvertToUtc(DateTime turkeyTime)
        {
            return TimeZoneInfo.ConvertTime(turkeyTime, TurkeyTimeZone, TimeZoneInfo.Utc);
        }
        
        /// <summary>
        /// Gets Turkey time for storing in database (Turkey local time, not UTC)
        /// Based on requirements: Agent should send +3 time, others should display as received
        /// </summary>
        /// <returns>Current Turkey time</returns>
        public static DateTime GetUtcNowForStorage()
        {
            return GetTurkeyTime();
        }
        
        /// <summary>
        /// Formats date time for display in Turkey timezone
        /// </summary>
        /// <param name="utcDateTime">UTC datetime from database</param>
        /// <param name="format">Format string (default: "dd.MM.yyyy HH:mm:ss")</param>
        /// <returns>Formatted Turkey time string</returns>
        public static string FormatForDisplay(DateTime utcDateTime, string format = "dd.MM.yyyy HH:mm:ss")
        {
            var turkeyTime = ConvertToTurkeyTime(utcDateTime);
            return turkeyTime.ToString(format);
        }
        
        /// <summary>
        /// Checks if a device is considered offline based on last seen time
        /// Device is offline if not seen for more than 30 minutes
        /// </summary>
        /// <param name="lastSeenUtc">Last seen time in UTC</param>
        /// <param name="thresholdMinutes">Threshold in minutes (default: 30)</param>
        /// <returns>True if device is offline</returns>
        public static bool IsDeviceOffline(DateTime? lastSeenUtc, int thresholdMinutes = 30)
        {
            if (!lastSeenUtc.HasValue)
                return true;
                
            var timeDifference = DateTime.UtcNow - lastSeenUtc.Value;
            return timeDifference.TotalMinutes > thresholdMinutes;
        }
        
        /// <summary>
        /// Gets device status based on last seen time
        /// </summary>
        /// <param name="lastSeenUtc">Last seen time in UTC</param>
        /// <param name="currentStatus">Current device status</param>
        /// <param name="thresholdMinutes">Threshold in minutes (default: 30)</param>
        /// <returns>Calculated device status (0 = Active/Online, 1 = Inactive/Offline)</returns>
        public static int GetDeviceStatus(DateTime? lastSeenUtc, int currentStatus, int thresholdMinutes = 30)
        {
            // If device is offline (not seen for 30+ minutes), return status 1 (inactive)
            if (IsDeviceOffline(lastSeenUtc, thresholdMinutes))
                return 1;
            
            // If device was seen recently, return status 0 (active)
            return 0;
        }
    }
}