# Migration

Version 1.0.0 is the first release of this independent library. No previous scheduling package, public API, stored format or source repository is replaced.

Use `IntervalSchedule` for fixed elapsed durations from an anchor. Use `CalendarSchedule` for local wall-clock occurrences on specified weekdays. Replacing a timer with calendar lookup requires the caller to retain the last executed instant and choose explicit inclusive/exclusive behavior, a local horizon and an ambiguity policy.

Calendar lookup skips nonexistent local times. Repeated local times default to the earlier UTC instant; choose `Both` if both executions are wanted. The library calculates times and does not guarantee job execution or exactly-once delivery.

The package, assembly, namespace, project and repository names are all `BenScr.Scheduling` without a language suffix.
