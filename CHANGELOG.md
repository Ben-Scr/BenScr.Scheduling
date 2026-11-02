# Changelog

## 1.0.0

- Add UTC-anchored interval progression with positive tick durations and overflow-safe next-occurrence calculation.
- Add daily and weekly calendar schedules with an explicit time zone and local-date horizon.
- Add earlier, later, both and skip policies for ambiguous local times; skip invalid local times in forward gaps.
- Preserve exact inclusive/exclusive boundaries, UTC output and range omission rather than clamping.
- Provide examples, reviewed API inventory, contract/reference tests and package-consumer verification for .NET 10.
