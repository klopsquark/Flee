# Project Status

State on 2026-10-10. The fork follows a plan of eight phases towards a first release
(`doc/Flee fork plan to first release.md` in the repository). Every code change is recorded with
its reason and verification in `doc/CHANGE-RATIONALE.md` (entries R-001 to R-057); everything
postponed is in [Deferred Work](Deferred-Work).

## In short

| | |
| --- | --- |
| Version | 2.6.0, prepared, not yet released |
| Compatibility | Source-compatible with Flee 2.0.0; behaviour changes are bug fixes, listed in [Migration Guide](Migration-Guide) |
| Targets | netstandard2.0, netstandard2.1, net8.0, net10.0 |
| Package | ID `Flee`, private feed only |
| Tests | About 2,050 per runtime (net8.0 and net10.0), all passing, none skipped |
| Known failures | 0 (150 when the fork started) |
| CI | GitHub Actions, Windows and Ubuntu, Debug and Release |

## Phases

| Phase | Content | State |
| --- | --- | --- |
| 0 | Fork setup: baseline tag, licence, package identity, compatibility stance | Done |
| 1 | Build and tests: the 1,756 script cases recovered as NUnit tests, original VB.NET tests ported, CI | Done |
| 2 | Benchmark baseline with BenchmarkDotNet (15 vectors, four stages) | Done |
| 3 | SDK modernization: targets, packages, package metadata, `.editorconfig` | Done |
| 4 | Bug fixes and mechanical cleanup, warnings as errors, nullable annotations on the public API | Done |
| 5 | Own API changes and extensions | On hold by decision |
| 6 | Documentation: language reference, API guide, XML comments, internals, limitations, migration notes, changelog, this wiki | Done |
| 7 | Performance optimization (stretch goal) | Open: skip or time-box, to be decided |

## What was done

**Build and packaging**

- Builds with the .NET 10 SDK; targets netstandard2.0, netstandard2.1, net8.0 and net10.0
  (net5.0 and net6.0 dropped).
- Package with SPDX licence expression, README, XML documentation, symbols and Source Link;
  versions from GitVersion; a CycloneDX SBOM next to every build.
- Warnings are errors; `.editorconfig` describes the code style.

**Tests**

- The 1,756 expression script cases that upstream carried without a test runner run as data-driven
  tests again. 150 of them failed at first; all were fixed or their expectation corrected
  (`doc/known-failures.md`).
- The original VB.NET tests were ported, and the documentation examples (language reference, API
  guide, this wiki, the upstream wiki) run as tests.

**Bugs fixed** (each first pinned by a test)

- Unsigned 32- and 64-bit constants above the signed maximum did not compile.
- `in` did not accept non-generic collections.
- Out-of-range real literals became infinity on .NET Core.
- `GetType()` and other calls on value-type members crashed the process.
- Member access on `DateTime`/`TimeSpan` calculation-engine atoms failed (upstream #64, #111).
- Unknown names in the calculation engine threw `ArgumentNullException`; a failed `Add` left the
  name behind; syntax errors and unknown names in batches leaked internal exceptions.
- Upper-case keywords did not parse under a Turkish culture (upstream #105).
- `cast(x, char)` did not convert.
- Debug builds of Flee failed on .NET 8 and later (upstream #110, #82).
- `DefineVariable` variables threw when read before they had a value.
- A cloned context shared options and imports with the original; `RecreateParser` kept an old
  parser for the calculation engines.
- `SimpleCalcEngine` lost its variables when an add failed.

**Performance**

- On .NET 10 the fork parses, compiles and evaluates about 45 % faster than Flee 2.0.0 on .NET 6.
- A .NET 10 regression found on the way, where the JIT inlined Flee's helpers into every expression,
  is fixed: the first evaluation of an expression that reads variables is about 20 times faster,
  loading a calculation engine about 30 times.

**Code**

- Field names follow C# conventions, one type per file, boolean logic simplified, impossible states
  throw clear exceptions, unused code removed, nullable annotations on the public API.
- The bundled parser is unchanged apart from two approved exceptions (the Turkish fix and two
  malformed doc comments).

## What is open

**Before the first release**

- Decide about Phase 7 (performance): skip it, or set a time box.
- Release checklist: final benchmark comparison with the baseline, tag the release, publish the
  package to the private feed.

**After the first release** (details in [Deferred Work](Deferred-Work))

- Parser: non-ASCII identifiers (D-01), parser experiments behind a switch that checks old against
  new on every script case (D-02), cleanup (D-03), typed collections (D-04).
- Nullable annotations for the internals, then project-wide (D-05).
- `EmitToAssembly` on net10.0 with `PersistedAssemblyBuilder`, to inspect generated IL (D-06).
- Remaining .NET 10 performance gaps (D-07, D-08).

**Phase 5, on hold** (API changes that need a decision)

- Rejecting a variable value of the wrong type (D-09), user-defined `&`/`|` for `and`/`or` (D-10),
  thread-safe owners (D-11), C#-like overload ambiguity (D-12, D-13), a public API baseline file
  (D-14), setters that accept null (D-16), and smaller items (D-15, D-17 to D-19, D-30 to D-32).

**Housekeeping**

- NUnit 4 (D-20), a benchmark vector with real-world expressions (D-21), fixes from a local Flee
  copy if supplied (D-22), remaining IDE naming hints (D-24).
