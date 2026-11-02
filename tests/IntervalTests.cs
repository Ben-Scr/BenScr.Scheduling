using System;
using System.Numerics;
using Xunit;

namespace BenScr.Scheduling.Tests;

public sealed class IntervalTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void RejectsNonpositiveIntervals(long ticks)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalSchedule(DateTimeOffset.MinValue, TimeSpan.FromTicks(ticks)));
    }

    [Fact]
    public void AnchorNormalizesOffsetAndStartsTheSequence()
    {
        var anchor = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
        var schedule = new IntervalSchedule(anchor, TimeSpan.FromMinutes(7));
        Assert.Equal(TestZones.Utc(2024, 1, 1, 10), schedule.Anchor);
        Assert.Equal(TimeSpan.Zero, schedule.Anchor.Offset);
        Assert.Equal(TimeSpan.FromMinutes(7), schedule.Interval);
        Assert.Equal(schedule.Anchor, schedule.Next(anchor.AddYears(-1)));
        Assert.Equal(schedule.Anchor, schedule.Next(anchor, true));
        Assert.Equal(schedule.Anchor.AddMinutes(7), schedule.Next(anchor));
    }

    [Fact]
    public void BetweenOccurrencesAlwaysRoundsUp()
    {
        var schedule = new IntervalSchedule(TestZones.Utc(2024, 1, 1), TimeSpan.FromTicks(13));
        Assert.Equal(schedule.Anchor.AddTicks(26), schedule.Next(schedule.Anchor.AddTicks(14)));
        Assert.Equal(schedule.Anchor.AddTicks(26), schedule.Next(schedule.Anchor.AddTicks(14), true));
    }

    [Fact]
    public void InclusiveAndExclusiveOperateOnInstants()
    {
        var schedule = new IntervalSchedule(TestZones.Utc(2024, 1, 1), TimeSpan.FromHours(1));
        DateTimeOffset shifted = schedule.Anchor.AddHours(4).ToOffset(TimeSpan.FromHours(-11));
        Assert.Equal(schedule.Anchor.AddHours(4), schedule.Next(shifted, true));
        Assert.Equal(schedule.Anchor.AddHours(5), schedule.Next(shifted));
        Assert.Equal(TimeSpan.Zero, schedule.Next(shifted)!.Value.Offset);
    }

    [Fact]
    public void SingleTickIntervalIncludesExactMaximum()
    {
        var schedule = new IntervalSchedule(DateTimeOffset.MinValue, TimeSpan.FromTicks(1));
        Assert.Equal(DateTimeOffset.MaxValue, schedule.Next(DateTimeOffset.MaxValue, true));
        Assert.Equal(DateTimeOffset.MaxValue, schedule.Next(DateTimeOffset.MaxValue.AddTicks(-1)));
        Assert.Null(schedule.Next(DateTimeOffset.MaxValue));
    }

    [Fact]
    public void MaximumIntervalDoesNotOverflow()
    {
        var schedule = new IntervalSchedule(DateTimeOffset.MinValue, TimeSpan.MaxValue);
        Assert.Equal(DateTimeOffset.MinValue, schedule.Next(DateTimeOffset.MinValue, true));
        Assert.Null(schedule.Next(DateTimeOffset.MinValue));
        Assert.Null(schedule.Next(DateTimeOffset.MaxValue));
    }

    [Fact]
    public void LateAnchorHasOnlyRepresentableOccurrences()
    {
        var schedule = new IntervalSchedule(DateTimeOffset.MaxValue.AddTicks(-9), TimeSpan.FromTicks(5));
        Assert.Equal(DateTimeOffset.MaxValue.AddTicks(-4), schedule.Next(schedule.Anchor));
        Assert.Null(schedule.Next(DateTimeOffset.MaxValue.AddTicks(-4)));
    }

    [Fact]
    public void MaximumAnchorCanBeReturnedInclusively()
    {
        var schedule = new IntervalSchedule(DateTimeOffset.MaxValue, TimeSpan.FromTicks(1));
        Assert.Equal(DateTimeOffset.MaxValue, schedule.Next(DateTimeOffset.MinValue));
        Assert.Equal(DateTimeOffset.MaxValue, schedule.Next(DateTimeOffset.MaxValue, true));
        Assert.Null(schedule.Next(DateTimeOffset.MaxValue));
    }

    [Fact]
    public void ArithmeticMatchesBigIntegerReferenceForBoundaryCombinations()
    {
        long maximum = DateTime.MaxValue.Ticks;
        long[] anchors = { 0, 1, maximum / 2, maximum - 1, maximum };
        long[] intervals = { 1, 7, TimeSpan.TicksPerDay, maximum, long.MaxValue };
        long[] queries = { 0, 1, 6, 7, maximum / 2, maximum - 1, maximum };
        foreach (long anchor in anchors)
        {
            foreach (long interval in intervals)
            {
                var schedule = new IntervalSchedule(new DateTimeOffset(anchor, TimeSpan.Zero), TimeSpan.FromTicks(interval));
                foreach (long query in queries)
                {
                    foreach (bool inclusive in new[] { false, true })
                    {
                        BigInteger target = (BigInteger)query + (inclusive ? 0 : 1);
                        BigInteger difference = BigInteger.Max(0, target - anchor);
                        BigInteger number = (difference + interval - 1) / interval;
                        BigInteger expected = anchor + number * interval;
                        DateTimeOffset? actual = schedule.Next(new DateTimeOffset(query, TimeSpan.Zero), inclusive);
                        Assert.Equal(expected <= maximum ? (long?)expected : null, actual?.UtcTicks);
                    }
                }
            }
        }
    }
}
