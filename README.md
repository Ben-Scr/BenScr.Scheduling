# BenScr.Scheduling

Pure interval and calendar schedule calculations for .NET 10. Compute the next occurrence without a background service, timer, system-clock dependency or runtime package dependencies.

Created by Ben Schneider — https://github.com/Ben-Scr

```sh
dotnet add package BenScr.Scheduling --version 1.0.0
```

```csharp
using BenScr.Scheduling;

var anchor = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
var everyQuarterHour = new IntervalSchedule(anchor, TimeSpan.FromMinutes(15));
DateTimeOffset? nextInterval = everyQuarterHour.Next(anchor); // 00:15 UTC

var weekdays = Weekdays.Monday | Weekdays.Tuesday | Weekdays.Wednesday
    | Weekdays.Thursday | Weekdays.Friday;
var morning = new CalendarSchedule(new TimeOnly(9, 0), TimeZoneInfo.Utc, weekdays);
DateTimeOffset? nextMorning = morning.Next(anchor, new DateOnly(2026, 1, 31));
```

## Contracts

- Every result is a UTC `DateTimeOffset` with zero offset. Query offsets describe the same instant and do not change results.
- `Next` is exclusive by default. Set `inclusive: true` to permit an occurrence exactly at the query instant. Resolution is one tick (100 ns).
- `IntervalSchedule` starts at its UTC-normalized anchor and repeats after a strictly positive elapsed duration. It never schedules before the anchor. `null` means no further occurrence fits the `DateTimeOffset` range. Arithmetic does not enumerate missed occurrences.
- A 24-hour interval measures elapsed time. Use a calendar schedule to preserve a local wall-clock time across changes in UTC offset.
- `CalendarSchedule` selects a local time on a nonempty weekday mask in an explicit `TimeZoneInfo`. `Weekdays.EveryDay` gives a daily schedule. Instances are immutable and safe for concurrent reads.
- Calendar `throughDate` is an **inclusive local calendar date**, not a UTC deadline. It makes the search horizon explicit. A past or exhausted horizon returns `null`; it is not an error. An occurrence on that local date can fall on another UTC date.
- Local times inside forward offset gaps are always skipped. The library does not silently move a 02:30 appointment to 03:00 or 03:30.
- Repeated local times follow `AmbiguousTimePolicy`: `Earlier` (default), `Later`, `Both`, or `Skip`. Earlier/later refer to UTC ordering, including negative and non-hour daylight deltas. With `Both`, query exclusively after the first result to retrieve the second occurrence.
- Local occurrences whose calculated UTC value is outside years 1–9999 are omitted, never clamped. Weekday filtering uses the occurrence's local date.
- Undefined weekday bits, an empty mask, a null zone and an undefined ambiguity policy are rejected at construction.

```csharp
// Supply the zone appropriate for your application. System zone identifiers
// and historical rules depend on the platform's installed zone database.
TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
var daily = new CalendarSchedule(new TimeOnly(2, 30), zone,
    ambiguousTimePolicy: AmbiguousTimePolicy.Both);
DateTimeOffset? occurrence = daily.Next(anchor, new DateOnly(2026, 12, 31));
```

The library uses the rules on the supplied zone and does not refresh or serialize a zone database. For repeatable cross-platform calculations, construct and retain an explicit custom zone with the intended adjustment rules. See [the complete consumer example](examples/Program.cs), which uses such a zone and runs assertions.

## Cost and scope

Interval lookup takes constant arithmetic work. Calendar lookup examines dates from a conservative lower bound through the supplied horizon, stopping once later dates cannot produce an earlier UTC result. Worst-case work is linear in that date range (at most 3,652,059 dates), with constant retained storage. Typical daily/weekly searches inspect only a few dates. Calendar queries use up to two offsets per ambiguous local occurrence.

This package does not execute jobs, persist schedules, parse cron expressions, apply holiday calendars or model leap seconds. Execution, retries and missed-job handling belong to the calling application. No native, trimming, AOT or other target-framework support is implied beyond the documented .NET 10 matrix.

## Build and verification

Install .NET SDK **10.0.303**, PowerShell 7 and Git. Build directly from project files; solution files are optional and ignored.

```powershell
dotnet test --project tests/BenScr.Scheduling.Tests.csproj -c Release
dotnet run --project examples/Example.csproj -c Release
pwsh -File tools/Verify.ps1
```

`Verify.ps1` requires a clean committed checkout. It checks locked restore, build, formatting, complete test execution, the reviewed public API, the example, exact package contents, documentation, original license/icon, Portable-PDB identity, Source Link and an isolated NuGet consumer with an empty cache. CI runs the same checks on Windows and Ubuntu. See [VERIFICATION.md](VERIFICATION.md) and [RELEASE.md](RELEASE.md).

The package embeds the unchanged `BenScr_Icon_Master.png`. License terms are in [LICENSE.txt](LICENSE.txt); this is a custom attribution license, not an MIT declaration. See [provenance](docs/PROVENANCE.md) and [migration notes](MIGRATION.md).
