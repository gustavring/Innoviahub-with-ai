using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Services
{
    public class TimeService
    {
        private readonly TimeZoneInfo _swedishTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

        public DateTime GetCurrentSwedishHour()
        {
            var swedishNow = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                _swedishTimeZone
            );

            return new DateTime(
                swedishNow.Year,
                swedishNow.Month,
                swedishNow.Day,
                swedishNow.Hour,
                0,
                0
            );
        }

        public DateTime ToUtc(DateTime localTime)
        {
            var unspecifiedTime = DateTime.SpecifyKind(
                localTime,
                DateTimeKind.Unspecified
            );

            return TimeZoneInfo.ConvertTimeToUtc(
                unspecifiedTime,
                _swedishTimeZone
            );
        }

        public DateTime GetCurrentSwedishTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                _swedishTimeZone
            );
        }
    }
}