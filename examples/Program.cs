using System;
using BenScr.Scheduling;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

var anchor = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
var interval = new IntervalSchedule(anchor, TimeSpan.FromMinutes(15));
Require(interval.Next(anchor) == anchor.AddMinutes(15), "Interval progression failed.");
Require(interval.Next(anchor, true) == anchor, "Inclusive boundary failed.");
Require(new IntervalSchedule(DateTimeOffset.MaxValue, TimeSpan.FromTicks(1)).Next(DateTimeOffset.MaxValue) is null,
    "Maximum range failed.");

var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 31);
var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 27);
var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
    TimeSpan.FromHours(1), start, end);
var zone = TimeZoneInfo.CreateCustomTimeZone("Example", TimeSpan.FromHours(1), "Example", "Standard", "Daylight", new[] { rule });
var daily = new CalendarSchedule(new TimeOnly(2, 30), zone, ambiguousTimePolicy: AmbiguousTimePolicy.Both);
Require(daily.Next(new DateTimeOffset(2024, 3, 30, 23, 0, 0, TimeSpan.Zero), new DateOnly(2024, 3, 31)) is null,
    "Invalid local time must be skipped.");
var firstFold = new DateTimeOffset(2024, 10, 27, 0, 30, 0, TimeSpan.Zero);
Require(daily.Next(firstFold, new DateOnly(2024, 10, 27)) == firstFold.AddHours(1), "Repeated local time failed.");
var weekly = new CalendarSchedule(new TimeOnly(9, 0), TimeZoneInfo.Utc, Weekdays.Friday);
Require(weekly.Next(anchor, new DateOnly(2024, 1, 4)) is null, "Horizon failed.");
Require(weekly.Next(anchor, new DateOnly(2024, 1, 5)) == anchor.AddDays(4).AddHours(9), "Weekly selection failed.");
Console.WriteLine("UTC intervals, inclusive boundaries, weekdays, DST gaps/folds, horizon and range checks passed.");
