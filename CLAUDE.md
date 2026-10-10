# CLAUDE.md

Guidance for Claude Code (and any other contributor) working in this repository.

## What this is

A maintained fork of **Flee** (Fast Lightweight Expression Evaluator), a .NET library that
parses string expressions such as `sqrt(a^2 + b^2)`, type-checks them and compiles them to IL
in a `DynamicMethod`. Upstream is `mparlak/Flee`; this fork branches off at upstream commit
`f3b4fe2` (Flee 2.0.0, March 2022), tagged `upstream-baseline`.

The work follows `doc/Flee fork plan to first release.md` (the plan). Background figures and
the architecture summary are in `doc/flee-project-findings.md`.

## Ground rules

These come from the plan and from the maintainer, and apply to every change.

- **Every code change is explained and documented** in `doc/CHANGE-RATIONALE.md`: one entry
  per change with what, why and how it was verified. Changes with a real choice in them are
  discussed with the maintainer before they are made. Build, test and project-file changes
  count as code changes.
- **Pin behaviour before changing it.** A test records what the library does today, bugs
  included, before any fix.
- **Measure before optimizing.** Compare against the Phase 2 benchmark baseline and report
  measured numbers, or say plainly that a number is an estimate.
- **One kind of change per commit.** Mechanical cleanup, bug fixes and API changes never share
  a commit.
- **Every phase ends green**: build and tests pass, and failing script cases sit on the
  known-failures list.
- **Do not touch the parser** (`src/Flee/Parsing`) before the first release. Parser and grammar
  experiments come afterwards, behind a switch that checks old and new against every script case.
- **Phase 5 (own adjustments) is on hold.** Do not start API changes or extensions.
- **Licence:** LGPL 2.1 or later (`LICENSE`). Keep every existing copyright notice.
- **`README.markdown` is upstream's README and stays untouched.** `README.md` is the fork's own.
- Keep the plan current: tick off finished items and record decisions in its Open decisions table.

Decisions made so far (details in the plan's Open decisions table):

- Package ID and namespace stay `Flee`. Packages go to the maintainer's private feed only, never
  to nuget.org (where `Flee` belongs to upstream).
- The first release is source-compatible with Flee 2.0.0. Any deviation is documented.
- Target frameworks (decided in Phase 3): netstandard2.0, netstandard2.1, net8.0, net10.0.
- `EmitToAssembly` stays as a no-op for source compatibility.
- Nullable reference types are off in `src/Flee`. A file opts in with `#nullable enable` once it
  is annotated; new files start with it. Public API first (Phase 4), the parser last.

## Branches and pull requests

- `develop` is the default branch and the integration branch. `master` mirrors upstream.
- Work on a `claude/*` (or other topic) branch cut from `develop` and open the PR into `develop`.
- Versions come from GitVersion (`GitVersion.yml`, `next-version: 2.6.0`). Do not create tags
  that look like versions unless you mean to release; the baseline tag is deliberately named
  `upstream-baseline`.

## Layout

| Path | Content |
| --- | --- |
| `src/Flee` | The library. Targets netstandard2.0, netstandard2.1, net8.0, net10.0 |
| `src/Flee/Parsing` | Bundled Grammatica parser runtime, the generated parser and `Expression.grammar` |
| `src/Flee/ExpressionElements` | One class per language construct; each emits its own IL |
| `src/Flee/InternalTypes` | `Expression<T>`, IL generator, branch manager, implicit conversions |
| `src/Flee/PublicTypes` | `ExpressionContext`, options, imports, variables, exceptions |
| `src/Flee/CalcEngine` | `CalculationEngine` and `SimpleCalcEngine` |
| `test/Flee.Test` | NUnit tests, run on net8.0 and net10.0 |
| `benchmarks/Flee.Benchmarks` | BenchmarkDotNet project; results in `benchmarks/results` |
| `test/Flee.Test/TestScripts` | Expression script files: `ValidExpressions.txt`, `InvalidExpressions.txt`, `ValidCasts.txt`, `CheckedTests.txt` and others |
| `doc` | Plan, findings, change rationale, upstream issue triage |
| `build` | All build output (git-ignored), see below |

## Build and test

Requires the .NET 10 SDK, and the .NET 8 runtime for the net8.0 test run. The benchmark project
also targets net6.0 (the baseline runtime) and needs the .NET 6 runtime to run there.

```
dotnet tool restore            # once: CycloneDX, used for the SBOM step
dotnet build Flee.sln
dotnet test Flee.sln
```

- Output goes to `build/<project>/<configuration>/` (one subfolder per framework for `Flee`),
  set up in `Directory.Build.props`.
- Every build of `Flee` also copies the assemblies to `build/runtime/Flee/<framework>/`, writes a
  CycloneDX SBOM there, and produces a NuGet package. The package goes to `C:\dev\nuget\` when
  that folder exists, otherwise to `build/nuget/`.
- `/p:SkipSbom=true` skips the SBOM for fast local cycles.
- The build reports about 74 warning lines (each counted once per target framework). Almost all
  come from `src/Flee/Parsing` (two misplaced `[Obsolete]` attributes and their uses), which is
  not touched before the release without the maintainer's OK; the rest are the obsolete
  serialization members of `ExpressionCompileException`, kept for compatibility.

## State of the tests

About 1,860 tests per runtime: the 1,756 script cases (data-driven, `test/Flee.Test/ScriptTests`)
plus fixture tests, many ported from the original VB.NET Flee test project. Known failures are
listed in `test/Flee.Test/TestScripts/KnownFailures.txt` (empty since Phase 4; history in
`doc/known-failures.md`) or marked `[Category("KnownFailure")]` with `[Ignore]`, and reported as
skipped. A listed case that starts passing fails the run, so remove its entry when you fix it.
Bug fixes follow the pattern: commit a test that records the bug (as a known failure), then the
fix that removes the marker. The suite runs under the en-GB culture
(`test/Flee.Test/TestCulture.cs`) because Flee's parser defaults follow the current culture.

`dotnet test` reports success only if the test host did not crash: check for "aborted" in the
output, not just the pass/fail counts.

## Writing style for docs and commit messages

Plain, short English. Commit messages say what changed and why, one kind of change per commit.
