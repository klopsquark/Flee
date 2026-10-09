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
- Target frameworks stay as they are (net6.0, net5.0, netstandard2.1, netstandard2.0) unless the
  maintainer decides otherwise in Phase 3.

## Branches and pull requests

- `develop` is the default branch and the integration branch. `master` mirrors upstream.
- Work on a `claude/*` (or other topic) branch cut from `develop` and open the PR into `develop`.
- Versions come from GitVersion (`GitVersion.yml`, `next-version: 2.6.0`). Do not create tags
  that look like versions unless you mean to release; the baseline tag is deliberately named
  `upstream-baseline`.

## Layout

| Path | Content |
| --- | --- |
| `src/Flee` | The library. Targets net6.0, net5.0, netstandard2.1, netstandard2.0 |
| `src/Flee/Parsing` | Bundled Grammatica parser runtime, the generated parser and `Expression.grammar` |
| `src/Flee/ExpressionElements` | One class per language construct; each emits its own IL |
| `src/Flee/InternalTypes` | `Expression<T>`, IL generator, branch manager, implicit conversions |
| `src/Flee/PublicTypes` | `ExpressionContext`, options, imports, variables, exceptions |
| `src/Flee/CalcEngine` | `CalculationEngine` and `SimpleCalcEngine` |
| `test/Flee.Test` | NUnit tests (net6.0) |
| `test/Flee.Test/TestScripts` | Expression script files: `ValidExpressions.txt`, `InvalidExpressions.txt`, `ValidCasts.txt`, `CheckedTests.txt` and others |
| `doc` | Plan, findings, change rationale, upstream issue triage |
| `build` | All build output (git-ignored), see below |

## Build and test

Requires a .NET SDK that can build net6.0 and the .NET 6 runtime for the tests
(the .NET 10 SDK works).

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
- The build currently reports about 2,400 warnings (mostly nullable warnings over code that was
  never annotated). Do not fix them in passing; that is Phase 3 work.

## State of the tests

As of the start of Phase 1 the test project has 48 test methods; 8 of them fail on the unchanged
code and the script files are not read by any test. Phase 1 turns the scripts into data-driven
tests and records every failing case on a known-failures list rather than fixing it.

## Writing style for docs and commit messages

Plain, short English. Commit messages say what changed and why, one kind of change per commit.
