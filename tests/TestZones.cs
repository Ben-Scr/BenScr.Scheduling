using System;

namespace BenScr.Scheduling.Tests;

internal static class TestZones
{
    internal static TimeZoneInfo European { get; } = Create("Test-European", 1, 1, 3, 31, 2, 10, 27, 3);
    internal static TimeZoneInfo NegativeDaylight { get; } = Create("Test-Negative", 2, -1, 3, 10, 2, 11, 3, 2);

    internal static TimeZoneInfo Create(string id, double baseHours, double daylightHours,
        int startMonth, int startDay, int startHour, int endMonth, int endDay, int endHour)
    {
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, startHour, 0, 0), startMonth, startDay);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, endHour, 0, 0), endMonth, endDay);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2020, 1, 1),
            new DateTime(2030, 12, 31), TimeSpan.FromHours(daylightHours), start, end);
        return TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(baseHours), id, id, id, new[] { rule });
    }

    internal static DateTimeOffset Utc(int year, int month, int day, int hour = 0, int minute = 0)
        => new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
