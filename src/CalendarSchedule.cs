using System;

namespace BenScr.Scheduling;

/// <summary>An immutable wall-clock schedule on selected weekdays in an explicit time zone.</summary>
/// <remarks>Thread-safe. Invalid local times in forward offset gaps are always skipped, never shifted.
/// Ambiguous times follow the selected policy. Results depend on the supplied zone's adjustment rules;
/// system zone databases can differ across machines and change over time. No clock is read.</remarks>
public sealed class CalendarSchedule
{
    private const long MaximumOffsetTicks = 14 * TimeSpan.TicksPerHour;

    /// <summary>Creates a daily or weekly schedule with explicit ambiguity handling.</summary>
    /// <param name="time">The local wall-clock time, at 100-nanosecond resolution.</param>
    /// <param name="timeZone">The immutable zone used for all calendar calculations; must not be null.</param>
    /// <param name="days">A nonempty set containing only defined weekday bits.</param>
    /// <param name="ambiguousTimePolicy">The policy for repeated local times; undefined values are rejected.</param>
    public CalendarSchedule(TimeOnly time, TimeZoneInfo timeZone, Weekdays days = Weekdays.EveryDay,
        AmbiguousTimePolicy ambiguousTimePolicy = AmbiguousTimePolicy.Earlier)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        if (days == 0 || (days & ~Weekdays.EveryDay) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Select at least one defined weekday.");
        }
        if (!Enum.IsDefined(ambiguousTimePolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(ambiguousTimePolicy));
        }
        Time = time;
        TimeZone = timeZone;
        Days = days;
        AmbiguousTimePolicy = ambiguousTimePolicy;
    }

    /// <summary>Gets the local wall-clock occurrence time.</summary>
    public TimeOnly Time { get; }

    /// <summary>Gets the explicit time zone. The system local zone is never implicitly selected.</summary>
    public TimeZoneInfo TimeZone { get; }

    /// <summary>Gets the selected local calendar weekdays.</summary>
    public Weekdays Days { get; }

    /// <summary>Gets the policy for repeated local times.</summary>
    public AmbiguousTimePolicy AmbiguousTimePolicy { get; }

    /// <summary>Returns the earliest eligible UTC occurrence whose local date is at most the supplied horizon.</summary>
    /// <param name="after">The query instant. Its displayed offset is ignored.</param>
    /// <param name="throughDate">The inclusive last local calendar date to examine, not a UTC deadline.</param>
    /// <param name="inclusive">Whether an occurrence exactly at the query instant may be returned.</param>
    /// <returns>A zero-offset instant, or null when the horizon contains no representable eligible occurrence.</returns>
    /// <remarks>A horizon containing no eligible date is an empty search, not an error.
    /// Out-of-range UTC conversions are omitted instead of clamped. Work is bounded by the candidate
    /// dates through the horizon (at most the DateOnly range); normal searches stop shortly after a match.</remarks>
    public DateTimeOffset? Next(DateTimeOffset after, DateOnly throughDate, bool inclusive = false)
    {
        long afterTicks = after.UtcTicks;
        // All TimeZoneInfo UTC offsets are within +/-14 hours. Begin before any possible
        // eligible local date, including date-crossing backward transitions.
        long earliestLocalTicks = Math.Max(0, afterTicks - MaximumOffsetTicks);
        int startDay = DateOnly.FromDateTime(new DateTime(earliestLocalTicks)).DayNumber;
        long? best = null;
        for (int day = startDay; day <= throughDate.DayNumber; day++)
        {
            DateOnly date = DateOnly.FromDayNumber(day);
            DateTime local = date.ToDateTime(Time, DateTimeKind.Unspecified);
            // Even the largest possible offset cannot place this or a subsequent date before best.
            if (best.HasValue && local.Ticks - MaximumOffsetTicks >= best.Value)
            {
                break;
            }
            if ((Days & (Weekdays)(1 << (int)date.DayOfWeek)) == 0 || TimeZone.IsInvalidTime(local))
            {
                continue;
            }
            if (!TimeZone.IsAmbiguousTime(local))
            {
                Consider(local, TimeZone.GetUtcOffset(local), afterTicks, inclusive, ref best);
                continue;
            }
            if (AmbiguousTimePolicy == AmbiguousTimePolicy.Skip)
            {
                continue;
            }
            TimeSpan[] offsets = TimeZone.GetAmbiguousTimeOffsets(local);
            TimeSpan earlier = offsets[0] > offsets[1] ? offsets[0] : offsets[1];
            TimeSpan later = offsets[0] < offsets[1] ? offsets[0] : offsets[1];
            if (AmbiguousTimePolicy != AmbiguousTimePolicy.Later)
            {
                Consider(local, earlier, afterTicks, inclusive, ref best);
            }
            if (AmbiguousTimePolicy != AmbiguousTimePolicy.Earlier)
            {
                Consider(local, later, afterTicks, inclusive, ref best);
            }
        }
        return best.HasValue ? new DateTimeOffset(best.Value, TimeSpan.Zero) : null;
    }

    private static void Consider(DateTime local, TimeSpan offset, long afterTicks, bool inclusive, ref long? best)
    {
        long utcTicks = local.Ticks - offset.Ticks;
        if (utcTicks < 0 || utcTicks > DateTime.MaxValue.Ticks || utcTicks < afterTicks ||
            (!inclusive && utcTicks == afterTicks))
        {
            return;
        }
        if (!best.HasValue || utcTicks < best.Value)
        {
            best = utcTicks;
        }
    }
}
