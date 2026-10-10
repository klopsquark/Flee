# Development

How to build, test and change the fork. The repository's `CLAUDE.md` holds the same rules in short
form for anyone, human or AI, who works on it.

## Prerequisites

- .NET 10 SDK.
- .NET 8 runtime, for the net8.0 test run.
- .NET 6 runtime, only to run the benchmarks on the baseline runtime.

## Build and test

```
dotnet tool restore            # once: CycloneDX, used for the SBOM step
dotnet build Flee.sln
dotnet test Flee.sln
```

- All output goes to `build/<project>/<configuration>/`. Each build of `Flee` also copies the
  assemblies to `build/runtime/Flee/<framework>/`, writes a CycloneDX SBOM there and packs a NuGet
  package into `C:\dev\nuget\` (when that folder exists) or `build/nuget/`.
- `-p:SkipSbom=true` skips the SBOM, `-p:GeneratePackageOnBuild=false` the package, for fast local
  cycles.
- Warnings are errors. The only remaining messages are NETSDK1138 for the benchmarks' deliberate
  net6.0 target.
- `dotnet test` only succeeded if the test host did not crash: look for "aborted" in the output,
  not only at the counts.

## Repository layout

| Path | Content |
| --- | --- |
| `src/Flee` | The library ([Internals](Internals) describes the folders) |
| `test/Flee.Test` | NUnit tests, run on net8.0 and net10.0 under the en-GB culture |
| `test/Flee.Test/TestScripts` | The expression script files and `KnownFailures.txt` |
| `benchmarks/Flee.Benchmarks` | BenchmarkDotNet project; results in `benchmarks/results` |
| `doc` | Plan, change rationale, deferred work, language reference, API guide and the other documents |
| `wiki` | The hand-written pages of this wiki |
| `tools/Publish-Wiki.ps1` | Builds this wiki from `wiki/` and `doc/` and pushes it |

## Rules for changes

- **Explain every change** in `doc/CHANGE-RATIONALE.md`: what, why, behaviour, how it was verified,
  whether it was discussed. Build, test and project-file changes count. Changes with a real choice
  in them are discussed with the maintainer first.
- **Pin behaviour before changing it.** A bug fix is two commits: a test that records the bug,
  marked `[Category("KnownFailure")]` and `[Ignore]` (or listed in `KnownFailures.txt`), then the
  fix, which removes the marker. A listed case that starts passing fails the run.
- **Measure before optimizing**, against the baseline in `benchmarks/results/baseline-net6.0`; a
  stage more than 10 % slower is investigated before merging.
- **One kind of change per commit**: cleanup, bug fixes and API changes never share a commit.
- **The parser stays untouched** before the first release, apart from approved exceptions.
- **Phase 5 is on hold**: no API changes or extensions without a decision.
- **Postponed work goes into `doc/deferred.md`** in the same pull request that postpones it.
- Licence LGPL 2.1 or later; keep every copyright notice. `README.markdown` is upstream's README and
  stays untouched; `README.md` is the fork's.

## Branches, versions, CI

- `develop` is the default and integration branch; `master` mirrors upstream. Work on a topic
  branch from `develop` and open the pull request into `develop`.
- Versions come from GitVersion (`GitVersion.yml`, next version 2.6.0). The upstream baseline is the
  tag `upstream-baseline`; do not create version-like tags unless releasing.
- CI (`.github/workflows/ci.yml`) builds and tests on Windows and Ubuntu, in Debug and Release, and
  posts build errors and a test summary on the run.

## Tests

| Kind | Where | What |
| --- | --- | --- |
| Script cases | `ScriptTests` | 1,756 expressions from `TestScripts`: valid results, compile errors, casts, checked arithmetic |
| Documentation | `DocumentationTests` | Every example of the language reference, the API guide, this wiki and the upstream wiki |
| Fixtures | the other folders | Ported VB.NET tests, calculation engine, culture, cloning, regression tests for each fix |

Documentation examples come in two forms. Tables headed `| Expression | Result | Type |` in the
language reference are compiled and compared row by row. C# blocks marked
`<!-- example: Name -->` in the API guide and the wiki are copies of `#region doc:Name` blocks in
`ApiGuideExamples.cs` or `WikiExamples.cs`, where they run with assertions; a test fails when a
copy differs from its region.

## Benchmarks

```
dotnet run -c Release -f net10.0 --project benchmarks/Flee.Benchmarks -- --filter *
```

`StageBenchmarks` runs 15 vectors through parse, compile, compile-and-first-call and evaluate;
`CalculationEngineBenchmarks` and `VariableBenchmarks` cover the engine and variable writes. Run on
a quiet machine; keep results worth comparing in `benchmarks/results/<name>/` with a README naming
commit, machine and runtime. Benchmarks are not part of CI.

## Documentation and this wiki

- The documents in `doc/` are the source for most wiki pages; `wiki/` holds the pages that exist
  only here (Home, Getting Started, Examples, Project Status, Development).
- `wiki/generated-pages.txt` maps each document in `doc/` to its wiki page. Links between documents
  are rewritten to wiki links; links to other repository files point to GitHub.
- Publish with `pwsh tools/Publish-Wiki.ps1 -Push` after the changes are merged. Edit the sources,
  never the wiki directly: the next publish overwrites it.

## Releasing

The first-release checklist in the plan: CI green on every target, known failures empty or
documented, no benchmark regression beyond the threshold, documentation and changelog complete,
package metadata final, package published to the private feed and the release tagged.
