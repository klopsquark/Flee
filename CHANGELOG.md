# Changelog

Changes of this fork relative to upstream Flee 2.0.0 (`mparlak/Flee`, commit `f3b4fe2`). Upgrade
notes: [doc/migration-from-flee-2.0.md](doc/migration-from-flee-2.0.md). The reason and the
verification for each change: [doc/CHANGE-RATIONALE.md](doc/CHANGE-RATIONALE.md) (ids in
brackets).

## 2.6.0 (unreleased)

### Added

- Targets net8.0 and net10.0 next to netstandard2.0 and netstandard2.1 [R-011].
- Nullable reference type annotations on the public API [R-038].
- XML documentation (`Flee.xml`) for the public API, so IntelliSense shows it [R-048, R-049].
- Symbol package and Source Link [R-014].
- Documentation: language reference, API guide, architecture, limitations, migration notes.

### Changed

- Package metadata: SPDX licence expression `LGPL-2.1-or-later`, the fork's README [R-014].
- The package no longer adds `Resources/DocComments.xml` to consuming projects [R-049].
- Versions come from GitVersion, starting at 2.6.0 [R-001].
- net5.0 and net6.0 builds removed; those runtimes use the netstandard2.1 build [R-011].
- `System.Reflection.Emit` packages are only referenced for netstandard2.0 [R-012].
- Faster first evaluation on .NET 10 for expressions that read variables, call on-demand functions
  or use calculation-engine atoms [R-019].

### Deprecated

- `ExpressionOptions.EmitToAssembly` has no effect and is marked obsolete [R-015].

### Fixed

- `uint` and `ulong` constants above the signed maximum did not compile [R-021].
- `in` did not accept non-generic `IList` and `IDictionary` collections [R-022].
- Out-of-range real literals compiled to infinity on .NET Core instead of failing [R-023].
- `GetType()` on value-type members crashed the process [R-024].
- Member access on value-type calculation-engine atoms failed (upstream #64, #111) [R-026].
- Three element types had no name in error messages [R-027].
- Unknown names in calculation-engine expressions threw `ArgumentNullException` instead of a
  compile error [R-039].
- A failed `CalculationEngine.Add` left the atom name behind [R-040].
- Comparing `NamespaceImport` objects not attached to a context threw [R-041].
- Upper-case keywords containing `I` did not parse under the Turkish culture (upstream #105)
  [R-042].
- `cast(x, char)` did not convert, wrap or check for overflow [R-045].
- Debug builds of Flee failed on every compile on .NET 8 and later (upstream #110, #82) [R-010].
- Impossible internal states now throw a clear exception instead of emitting invalid IL [R-035].
