# Change rationale

Every change this fork makes to code, build or tests, relative to upstream `mparlak/Flee` at
`f3b4fe2` (tag `upstream-baseline`), with the reason for it and how it was verified.

This file is for maintainers and reviewers. The user-facing changelog and the migration notes
come later (plan, Phase 6) and are written from the entries here.

## How to add an entry

- One entry per change. A change is one commit, or a small series of commits of the same kind.
- Number entries consecutively (`R-001`, `R-002`, ...) and never renumber. Newest at the bottom.
- Add the entry in the same pull request as the change, and add a row to the index.
- **Kind** is one of: `build`, `test`, `cleanup` (no behaviour change), `fix` (behaviour change
  that corrects a bug), `api` (public surface changes), `perf`, `docs`.
- **Behaviour** says whether anything a library user can observe changes: `none`, or what changes.
- **Discussed** records the maintainer's decision when there was a real choice, or `not needed`.
- Documentation-only changes (README, plan, this file) do not need an entry.

Entry template:

```
### R-000: Short title

- **Kind / phase:** build / Phase 1
- **Commits:** abc1234
- **What:** What changed, in a few sentences.
- **Why:** The reason, and the alternatives that were rejected.
- **Behaviour:** none
- **Verified:** How: build, test run (counts), benchmark comparison, manual check.
- **Discussed:** Decision and date, or "not needed".
```

## Index

| Id | Title | Kind | Phase |
| --- | --- | --- | --- |
| R-001 | Shared build layout, packaging and versioning | build | before Phase 0 |
| R-002 | Licence file | build | Phase 0 |
| R-003 | Expression scripts run as data-driven tests | test | Phase 1 |
| R-004 | Test suite runs under en-GB; culture behaviour pinned | test | Phase 1 |
| R-005 | LongScriptTests read their script files | test | Phase 1 |
| R-006 | Timing category and CI workflow | build, test | Phase 1 |
| R-007 | Flee internals visible to the benchmark project | build | Phase 2 |
| R-008 | Benchmark project; timing tests moved out of the test project | build, test | Phase 2 |
| R-009 | Original individual and calc-engine tests ported | test | Phase 1 |
| R-010 | IL length check reads ILGenerator.ILOffset | fix | Phase 3 |
| R-011 | Target frameworks netstandard2.0/2.1, net8.0, net10.0; tests on net8.0 and net10.0 | build, test | Phase 3 |
| R-012 | Reflection.Emit packages only for netstandard2.0 | build | Phase 3 |
| R-013 | Test packages updated | test | Phase 3 |
| R-014 | Package metadata: licence expression, readme, symbols | build | Phase 3 |
| R-015 | EmitToAssembly is a no-op and obsolete | api | Phase 3 |
| R-016 | Benchmarks run on net6.0, net8.0 and net10.0 | build | Phase 3 |
| R-017 | Nullable reference types off in the library | build | Phase 3 |
| R-018 | .editorconfig describing the existing style | build | Phase 3 |
| R-019 | Generated code no longer inlines Flee's helpers (.NET 10 first-call cost) | perf, fix | Phase 4 |
| R-020 | Expected reasons in InvalidExpressions.txt updated | test | Phase 4 |

## Entries

### R-001: Shared build layout, packaging and versioning

Recorded after the fact: these commits landed on `develop` before this file existed.

- **Kind / phase:** build / before Phase 0
- **Commits:** b12dc4c, 5b1b14f, e9d250b, f04c2a5, 40d041c, 3108048, 2b58e2f (merged in PR #1)
- **What:**
  - `Directory.Build.props` sets `LangVersion latest` for every project, defaults
    `Configuration` to Debug, and sends all output to `build/<project>/<configuration>/`
    (Flee keeps one subfolder per target framework).
  - NuGet packaging is opt-in per project; only `Flee` packs. Packages go to `C:\dev\nuget\`
    when it exists, else `build/nuget/`.
  - Shared package metadata (authors, company, copyright, repository URL) lives in
    `Directory.Build.props`; the project and repository URLs point at the fork.
  - Every build copies the Flee assemblies to `build/runtime/Flee/<framework>/` and writes a
    CycloneDX SBOM there (tool pinned in `.config/dotnet-tools.json`).
  - Versions come from GitVersion (`GitVersion.MsBuild` 6.4.0, `next-version: 2.6.0`) instead of
    the fixed 2.0.0.
- **Why:** Same layout as the maintainer's other repositories; SBOM for Cyber Resilience Act
  readiness; 2.6.0 keeps the fork's packages apart from Flee 2.0.0 on nuget.org and from a 2.5.1
  already in the local feed.
- **Behaviour:** none in the library. Assembly and package versions change from 2.0.0 to
  GitVersion's 2.6.0 series.
- **Verified:** Solution builds (0 errors); the test run is unchanged at 40 of 48 passing.
- **Discussed:** Requested by the maintainer (props file), October 2026.

### R-002: Licence file

- **Kind / phase:** build / Phase 0
- **Commits:** in the Phase 0 pull request
- **What:** Adds `LICENSE` with the unmodified text of the GNU LGPL version 2.1, as published at
  gnu.org.
- **Why:** Upstream states "LGPL 2.1 or (at your option) any later version" in the grammar
  header and the README, but ships no licence file. The licence itself does not change.
- **Behaviour:** none.
- **Verified:** Text compared with gnu.org's `lgpl-2.1.txt` (downloaded copy, 501 lines).
- **Discussed:** Licence kept as LGPL 2.1 or later, decided by the maintainer.

### R-003: Expression scripts run as data-driven tests

- **Kind / phase:** test / Phase 1
- **Commits:** a536484
- **What:** The 1,756 cases in `ValidExpressions.txt`, `InvalidExpressions.txt`, `ValidCasts.txt`
  and `CheckedTests.txt` run as one NUnit test case each (`test/Flee.Test/ScriptTests`). The
  expression owner and helper types they need are ported to C# from the original VB.NET Flee test
  project (`TestTypes.vb`, `BulkTests.vb`, `Core.vb`, LGPL 2.1 or later), preserved in
  `george-playstudiosasia/PlayStudios.Flee`; its script files are identical to ours. Cases that
  fail today are listed in `TestScripts/KnownFailures.txt` with a category and reported as
  ignored; a listed case that passes, or an entry whose line no longer matches, fails the run.
- **Why:** No test read the scripts, so most of the language had no regression protection. The
  owner fixture was missing from this repository; porting the original avoids guessing it from
  the scripts. Deviations from the original harness, each deliberate:
  - Each case gets fresh contexts and a fresh owner, instead of contexts shared across a file,
    so cases cannot influence each other.
  - `InvalidExpressions` also checks the expected `CompileExceptionReason`. The original parsed it
    and never compared it; checking it pins today's reasons (62 differ, see
    `doc/known-failures.md`).
  - `System.AppDomainInitializer` does not exist on .NET Core; the test project declares a
    delegate of the same name and signature, and expected results naming it map to that type.
  - The three cases that crash the process are listed as `crash` and not run.
  - The two `debug-il-length` cases count as known failures only in Debug builds.
- **Behaviour:** none (test only).
- **Verified:** net6.0 Debug: 1,606 pass, 150 known failures, 0 fail. Release: 1,608 and 148.
- **Discussed:** not needed (plan item). The known-failures list itself is for Phase 4.

### R-004: Test suite runs under en-GB; culture behaviour pinned

- **Kind / phase:** test / Phase 1
- **Commits:** 0f98d28, 4837bae
- **What:** `[assembly: SetCulture("en-GB")]` in `test/Flee.Test/TestCulture.cs`. New
  `CultureTests` record the parser defaults and behaviour under de-DE, en-GB, the invariant
  culture and tr-TR.
- **Why:** Flee takes its decimal separator, argument separator and date format from the
  current culture. The existing tests assume '.' and ','; all 8 that failed on the maintainer's
  German machine failed for that reason alone. en-GB rather than en-CA (the original harness's
  culture) because en-CA has used yyyy-MM-dd dates since .NET 5. The culture behaviour itself is
  a feature worth protecting, and the Turkish case is a known bug (upstream #105) that Phase 4
  should flip.
- **Behaviour:** none (test only).
- **Verified:** all 48 existing tests pass under en-GB on a de-DE machine; the 5 culture tests
  pass.
- **Discussed:** not needed.

### R-005: LongScriptTests read their script files

- **Kind / phase:** test / Phase 1
- **Commits:** f7eeb76
- **What:** `LongScriptWithManyFunctions` and `FailingLongScriptWithManyFunctions` load their
  `.js` script files instead of inline copies; commented-out loading code is removed.
- **Why:** The plan lists the commented-out file loading as cleanup. The inline copies differed
  from the files only in whitespace, so the files are now the single source.
- **Behaviour:** none (test only).
- **Verified:** both tests pass.
- **Discussed:** not needed.

### R-006: Timing category and CI workflow

- **Kind / phase:** build, test / Phase 1
- **Commits:** aeeee09, 0586e7e
- **What:** The `Benchmarks` fixture (two wall-clock assertions) gets `[Category("Timing")]`.
  `.github/workflows/ci.yml` builds and tests on Windows and Linux, Debug and Release, without
  the Timing category, and reports test counts and line coverage in the job summary.
- **Why:** The plan asks for CI on both platforms and a coverage figure. Timing assertions fail
  under coverage instrumentation (ProfileCompilationTime took 3.6 s against a 2 s limit) and on
  shared runners; Phase 2 moves them to the benchmark project.
- **Behaviour:** none.
- **Verified:** locally on Windows: 1,657 pass, 150 known failures, 0 fail with the CI filter
  and coverage; line coverage 66.8 % (8,553 of 12,797 lines). The workflow itself has not run
  yet; the first push will show the Linux result.
- **Discussed:** not needed.

### R-007: Flee internals visible to the benchmark project

- **Kind / phase:** build / Phase 2
- **Commits:** 965f5f9
- **What:** `<InternalsVisibleTo Include="Flee.Benchmarks" />` in `src/Flee/Flee.csproj`.
- **Why:** The plan asks for the parse stage to be measured on its own. The only way into it is
  `ExpressionContext.Parse` plus the setup `Expression<T>` does before calling it, all internal.
  The alternative, a public parse API, would be an API change and belongs to Phase 5 at the
  earliest.
- **Behaviour:** none. The attribute only grants access to an assembly named Flee.Benchmarks;
  Flee is not strong-named, so it does not restrict anything either.
- **Verified:** solution builds; tests unchanged.
- **Discussed:** not needed (plan item).

### R-008: Benchmark project; timing tests moved out of the test project

- **Kind / phase:** build, test / Phase 2
- **Commits:** 9673ced, 966030e, and the BenchmarkDotNet 0.15.8 update
- **What:** New `benchmarks/Flee.Benchmarks` (BenchmarkDotNet 0.15.8, net6.0) with 15 vectors
  through parse, compile and evaluate, a calculation-engine and a variable-write benchmark.
  `test/Flee.Test/ExpressionTests/Benchmarks.cs` is deleted; its two workloads live on as
  `VariableBenchmarks` and the `Legacy*` vectors. CI drops the Timing filter.
- **Why:** Plan, Phase 2. net6.0 is the baseline runtime because it is upstream's newest target;
  Phase 3 adds the new runtime to the same project so both can be compared. BenchmarkDotNet
  0.15.8 is the newest release (November 2025) and still supports net6.0; the project first
  used 0.14.0 and moved before the baseline was recorded, so all results share one version. The legacy
  `SmallBranching` expression contains a typo (`OF` for `OR`) and never compiled; the old test
  hid it by compiling `SmallExpression` twice. The benchmark corrects the typo.
- **Behaviour:** none.
- **Verified:** dry run of all 48 benchmarks succeeds; tests: 1,657 pass, 150 known failures.
- **Discussed:** not needed. The 10 % regression threshold is the plan's suggestion, and the
  vector with the maintainer's own expressions is still open.

### R-009: Original individual and calc-engine tests ported

- **Kind / phase:** test / Phase 1
- **Commits:** c51a3d9, b56253b, 1af6e3b
- **What:** From the original VB.NET test project: 31 of the 32 `IndividualTests` (new
  `ExpressionTests/IndividualTests.cs`), the script-driven `SimpleCalcEngineTests` (filling the
  empty stub, one case per line of `SimpleCalcEngineTests.txt`), and 12 `CalcEngineTestFixture`
  tests that the C# conversion had lost. Original names and expectations are kept.
- **Why:** They cover threading, imports, owners, overload resolution, on-demand variables and
  functions, cloning and the calculation engine, none of which the scripts reach. Adaptations,
  each commented in the code: internal type names (`Ciloci.Flee.*` became `Flee.*`), a .NET
  Framework-only `Math` method, NUnit 3 `Assert.Throws` for `ExpectedException`, worker-thread
  failures rethrown on the test thread, and a rebuilt `CaseSensitiveOwner` (the original was a
  binary-only DLL). `TestStringQuote` is left out: `ExpressionParserOptions.StringQuote` no longer
  exists. The original overload test caught every exception, so its "ambiguous" cases could not
  fail; the port checks them.
- **Known failures:** two, marked `[Category("KnownFailure")]` and ignored with the cause:
  `TestOverloadResolution` (two calls resolve to an overload instead of being rejected as
  ambiguous; for `ReferenceType4("abc")` the original expectation looks wrong, C# picks the same
  overload) and `TestElementNamesInResourceFile` (three element classes have no entry in
  `ElementNames.resx`).
- **Behaviour:** none (test only).
- **Verified:** Debug: 1,704 pass, 152 ignored, 0 fail. Release: 0 fail.
- **Discussed:** not needed.

### R-010: IL length check reads ILGenerator.ILOffset

- **Kind / phase:** fix / Phase 3
- **Commits:** a57e2ed
- **What:** `Utility.GetILGeneratorLength` returns `ilg.ILOffset` instead of reading the private
  field `ILGenerator.m_length` by reflection.
- **Why:** The field does not exist on .NET 8 and later. Every compile in a Debug build of Flee
  then threw `NullReferenceException` from the Debug-only length check (upstream PR #117, issues
  #110 and #82). `ILOffset` is public, has the same value and exists on every target.
- **Behaviour:** Debug builds on .NET 8+ work. Release builds never ran this code.
- **Verified:** with the old code the suite on net8.0 Debug failed 1,511 tests with
  `NullReferenceException`; with the fix it passes as on net6.0.
- **Discussed:** not needed (required by the target decision, R-011).

### R-011: Target frameworks netstandard2.0/2.1, net8.0, net10.0; tests on net8.0 and net10.0

- **Kind / phase:** build, test / Phase 3
- **Commits:** 2c066c0, 84724ae
- **What:** Flee targets netstandard2.0, netstandard2.1, net8.0 and net10.0 instead of net6.0,
  net5.0, netstandard2.1 and netstandard2.0. The tests run on net8.0 and net10.0; CI installs the
  .NET 8 runtime instead of .NET 6.
- **Why:** .NET 5 and 6 are out of support; net8.0 and net10.0 are the current long-term-support
  releases. Apps on .NET 5 to 7 still get the netstandard2.1 build.
- **Behaviour:** consumers on .NET 8+ get a build compiled for their runtime. No source change.
  One known failure changed shape: `mouse.shareddt.gettype().name` threw
  `InvalidProgramException` on .NET 6 and crashes the process on .NET 8 and 10, so it moved to
  the `crash` category.
- **Verified:** net8.0 and net10.0: 1,704 pass, 152 known failures, the same list as on net6.0.
- **Discussed:** decided by the maintainer on 2026-10-09.

### R-012: Reflection.Emit packages only for netstandard2.0

- **Kind / phase:** build / Phase 3
- **Commits:** f0975d5
- **What:** `System.Reflection.Emit`, `.ILGeneration` and `.Lightweight` 4.7.0 are referenced only
  for netstandard2.0. `System.Reflection` 4.3.0 and `System.ComponentModel` 4.3.0 are removed.
- **Why:** The plan's item. All other targets have Reflection.Emit built in, and the two 4.3.0
  packages were not needed by any target. Fewer stale transitive dependencies for consumers.
- **Behaviour:** none.
- **Verified:** all four targets build; tests unchanged.
- **Discussed:** not needed.

### R-013: Test packages updated

- **Kind / phase:** test / Phase 3
- **Commits:** e6783b8
- **What:** Microsoft.NET.Test.Sdk 18.10.1, NUnit3TestAdapter 6.3.0, coverlet.collector 10.1.0,
  NUnit 3.14.0.
- **Why:** The plan's item. NUnit stays on 3.x: NUnit 4 and 5 move the classic asserts
  (`Assert.AreEqual` and friends, used throughout the suite) to `ClassicAssert`, which would mean
  touching nearly every test. That is a separate decision.
- **Behaviour:** none.
- **Verified:** results unchanged on both runtimes.
- **Discussed:** not needed; moving to NUnit 4+ is open.

### R-014: Package metadata: licence expression, readme, symbols

- **Kind / phase:** build / Phase 3
- **Commits:** 89c455a
- **What:** `PackageLicenseExpression LGPL-2.1-or-later` replaces a licence URL that pointed at the
  upstream repository; the icon URL (also the repository page) is dropped; the package carries
  `README.md`, a `.snupkg` symbol package and repository information for Source Link. The
  description names the fork.
- **Why:** The plan's item; removes the NU5125 and NU5048 pack warnings.
- **Behaviour:** none in the library.
- **Verified:** packed to a scratch folder and read the nuspec: licence expression, readme,
  repository commit and the four lib folders are present; netstandard2.0 alone depends on the
  Reflection.Emit packages.
- **Discussed:** not needed. Found in passing, left for Phase 6: the package ships
  `Resources/DocComments.xml` as a content file, which NuGet adds to every consuming project.

### R-015: EmitToAssembly is a no-op and obsolete

- **Kind / phase:** api / Phase 3
- **Commits:** ede53c2 (pinning tests), da0cbf6
- **What:** `ExpressionOptions.EmitToAssembly` keeps its getter and setter but no longer does
  anything, and carries `[Obsolete]`. The code that re-emitted each expression into an in-memory
  assembly is removed.
- **Why:** The assembly was never saved (the save call was commented out because .NET Core could
  not save dynamic assemblies), so the option only cost compile time and leaked memory: such
  assemblies are never unloaded. Keeping the property keeps the fork source-compatible; callers
  get a compiler warning that tells them it has no effect.
- **Behaviour:** compiling with the option set is faster and no longer leaks; results are the
  same. Code that sets the option gets warning CS0618.
- **Verified:** new `EmitToAssemblyTests` pass before and after; full suite unchanged in Debug and
  Release on both runtimes.
- **Discussed:** decided by the maintainer on 2026-10-09 (keep as a no-op). Reimplementing it on
  net10.0 with `PersistedAssemblyBuilder` (.NET 9+) is noted as a post-release idea.

### R-016: Benchmarks run on net6.0, net8.0 and net10.0

- **Kind / phase:** build / Phase 3
- **Commits:** bf2f858
- **What:** The benchmark project targets net6.0, net8.0 and net10.0, so `--runtimes` compares
  them in one run.
- **Why:** The plan asks for the old and the new runtime to be compared with the baseline. On
  net6.0 Flee now comes from its netstandard2.1 build, because net6.0 is no longer a Flee target.
- **Behaviour:** none.
- **Verified:** dry run on all three runtimes.
- **Discussed:** not needed.

### R-017: Nullable reference types off in the library

- **Kind / phase:** build / Phase 3
- **Commits:** the commit that adds this entry
- **What:** `<Nullable>disable</Nullable>` in `src/Flee/Flee.csproj` (was `enable`). The test and
  benchmark projects keep `enable`.
- **Why:** Upstream switched nullable on in 2022 over 2007 code that was never annotated, which
  produced about 1,240 warnings per target and buried every useful one. The long-term goal is a
  fully annotated library: files opt in with `#nullable enable` once annotated, public API first
  (a Phase 4 item), the parser last; when every file has opted in, the project switches to
  `enable` with nullable warnings as errors.
- **Behaviour:** none. Nullable annotations only affect compile-time warnings.
- **Verified:** solution warnings drop from 2,430 to 450; tests unchanged in Debug and Release on
  net8.0 and net10.0.
- **Discussed:** decided by the maintainer on 2026-10-09.

### R-018: .editorconfig describing the existing style

- **Kind / phase:** build / Phase 3
- **Commits:** the commit that adds this entry
- **What:** A root `.editorconfig`: UTF-8 (with BOM for C#), CRLF, four-space indents, braces on
  their own line, keyword types (`int`, `string`). Script files under `TestScripts` are left
  exactly as they are.
- **Why:** The plan's item. It writes down the style the code already uses so editors keep to it.
  Nothing is enforced in the build: making style rules or analyzer findings into errors only makes
  sense once the build is clean, and the remaining 450 warnings are Phase 4 cleanup. The .NET SDK
  analyzers already run at their default level; raising it is a later decision.
- **Behaviour:** none; the build output is identical (450 warnings, 0 errors).
- **Verified:** full rebuild.
- **Discussed:** not needed. Warnings as errors moves to the end of Phase 4.

### R-020: Expected reasons in InvalidExpressions.txt updated

- **Kind / phase:** test / Phase 4
- **Commits:** the commit that adds this entry
- **What:** The expected `CompileExceptionReason` on 63 lines of `InvalidExpressions.txt` is
  changed to the reason the library reports today. The 63 matching entries (categories
  `wrong-reason` and `script-error`) leave `KnownFailures.txt`. Every changed line, with the old
  and new reason and why they differ, is listed in `doc/known-failures.md`, section "Expected
  reasons updated in Phase 4". Line 333 had a result value (`200`) where the reason belongs.
- **Why:** The original harness never compared these reasons, so the script had drifted from the
  library. Every expression is still rejected; only the reported reason differs, and today's
  reasons are reasonable (an unknown function is `UndefinedName`, a malformed `if` is a
  `SyntaxError`). The library is unchanged.
- **Behaviour:** none (test data only).
- **Verified:** net8.0 and net10.0: 1,769 pass, 89 skipped (87 script known failures and 2
  fixture tests), 0 fail.
- **Discussed:** decided by the maintainer on 2026-10-10: update the script and document the
  difference.
