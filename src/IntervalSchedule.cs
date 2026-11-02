using System;

namespace BenScr.Scheduling;

/// <summary>An immutable UTC-anchored arithmetic progression of instants, starting at its anchor.</summary>
/// <remarks>Thread-safe. No clock is read and no work is executed. Intervals measure elapsed time,
/// so a 24-hour interval does not preserve a local wall-clock time across offset changes.</remarks>
public sealed class IntervalSchedule
{
    /// <summary>Creates a schedule. The interval must be positive; the anchor is normalized to UTC.</summary>
    public IntervalSchedule(DateTimeOffset anchor, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "The interval must be positive.");
        }
        Anchor = anchor.ToUniversalTime();
        Interval = interval;
    }

    /// <summary>Gets the first scheduled instant, with zero offset.</summary>
    public DateTimeOffset Anchor { get; }

    /// <summary>Gets the fixed positive duration between instants, at 100-nanosecond resolution.</summary>
    public TimeSpan Interval { get; }

    /// <summary>Returns the first scheduled UTC instant after the query, or null if none is representable.</summary>
    /// <param name="after">The query instant; its displayed offset does not affect the result.</param>
    /// <param name="inclusive">Whether an occurrence exactly at the query instant may be returned.</param>
    /// <returns>A zero-offset occurrence, or null after the last representable occurrence.</returns>
    public DateTimeOffset? Next(DateTimeOffset after, bool inclusive = false)
    {
        long anchorTicks = Anchor.UtcTicks;
        long afterTicks = after.UtcTicks;
        if (afterTicks < anchorTicks)
        {
            return Anchor;
        }
        long elapsed = afterTicks - anchorTicks;
        long number = elapsed / Interval.Ticks;
        if (!inclusive || elapsed % Interval.Ticks != 0)
        {
            number++;
        }
        long remaining = DateTime.MaxValue.Ticks - anchorTicks;
        if (number > remaining / Interval.Ticks)
        {
            return null;
        }
        return new DateTimeOffset(anchorTicks + number * Interval.Ticks, TimeSpan.Zero);
    }
}
