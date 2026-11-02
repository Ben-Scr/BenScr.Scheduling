# Provenance

This repository contains a new scheduling implementation by Ben Schneider. It does not incorporate source from legacy scheduling repositories, private projects or third-party scheduling libraries. The target GitHub repository and NuGet package ID were absent when checked on 2026-11-02.

The build, package audit and verification scripts follow the existing public BenScr library structure. Runtime dependencies: none. Test-only dependency: xunit.v3 4.0.2 and its locked dependency graph.

The unchanged user-supplied icon is `branding/BenScr_Icon_Master.png`: PNG RGBA, 1254 × 1254, 795819 bytes, SHA-256 `a5dcabceb766e13c1840a95130ee98a458e12e3713fd9b07f3ceae75a1f5b993`. The same bytes are embedded as `icon.png`; no resize or graphic modification is performed.

`LICENSE.txt` preserves the existing Ben Schneider custom attribution license bytes. SHA-256: `f6600e285e17c48188a65745d7595807e1755c72201e8eef201a9174fca18573`. Package audit checks this hash. The license is not represented as MIT.

Time-zone interpretation uses the .NET platform APIs. Local calendar values have `DateTimeKind.Unspecified`; ambiguous offsets come from the selected zone. References:

- [TimeZoneInfo.IsInvalidTime](https://learn.microsoft.com/dotnet/api/system.timezoneinfo.isinvalidtime)
- [TimeZoneInfo.GetAmbiguousTimeOffsets](https://learn.microsoft.com/dotnet/api/system.timezoneinfo.getambiguoustimeoffsets)
- [TimeZoneInfo.BaseUtcOffset](https://learn.microsoft.com/dotnet/api/system.timezoneinfo.baseutcoffset)

The rules associated with a system time-zone identifier depend on the installed platform database. Tests and the runnable consumer use explicit custom zones to avoid assuming a particular system database revision.
