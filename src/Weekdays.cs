using System;

namespace BenScr.Scheduling;

/// <summary>A set of local calendar weekdays. Calendar schedules require at least one defined day.</summary>
[Flags]
public enum Weekdays
{
    /// <summary>Sunday.</summary>
    Sunday = 1,
    /// <summary>Monday.</summary>
    Monday = 2,
    /// <summary>Tuesday.</summary>
    Tuesday = 4,
    /// <summary>Wednesday.</summary>
    Wednesday = 8,
    /// <summary>Thursday.</summary>
    Thursday = 16,
    /// <summary>Friday.</summary>
    Friday = 32,
    /// <summary>Saturday.</summary>
    Saturday = 64,
    /// <summary>Every day, including weekends.</summary>
    EveryDay = 127
}
