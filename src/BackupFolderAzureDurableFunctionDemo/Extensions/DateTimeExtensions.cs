using System;
using System.Globalization;
using BackupFolderAzureDurableFunctionDemo.Services.Models;

namespace BackupFolderAzureDurableFunctionDemo.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime EndOfThisMonth(this DateTime self)
        {
            var result = self.AddMonths(1);
            return new DateTime(result.Year, result.Month, 1).AddDays(-1);
        }

        public static DateTime EndOfThisWeek(this DateTime self)
        {
            return self.StartOfThisWeek().AddDays(6);
        }

        /// <summary>
        ///     Returns the end of the year.
        /// </summary>
        /// <param name="self"></param>
        public static DateTime EndOfThisYear(this DateTime self)
        {
            return new DateTime(self.Year, 12, 31);
        }

        /// <summary>
        /// Converts Central European Standard Time to Utc Date/time
        /// </summary>
        /// <param name="dt">Central European Standard Date/time</param>
        /// <returns>Utc DateTime</returns>
        public static DateTime FromCentralEuropeanStandardTimeToUtc(this DateTime dt)
        {
            var userTime = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
            return userTime.FromTimeZoneDateTimeToUtc();
        }

        /// <summary>
        /// Converts Time Zone Date/time to Utc Date/time
        /// </summary>
        /// <param name="dt">Time zone Date/time</param>
        /// <param name="id">Time zone Id, default Central European Standard Time</param>
        /// <returns>Time zone DateTime</returns>
        public static DateTime FromTimeZoneDateTimeToUtc(this DateTime dt, string id = "Central European Standard Time")
        {
            var infoTimeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return TimeZoneInfo.ConvertTimeToUtc(dt, infoTimeZone);
        }

        /// <summary>
        /// Converts Utc Date/time to the Central European Standard Time
        /// </summary>
        /// <param name="utcDateTime">Utc Date/time</param>
        /// <returns>Central European Standard DateTime</returns>
        public static DateTime FromUtcToCentralEuropeanStandardTime(this DateTime utcDateTime)
        {
            return utcDateTime.FromUtcToTimeZoneDateTime();
        }

        /// <summary>
        /// Converts Utc Date/time to the Time Zone Date/time
        /// </summary>
        /// <param name="utcDateTime">Utc Date/time</param>
        /// <param name="id">Time zone Id, default Central European Standard Time</param>
        /// <returns>Time zone DateTime</returns>
        public static DateTime FromUtcToTimeZoneDateTime(this DateTime utcDateTime, string id = "Central European Standard Time")
        {
            var infoTimeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, infoTimeZone);
        }

        // This presumes that weeks start with Monday.
        // Week 1 is the 1st week of the year with a Thursday in it.
        public static int GetIso8601WeekOfYear(this DateTime self)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(self);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                self = self.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(self, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Finds the next date whose day of the week equals the specified day of the week.
        /// </summary>
        /// <param name="startDate">
        ///		The date to begin the search.
        /// </param>
        /// <param name="desiredDay">
        ///		The desired day of the week whose date will be returneed.
        /// </param>
        /// <returns>
        ///		The returned date occurs on the given date's week.
        ///		If the given day occurs before given date, the date for the
        ///		following week's desired day is returned.
        /// </returns>
        public static DateTime GetNextDateForDay(this DateTime startDate, DayOfWeek desiredDay)
        {
            // Given a date and day of week,
            // find the next date whose day of the week equals the specified day of the week.
            return startDate.AddDays(DaysToAdd(startDate.DayOfWeek, desiredDay));
        }

        /// <summary>
        ///     Returns the quarter of the year within which the given date falls:
        ///     Jan, Feb, Mar = 1
        ///     ...
        ///     Oct, Nov, Dec = 4
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static int Quarter(this DateTime self)
        {
            return (self.Month + 2) / 3;
        }

        public static DateTime RoundDown(this DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            return new DateTime(dt.Ticks - delta, dt.Kind);
        }

        //Usage:
        //var date = new DateTime(2010, 02, 05, 10, 35, 25, 450); // 2010/02/05 10:35:25
        //var roundedUp = date.RoundUp(TimeSpan.FromMinutes(15)); // 2010/02/05 10:45:00
        //var roundedDown = date.RoundDown(TimeSpan.FromMinutes(15)); // 2010/02/05 10:30:00
        //var roundedToNearest = date.RoundToNearest(TimeSpan.FromMinutes(15)); // 2010/02/05 10:30:00
        public static DateTime RoundUp(this DateTime dt, TimeSpan d)
        {
            var delta = (d.Ticks - (dt.Ticks % d.Ticks)) % d.Ticks;
            return new DateTime(dt.Ticks + delta, dt.Kind);
        }

        /// <summary>
        ///     Returns the start of the month for the current month.
        /// </summary>
        /// <param name="self"></param>
        public static DateTime StartOfThisMonth(this DateTime self)
        {
            return new DateTime(self.Year, self.Month, 1);
        }

        public static DateTime StartOfThisWeek(this DateTime self)
        {
            var returnDateTime = self.AddDays(-(self.DayOfWeek - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek));
            return returnDateTime;
        }

        /// <summary>
        ///     Returns the start of the year.
        /// </summary>
        /// <param name="self"></param>
        public static DateTime StartOfThisYear(this DateTime self)
        {
            return new DateTime(self.Year, 1, 1);
        }

        /// <summary>
        ///     Return a datetime as a Unix epoch timestamp (seconds since Jan 1, 1970, midnight UTC)
        /// </summary>
        /// <param name="self">The date to convert.</param>
        /// <returns>Seconds since Unix epoch.</returns>
        public static DateEpoch ToDateEpoch(this DateTime self)
        {
            if (self == default(DateTime))
            {
                self = DateTime.UtcNow;
            }
            return new DateEpoch(self);
        }

        public static string ToSqlDate(this DateTime self)
        {
            return self.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static string ToSqlDateShort(this DateTime self)
        {
            return self.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        public static string ToSqlDateTime(this DateTime self)
        {
            return self.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Get this datetime as a Unix epoch timestamp (seconds since Jan 1, 1970, midnight UTC).
        /// </summary>
        /// <param name="self">The date to convert.</param>
        /// <returns>Seconds since Unix epoch.</returns>
        public static long ToUnixEpochDate(this DateTime self)
        {
            var timeStart = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            if (self < timeStart)
            {
                return 0;
            }
            return (long)Math.Round((self.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
        }

        /// <summary>
        ///     Get this datetime as a Unix epoch timestamp (seconds since Jan 1, 1970, midnight UTC).
        /// </summary>
        /// <param name="self">The date to convert.</param>
        /// <returns>Seconds since Unix epoch.</returns>
        public static long ToUnixEpochDate(this DateTime? self)
        {
            var dateToConvert = self ?? DateTime.MinValue;
            var timeStart = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            if (dateToConvert < timeStart)
            {
                return 0;
            }
            return (long)Math.Round((dateToConvert.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
        }

        /// <summary>
        /// Calculates the number of days to add to the given day of
        /// the week in order to return the next occurrence of the
        /// desired day of the week.
        /// http://angstrey.com/index.php/2009/04/25/finding-the-next-date-for-day-of-week/
        /// </summary>
        /// <param name="current">
        ///		The starting day of the week.
        /// </param>
        /// <param name="desired">
        ///		The desired day of the week.
        /// </param>
        /// <returns>
        ///		The number of days to add to <var>current</var> day of week
        ///		in order to achieve the next <var>desired</var> day of week.
        /// </returns>
        private static int DaysToAdd(DayOfWeek current, DayOfWeek desired)
        {
            var c = (int)current;
            var d = (int)desired;
            var n = 7 - c + d;
            return n > 7 ? n % 7 : n;
        }
    }
}