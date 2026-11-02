using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BenScr.Scheduling.Tests;

public sealed class CalendarTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(-1)]
    public void RejectsEmptyOrUndefinedWeekdays(int days)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarSchedule(default, TimeZoneInfo.Utc, (Weekdays)days));
    }

    [Fact]
    public void RejectsNullZoneAndUndefinedAmbiguity()
    {
        Assert.Throws<ArgumentNullException>(() => new CalendarSchedule(default, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarSchedule(default, TimeZoneInfo.Utc,
            ambiguousTimePolicy: (AmbiguousTimePolicy)77));
    }

    [Fact]
    public void DailyBoundaryAndPropertiesAreExplicit()
    {
        var schedule = new CalendarSchedule(new TimeOnly(12, 30), TimeZoneInfo.Utc);
        DateTimeOffset query = TestZones.Utc(2024, 2, 28, 12, 30);
        Assert.Equal(query, schedule.Next(query, new DateOnly(2024, 2, 28), true));
        Assert.Null(schedule.Next(query, new DateOnly(2024, 2, 28)));
        Assert.Equal(TestZones.Utc(2024, 2, 29, 12, 30), schedule.Next(query, new DateOnly(2024, 3, 1)));
        Assert.Equal(new TimeOnly(12, 30), schedule.Time);
        Assert.Same(TimeZoneInfo.Utc, schedule.TimeZone);
        Assert.Equal(Weekdays.EveryDay, schedule.Days);
        Assert.Equal(AmbiguousTimePolicy.Earlier, schedule.AmbiguousTimePolicy);
    }

    [Theory]
    [InlineData(Weekdays.Sunday, 7)]
    [InlineData(Weekdays.Monday, 1)]
    [InlineData(Weekdays.Tuesday, 2)]
    [InlineData(Weekdays.Wednesday, 3)]
    [InlineData(Weekdays.Thursday, 4)]
    [InlineData(Weekdays.Friday, 5)]
    [InlineData(Weekdays.Saturday, 6)]
    public void EachWeekdayMapsToLocalCalendar(Weekdays days, int januaryDay)
    {
        var schedule = new CalendarSchedule(new TimeOnly(10, 0), TimeZoneInfo.Utc, days);
        Assert.Equal(TestZones.Utc(2024, 1, januaryDay, 10), schedule.Next(TestZones.Utc(2024, 1, 1), new DateOnly(2024, 1, 7)));
    }

    [Fact]
    public void WeeklyUnionSelectsEarliestDayAndWrapsWeek()
    {
        var schedule = new CalendarSchedule(new TimeOnly(9, 0), TimeZoneInfo.Utc, Weekdays.Monday | Weekdays.Friday);
        Assert.Equal(TestZones.Utc(2024, 1, 5, 9), schedule.Next(TestZones.Utc(2024, 1, 1, 9), new DateOnly(2024, 2, 1)));
        Assert.Equal(TestZones.Utc(2024, 1, 8, 9), schedule.Next(TestZones.Utc(2024, 1, 5, 9), new DateOnly(2024, 2, 1)));
    }

    [Fact]
    public void SpringGapIsSkippedWithoutMovingWallTime()
    {
        var schedule = new CalendarSchedule(new TimeOnly(2, 30), TestZones.European);
        DateTimeOffset query = TestZones.Utc(2024, 3, 30, 23);
        Assert.True(TestZones.European.IsInvalidTime(new DateTime(2024, 3, 31, 2, 30, 0)));
        Assert.Null(schedule.Next(query, new DateOnly(2024, 3, 31)));
        Assert.Equal(TestZones.Utc(2024, 4, 1, 0, 30), schedule.Next(query, new DateOnly(2024, 4, 1)));
    }

    [Theory]
    [InlineData(AmbiguousTimePolicy.Earlier, 0)]
    [InlineData(AmbiguousTimePolicy.Later, 1)]
    [InlineData(AmbiguousTimePolicy.Both, 0)]
    public void AutumnFoldSelectsUtcInstant(AmbiguousTimePolicy policy, int hour)
    {
        var schedule = new CalendarSchedule(new TimeOnly(2, 30), TestZones.European, ambiguousTimePolicy: policy);
        Assert.Equal(TestZones.Utc(2024, 10, 27, hour, 30), schedule.Next(TestZones.Utc(2024, 10, 26, 23), new DateOnly(2024, 10, 27)));
    }

    [Fact]
    public void BothReturnsSecondFoldAfterFirstEvenThoughWallTimeMovesBack()
    {
        var schedule = new CalendarSchedule(new TimeOnly(2, 30), TestZones.European, ambiguousTimePolicy: AmbiguousTimePolicy.Both);
        DateTimeOffset first = TestZones.Utc(2024, 10, 27, 0, 30);
        DateTimeOffset second = TestZones.Utc(2024, 10, 27, 1, 30);
        DateOnly horizon = new(2024, 10, 27);
        Assert.Equal(first, schedule.Next(first, horizon, true));
        Assert.Equal(second, schedule.Next(first, horizon));
        Assert.Equal(second, schedule.Next(first.AddMinutes(45), horizon));
        Assert.Equal(second, schedule.Next(second, horizon, true));
        Assert.Null(schedule.Next(second, horizon));
    }

    [Fact]
    public void EarlierDoesNotFallBackToLaterWhenQueryPassesFirst()
    {
        var schedule = new CalendarSchedule(new TimeOnly(2, 30), TestZones.European);
        Assert.Null(schedule.Next(TestZones.Utc(2024, 10, 27, 0, 30), new DateOnly(2024, 10, 27)));
    }

    [Fact]
    public void SkipPolicyOmitsEntireFoldDate()
    {
        var schedule = new CalendarSchedule(new TimeOnly(2, 30), TestZones.European, ambiguousTimePolicy: AmbiguousTimePolicy.Skip);
        Assert.Null(schedule.Next(TestZones.Utc(2024, 10, 26, 23), new DateOnly(2024, 10, 27)));
        Assert.Equal(TestZones.Utc(2024, 10, 28, 1, 30), schedule.Next(TestZones.Utc(2024, 10, 26, 23), new DateOnly(2024, 10, 28)));
    }

    [Fact]
    public void HalfHourDaylightShiftIsNotAssumedToBeOneHour()
    {
        TimeZoneInfo zone = TestZones.Create("HalfHour", 10, 0.5, 10, 5, 2, 4, 7, 2);
        var schedule = new CalendarSchedule(new TimeOnly(1, 45), zone, ambiguousTimePolicy: AmbiguousTimePolicy.Both);
        DateTimeOffset first = TestZones.Utc(2024, 4, 6, 15, 15);
        DateTimeOffset second = TestZones.Utc(2024, 4, 6, 15, 45);
        Assert.Equal(first, schedule.Next(TestZones.Utc(2024, 4, 6, 15), new DateOnly(2024, 4, 7)));
        Assert.Equal(second, schedule.Next(first, new DateOnly(2024, 4, 7)));
    }

    [Fact]
    public void NegativeDaylightDeltaAlsoResolvesByUtcOrder()
    {
        var schedule = new CalendarSchedule(new TimeOnly(1, 30), TestZones.NegativeDaylight,
            ambiguousTimePolicy: AmbiguousTimePolicy.Both);
        DateTimeOffset first = TestZones.Utc(2024, 3, 9, 23, 30);
        Assert.True(TestZones.NegativeDaylight.IsAmbiguousTime(new DateTime(2024, 3, 10, 1, 30, 0)));
        Assert.Equal(first, schedule.Next(TestZones.Utc(2024, 3, 9, 23), new DateOnly(2024, 3, 10)));
        Assert.Equal(first.AddHours(1), schedule.Next(first, new DateOnly(2024, 3, 10)));
    }

    [Theory]
    [InlineData(-14)]
    [InlineData(14)]
    public void CalendarHorizonUsesLocalDateAcrossUtcDayBoundary(int hours)
    {
        TimeZoneInfo zone = TimeZoneInfo.CreateCustomTimeZone("Extreme", TimeSpan.FromHours(hours), "Extreme", "Extreme");
        var schedule = new CalendarSchedule(new TimeOnly(0, 30), zone);
        DateTimeOffset expected = new DateTimeOffset(2024, 1, 2, 0, 30, 0, TimeSpan.FromHours(hours)).ToUniversalTime();
        Assert.Equal(expected, schedule.Next(expected.AddTicks(-1).ToOffset(TimeSpan.FromHours(-hours)), new DateOnly(2024, 1, 2)));
        Assert.Null(schedule.Next(expected, new DateOnly(2024, 1, 2)));
    }

    [Fact]
    public void PastHorizonIsAnEmptySearch()
    {
        var schedule = new CalendarSchedule(default, TimeZoneInfo.Utc);
        Assert.Null(schedule.Next(TestZones.Utc(2024, 1, 1), new DateOnly(1900, 1, 1)));
    }

    [Fact]
    public void MaximumTimeRetainsTickPrecisionAndDoesNotOverflow()
    {
        var schedule = new CalendarSchedule(TimeOnly.MaxValue, TimeZoneInfo.Utc);
        Assert.Equal(DateTimeOffset.MaxValue, schedule.Next(DateTimeOffset.MaxValue.AddTicks(-1), DateOnly.MaxValue));
        Assert.Equal(DateTimeOffset.MaxValue, schedule.Next(DateTimeOffset.MaxValue, DateOnly.MaxValue, true));
        Assert.Null(schedule.Next(DateTimeOffset.MaxValue, DateOnly.MaxValue));
    }

    [Fact]
    public void MinimumTimeIsAvailableInclusively()
    {
        var schedule = new CalendarSchedule(TimeOnly.MinValue, TimeZoneInfo.Utc);
        Assert.Equal(DateTimeOffset.MinValue, schedule.Next(DateTimeOffset.MinValue, DateOnly.MinValue, true));
        Assert.Null(schedule.Next(DateTimeOffset.MinValue, DateOnly.MinValue));
    }

    [Fact]
    public void UnrepresentableUtcTimesAreOmittedInsteadOfClamped()
    {
        var positive = TimeZoneInfo.CreateCustomTimeZone("Positive", TimeSpan.FromHours(14), "Positive", "Positive");
        var negative = TimeZoneInfo.CreateCustomTimeZone("Negative", TimeSpan.FromHours(-14), "Negative", "Negative");
        Assert.Null(new CalendarSchedule(TimeOnly.MinValue, positive).Next(DateTimeOffset.MinValue, DateOnly.MinValue, true));
        Assert.Null(new CalendarSchedule(TimeOnly.MaxValue, negative).Next(DateTimeOffset.MaxValue.AddTicks(-1), DateOnly.MaxValue));
        Assert.Equal(new DateTimeOffset(1, 1, 1, 10, 0, 0, TimeSpan.Zero),
            new CalendarSchedule(TimeOnly.MinValue, positive).Next(DateTimeOffset.MinValue, new DateOnly(1, 1, 2)));
    }

    [Fact]
    public void SameImmutableScheduleSupportsConcurrentQueries()
    {
        var schedule = new CalendarSchedule(new TimeOnly(9, 0), TestZones.European);
        DateTimeOffset after = TestZones.Utc(2024, 3, 31, 0);
        DateTimeOffset?[] results = new DateTimeOffset?[128];
        Parallel.For(0, results.Length, index => results[index] = schedule.Next(after, new DateOnly(2024, 4, 2)));
        Assert.All(results, result => Assert.Equal(TestZones.Utc(2024, 3, 31, 7), result));
    }

    [Theory]
    [InlineData(3, 30)]
    [InlineData(10, 26)]
    public void BothPolicyMatchesIndependentUtcMinuteScan(int month, int day)
    {
        DateTimeOffset start = TestZones.Utc(2024, month, day);
        DateTimeOffset end = start.AddDays(3);
        var schedule = new CalendarSchedule(new TimeOnly(2, 30), TestZones.European,
            ambiguousTimePolicy: AmbiguousTimePolicy.Both);
        DateTimeOffset[] reference = Enumerable.Range(0, 3 * 24 * 60)
            .Select(minute => start.AddMinutes(minute))
            .Where(instant => TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, TestZones.European).DateTime) == new TimeOnly(2, 30))
            .ToArray();
        DateTimeOffset cursor = start.AddTicks(-1);
        foreach (DateTimeOffset expected in reference)
        {
            DateTimeOffset? actual = schedule.Next(cursor, DateOnly.FromDateTime(end.DateTime));
            Assert.Equal(expected, actual);
            cursor = actual!.Value;
        }
        Assert.True(schedule.Next(cursor, DateOnly.FromDateTime(end.DateTime)) >= end);
    }
}
