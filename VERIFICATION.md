# Verification

The test suite contains 42 cases with no skipped tests. It covers:

- interval anchors, tick precision, offset normalization, inclusive/exclusive boundaries and positive-duration validation;
- 350 combinations compared to an independent arbitrary-precision interval reference, including the full DateTime range and TimeSpan.MaxValue;
- every weekday, masks, leap dates, local-date horizons and offset extremes ±14 hours;
- fixed custom-zone forward gaps, backward folds, all four ambiguity policies, 30-minute and negative daylight deltas;
- both fold occurrences retrieved through successive queries;
- an independent UTC-minute scan across spring and autumn transitions (8,640 candidate instants);
- range exhaustion without clamping and concurrent reads of an immutable schedule.

Tests use explicit query instants and custom time zones. They do not read the system clock or sleep. The runnable example repeats representative assertions through either a project reference or a fresh NuGet PackageReference.

Local evidence is written under ignored `artifacts/verification-<id>/`; the latest successful path is `artifacts/latest-verification.txt`. Each release manifest records the actual commit, package hashes and test counters. CI uses Windows and Ubuntu; a configured workflow is not itself a claim that a particular remote run passed.

The final archive audit requires the exact expected files, original license/icon hashes, .NET 10 DLL and XML, matching Portable PDB, source checksums, repository commit and Source Link URL inventory. Package consumers restore into an empty temporary cache, disallow project references and verify the installed nuspec commit.

The shared master icon has already been visually reviewed for the BenScr family on light and dark surfaces at 32 and 128 pixels. This package retains the same bytes. The published gallery rendering is checked after NuGet indexing as part of release verification.
