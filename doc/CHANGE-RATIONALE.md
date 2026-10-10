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
| R-021 | UInt32 and UInt64 constants above the signed maximum compile | fix | Phase 4 |
| R-022 | `in` works with non-generic IList and IDictionary | fix | Phase 4 |
| R-023 | Out-of-range real literals report ConstantOverflow again | fix | Phase 4 |
| R-024 | Method calls on value types use the value-type path (GetType crash) | fix | Phase 4 |
| R-025 | Debug IL length check passes for 0xFFFFFFFF as a long | fix | Phase 4 |
| R-026 | Member access on value-type calculation-engine atoms (upstream #64, #111) | fix | Phase 4 |
| R-027 | Element names for three elements in the resources | fix | Phase 4 |
| R-028 | Fields renamed to common C# naming conventions | cleanup | Phase 4 |
| R-029 | Misplaced [Obsolete] attributes turned back into doc comments | cleanup | Phase 4 |
| R-030 | Comparisons with true and false removed | cleanup | Phase 4 |
| R-031 | Converter TODO markers on loop exits checked and removed | cleanup | Phase 4 |
| R-032 | Boolean & and \| replaced by && and \|\| | cleanup | Phase 4 |
| R-033 | ArithmeticElement looks up its helper methods once | cleanup | Phase 4 |
| R-034 | One type per file, file names match type names | cleanup | Phase 4 |
| R-035 | Debug.Assert on impossible paths becomes an exception | fix | Phase 4 |
| R-036 | Operator binders: BindToMethod returns null as designed | fix | Phase 4 |
| R-037 | Unused variables and fields removed (library and tests) | cleanup | Phase 4 |
| R-038 | Public API annotated for nullable reference types | api | Phase 4 |
| R-039 | Unknown names in calculation-engine expressions are compile errors | fix | Phase 4 |
| R-040 | A failed CalculationEngine.Add leaves nothing behind | fix | Phase 4 |
| R-041 | Detached NamespaceImports can be compared | fix | Phase 4 |

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

### R-019: Generated code no longer inlines Flee's helpers (.NET 10 first-call cost)

- **Kind / phase:** perf, fix / Phase 4
- **Commits:** 63a7928 (benchmark stage), the commit that adds this entry
- **What:** `[MethodImpl(MethodImplOptions.NoInlining)]` on the methods that generated IL calls:
  `VariableCollection.GetVariableValueInternal<T>`, `GetFunctionResultInternal<T>`,
  `GetVirtualPropertyValueInternal<T>`, `CalculationEngine.GetResult<T>` and the getter of
  `ExpressionContext.CalculationEngine`. The benchmarks gain a `CompileAndEvaluate` stage.
- **Why:** On .NET 10 every compiled expression that reads a variable (or calls an on-demand
  function, or references a calculation-engine atom) took about 1.4 ms on its first call, against
  about 0.05 ms on .NET 8. Loading 100 calculation-engine atoms took 444 ms instead of 17 ms. The
  JIT with tiered PGO inlined these helpers into each generated method. Found by tracing with
  dotnet-trace, confirmed by `DOTNET_TieredPGO=0` and by a minimal `DynamicMethod` repro that
  calls `GetVariableValueInternal<int>`. Marking them `NoInlining` is the smallest change that
  removes the cost without asking applications to change runtime settings. The maintainer uses
  the calculation engine, so this was the first Phase 4 item.
- **Behaviour:** none functionally. Performance on .NET 10: first call of a variable-reading
  expression 2.2 ms -> 0.11 ms (ArithmeticVariables, compile included); calculation-engine load
  444 ms -> 14.4 ms. Steady-state evaluation is unchanged or faster on both runtimes.
- **Verified:** tests unchanged (1,769 pass, 89 skipped); before-and-after benchmarks on .NET 8 and
  10 in `benchmarks/results/phase4-net10-first-call`. The calculation engine still loads 1.6
  times slower on .NET 10 than on .NET 8; recorded, not investigated further.
- **Discussed:** maintainer asked for the investigation (2026-10-10); not needed for the fix.

### R-021: UInt32 and UInt64 constants above the signed maximum compile

- **Kind / phase:** fix / Phase 4
- **Commits:** the commit that adds this entry
- **What:** `UInt32LiteralElement` and `UInt64LiteralElement` emit their value with an unchecked
  conversion to the signed IL constant instead of `Convert.ToInt32` / `Convert.ToInt64`.
- **Why:** The checked conversion threw `OverflowException` for every value above
  `Int32.MaxValue` (uint) or `Int64.MaxValue` (ulong), so expressions such as `4294967295U`,
  `0xFFFFFFFF` or `uint.maxvalue` did not compile at all. IL has no unsigned constants; the bits
  are the same, which is what an unchecked cast gives. Upstream #83 fixed the same pattern in
  `LiteralElement` only.
- **Behaviour:** these expressions now compile and give the right values. Nothing that compiled
  before changes, because the conversion only differs for values that used to throw.
- **Verified:** the 67 script cases of the `unsigned-literal` category pass and leave the
  known-failures list (failing first, as pinned in Phase 1). Debug: 1,836 pass, 22 skipped;
  Release: 1,838 and 20; net8.0 and net10.0.
- **Discussed:** not needed (bug fix).

### R-022: `in` works with non-generic IList and IDictionary

- **Kind / phase:** fix / Phase 4
- **Commits:** the commit that adds this entry
- **What:** `InElement.GetTargetCollectionType` falls back to the non-generic
  `System.Collections.IList` and `IDictionary` instead of the open generic `IList<>` and
  `IDictionary<,>`.
- **Why:** `typeof(IList<>).IsAssignableFrom(...)` is never true, so `x in list` with an
  `ArrayList`, a `Hashtable` or any other collection that only implements the non-generic
  interfaces failed to compile ("not a known collection type"). The code comment ("a regular
  IList or IDictionary") shows the non-generic interfaces were meant; most likely a VB-to-C#
  conversion slip. `Contains` is then called on the non-generic interface, with the operand
  converted to `object`.
- **Behaviour:** `in` against such collections now compiles and returns the right result. Types
  with a generic `ICollection<T>` or `IDictionary<K,V>` are matched earlier and unchanged.
- **Verified:** the 7 script cases of the `in-collection` category pass and leave the
  known-failures list; full suite green on net8.0 and net10.0.
- **Discussed:** not needed (bug fix).

### R-023: Out-of-range real literals report ConstantOverflow again

- **Kind / phase:** fix / Phase 4
- **Commits:** the commit that adds this entry
- **What:** `DoubleLiteralElement.Parse` and `SingleLiteralElement.Parse` treat an infinite parse
  result as an overflow and raise the `ConstantOverflow` compile error. The existing
  `OverflowException` handler stays for .NET Framework, where parsing still throws.
- **Why:** Since .NET Core 3.0, `double.Parse` and `float.Parse` return infinity for values out of
  range instead of throwing, so `1.7976931348623157E+309` silently compiled to infinity. A
  literal can never mean infinity, so infinity can only come from an out-of-range value.
- **Behaviour:** such literals are rejected at compile time with `ConstantOverflow`, as Flee did on
  .NET Framework. Expressions that compute infinity at run time (`1.0 / 0`) are unaffected.
- **Verified:** the 7 script cases of the `real-overflow-undetected` category pass and leave the
  known-failures list; full suite green on net8.0 and net10.0.
- **Discussed:** not needed (bug fix that restores the documented behaviour).

### R-024: Method calls on value types use the value-type path (GetType crash)

- **Kind / phase:** fix / Phase 4
- **Commits:** the commit that adds this entry
- **What:** `MemberElement.EmitMethodCall` tests `mi.ReflectedType.IsValueType` instead of
  `mi.GetType().IsValueType`.
- **Why:** `mi.GetType()` is the type of the `MethodInfo` object, never a value type, so every
  method call took the reference-type path (`callvirt`). For `GetType()` on a value-type field,
  which needs the value boxed, that produced invalid IL: `DateTimeA.GetType().Name` killed the
  process with an access violation, and the static-field variant threw `InvalidProgramException`
  (.NET 6) or crashed (.NET 8 and 10). The original VB code tested `mi.ReflectedType`; the
  VB-to-C# conversion changed it.
- **Behaviour:** `GetType()` on value-type members works. Other method calls on value types now
  get the IL the original design intended (`call` for the struct's own methods, `constrained.`
  plus `callvirt` for inherited `Equals`, `GetHashCode`, `ToString`); results are unchanged.
- **Verified:** the 4 script cases of the `crash` category run and pass, and leave the
  known-failures list; full suite green in Debug and Release on net8.0 and net10.0 (Release:
  1,856 pass, 2 skipped).
- **Discussed:** not needed (bug fix).

### R-025: Debug IL length check passes for 0xFFFFFFFF as a long

- **Kind / phase:** fix / Phase 4
- **Commits:** the commit that adds this entry
- **What:** `LiteralElement.EmitLoad(long)` emits values between `Int32.MaxValue` and
  `UInt32.MaxValue` through the Int32 overload of `EmitLoad`, followed by `conv.u8` as before,
  instead of calling `Emit(OpCodes.Ldc_I4, ...)` directly.
- **Why:** For 0xFFFFFFFF the signed operand is -1, and the runtime's `ILGenerator` writes that as
  the one-byte `ldc.i4.m1`, while Flee's length bookkeeping counted the five-byte `ldc.i4`. The
  Debug-only consistency check then failed (category `debug-il-length`). Found by logging both
  lengths per opcode. The Int32 overload chooses the short forms itself, so both agree.
- **Behaviour:** none in Release; Debug builds no longer assert. The emitted IL is the same as the
  runtime already produced.
- **Verified:** the last 2 script known failures pass; `KnownFailures.txt` is empty. Debug and
  Release: 1,856 pass, 2 skipped (fixture known failures), net8.0 and net10.0.
- **Discussed:** not needed (bug fix).

### R-026: Member access on value-type calculation-engine atoms (upstream #64, #111)

- **Kind / phase:** fix / Phase 4
- **Commits:** 044429f (pinning tests), the commit that adds this entry
- **What:** `IdentifierElement.EmitReferenceLoad` loads the address of a value-type atom result
  when the next element calls a member on it, as the variable path already did.
- **Why:** An atom such as `d = now` (a `DateTime`) followed by `res = d.AddDays(1)`, or
  `Duration = end - start` followed by `Duration.TotalHours`, produced invalid IL. Upstream users
  saw `NullReferenceException` (#64) and `InvalidProgramException` (#111); on .NET 10 the test
  host dies with an internal CLR error. The maintainer uses the calculation engine.
- **Behaviour:** such expressions now compile and evaluate correctly. Atoms of reference types and
  atoms used without a member access emit the same IL as before.
- **Verified:** new `ValueTypeAtomTests` (two tests, from the two issues) crashed before the fix
  (committed as ignored known failures) and pass after it; full suite green in Debug and Release
  on net8.0 and net10.0.
- **Discussed:** not needed (bug fix).

### R-027: Element names for three elements in the resources

- **Kind / phase:** fix / Phase 4
- **Commits:** the commit that adds this entry
- **What:** `ElementNames.resx` (and its designer file) gets entries for `LocalBasedElement` and
  `DecimalLiteralElement`, and the entry `Int32Literal` is renamed to `Int32LiteralElement`, the
  class it belongs to.
- **Why:** `ExpressionElement.Name` looks the name up by class name and is used as the prefix of
  compile error messages. For these three classes it returned null (and asserted in Debug
  builds). Found by the ported original test `TestElementNamesInResourceFile`.
- **Behaviour:** compile errors raised by these elements now start with the element name, like all
  others; Debug builds no longer assert there.
- **Verified:** `TestElementNamesInResourceFile` passes and is no longer ignored; full suite green
  on net8.0 and net10.0 (1,859 pass, 1 skipped).
- **Discussed:** not needed (bug fix).

### R-028: Fields renamed to common C# naming conventions

- **Kind / phase:** cleanup / Phase 4
- **Commits:** the commit that adds this entry
- **What:** 150 fields with the VB-era prefixes `_my`, `_our`, `My` and `Our` in `src/Flee` are
  renamed: private instance and private static fields to `_camelCase` (`_myValue` -> `_value`),
  private static readonly fields to `PascalCase` (`OurBinaryTypes` -> `BinaryTypes`), non-private
  fields of internal classes to `PascalCase` (`MyLeftChild` -> `LeftChild`). These are the default
  naming rules of the maintainer's IDE (JetBrains Rider), which flagged the old names. Four names
  would have clashed with an existing member and were resolved by hand: `MemberElement.MyName`
  became the property `MemberName` (replacing a read-only property of that name),
  `ExpressionResultPair.MyExpression` and `GenericExpressionResultPair<T>.MyResult` became private
  fields `_expression` and `_result`, and `DefaultExpressionOwner.Instance` became an
  auto-property.
- **Why:** Requested by the maintainer (2026-10-10). The renames were done with Roslyn's renamer
  (a small tool run from the scratch area), not by text replacement, so every reference follows
  and nothing else changes. Fields declared in `src/Flee/Parsing` (five, in the hand-written
  analyzer and the tokenizer) are left alone, following the rule not to touch the parser before
  the release. Nothing visible outside the assembly is renamed.
- **Behaviour:** none.
- **Verified:** build (450 warnings, as before); tests green in Debug and Release on net8.0 and
  net10.0 (1,859 pass, 1 skipped). The public API of the netstandard2.0 build, listed by
  reflection, is identical to upstream Flee 2.0.0 except for the intended `[Obsolete]` on
  `EmitToAssembly` (R-015).
- **Discussed:** decided by the maintainer on 2026-10-10.

### R-029: Misplaced [Obsolete] attributes turned back into doc comments

- **Kind / phase:** cleanup / Phase 4
- **Commits:** the commit that adds this entry
- **What:** Twelve internal classes outside the parser carried `[Obsolete("...")]` whose text is
  a class description ("Holds various shared utility methods", "Represents a function call").
  The text becomes a `/// <summary>` comment and the attribute goes.
- **Why:** The VB-to-C# conversion turned the original descriptions into `Obsolete` attributes.
  The classes are not obsolete; the attributes only produced warning CS0618 at every use (the plan
  noted it for `PropertyDictionary`). Two more in `src/Flee/Parsing` (`Analyzer`, `Parser`) stay
  for now, following the rule not to touch the parser.
- **Behaviour:** none; all twelve classes are internal.
- **Verified:** solution warnings drop from 450 to 106; tests unchanged.
- **Discussed:** not needed (plan item).

### R-030: Comparisons with true and false removed

- **Kind / phase:** cleanup / Phase 4
- **Commits:** the commit that adds this entry
- **What:** 260 comparisons of a `bool` expression with a literal in 39 files outside the parser
  are simplified: `x == true` and `x != false` become `x`, `x == false` and `x != true` become
  `!x`.
- **Why:** Plan item; a VB conversion idiom. Done with a small Roslyn tool working on the syntax
  tree, which only touches comparisons whose other side is a non-nullable `bool` (for `bool?`,
  `x == true` means something different; there were none) and adds parentheses where `!` needs
  them. Files in `src/Flee/Parsing` are left alone.
- **Behaviour:** none.
- **Verified:** tests green in Debug and Release on net8.0 and net10.0; build warnings unchanged
  (106).
- **Discussed:** not needed (plan item).

### R-031: Converter TODO markers on loop exits checked and removed

- **Kind / phase:** cleanup / Phase 4
- **Commits:** the commit that adds this entry
- **What:** The four comments `// TODO: might not be correct. Was : Exit For` (or `Exit While`)
  after `break;` in `CalculationEngine.EmitLoad`, `InvocationListElement.ResolveNamespaces` (two)
  and `ExpressionImports.FindType` are removed.
- **Why:** Plan item. The converter flagged every VB `Exit For` / `Exit While` because C#'s `break`
  leaves the innermost loop or `switch`, while VB's exits the named loop kind. In all four places
  the `break` sits directly in the loop the VB code exited, with no inner loop or `switch` in
  between, so it does exactly what the VB code did.
- **Behaviour:** none (comments only).
- **Verified:** by reading each loop; build and tests unchanged.
- **Discussed:** not needed (plan item).

### R-032: Boolean & and | replaced by && and ||

- **Kind / phase:** cleanup / Phase 4
- **Commits:** the commit that adds this entry
- **What:** 71 uses of `&` and `|` on `bool` operands in 17 files outside the parser become `&&`
  and `||`.
- **Why:** Plan item: the non-short-circuit operators always evaluate both sides, which is a bug
  wherever the right side relies on the left (`x != null & x.Foo`). The review found no such case:
  every right side is a pure check (type tests, comparisons, `EndsWith`). The conversion is still
  worth doing as idiomatic C# that does not invite that bug in later edits. Done with a Roslyn
  rewriter that only touches `bool` operands and would add parentheses where `||` ends up under
  `&&` (needed nowhere).
- **Behaviour:** none; right sides without side effects are now skipped when the left side decides.
- **Verified:** tests green in Debug and Release on net8.0 and net10.0; warnings unchanged.
- **Discussed:** not needed (plan item).

### R-033: ArithmeticElement looks up its helper methods once

- **Kind / phase:** cleanup / Phase 4
- **Commits:** the commit that adds this entry
- **What:** The three `MethodInfo` fields of `ArithmeticElement` (`Math.Pow` and two
  `string.Concat` overloads) are `static readonly` and initialised once, instead of being
  reassigned by every instance constructor.
- **Why:** Plan item. Static fields written from an instance constructor are a race between
  threads compiling expressions at the same time (harmless here, since every thread writes the
  same value) and cost three reflection lookups for every arithmetic operator parsed. The plan
  names this class; it is the only one in the library with that pattern.
- **Behaviour:** none.
- **Verified:** tests green; no benchmark run for this change alone (the saving is three
  reflection lookups per `+`, `-`, `*`, `/`, `%` or `^` parsed, too small to separate from noise
  in the stage benchmarks without a dedicated run).
- **Discussed:** not needed (plan item).

### R-034: One type per file, file names match type names

- **Kind / phase:** cleanup / Phase 4
- **Commits:** the commit that adds this entry
- **What:** Outside `src/Flee/Parsing`, every file now holds one type and is named after it. The
  multi-type files (the four `Miscellaneous.cs`, both `Exceptions.cs`, `BranchManager.cs`,
  `VariableTypes.cs`, `ImportTypes.cs`, `ResourceKeys.cs`) are split into 47 files, and 39
  single-type files are renamed (`Arithmetic.cs` -> `ArithmeticElement.cs`, `Member.cs` ->
  `MemberElement.cs`, and so on). Folders and namespaces are unchanged.
- **Why:** Plan item; makes types easy to find and stops the IDE's file-name hints. Done with a
  small Roslyn-based tool that copies each type's text verbatim (with its comments) under the
  original file's usings and namespace, and `git mv` for the renames, so history follows.
- **Behaviour:** none. Source-file paths quoted in older documents (Phase 1 findings,
  `doc/known-failures.md`) refer to the old names.
- **Verified:** build unchanged (106 warnings); tests green in Debug and Release on net8.0 and
  net10.0; public API identical to before (reflection listing).
- **Discussed:** not needed (plan item).

### R-035: Debug.Assert on impossible paths becomes an exception

- **Kind / phase:** fix / Phase 4
- **Commits:** the commit that adds this entry
- **What:** 16 of the 39 `Debug.Assert`/`Debug.Fail` checks outside the parser now throw in all
  builds: `switch` defaults for unknown operations or types (arithmetic, compare, and/or, shift,
  real-literal type, numeric cast target, short constants), a compare element with no matching
  emit path, and the three places where an implicit conversion that type checking promised fails
  during emission. They throw `InvalidOperationException("Flee internal error: ...")`. A literal
  field of an unsupported type throws `NotSupportedException` naming the type.
- **Why:** Plan item. In a Release build these asserts vanish, and the code carried on with no IL
  emitted or a null element, so a broken invariant surfaced later as `InvalidProgramException`, a
  wrong result or a `NullReferenceException`. Now it fails where it happens, with a message.
- **Kept as asserts (23), on purpose:** invariants that a later line would hit anyway (argument
  counts in overload scoring, local index range, which `Convert` checks), development checks (IL
  length bookkeeping, element names, and `PropertyDictionary`'s unknown-name check, which only a
  typo in Flee's own property names can trigger and every Debug test run would catch), and two
  that Release code relies on being reachable: `ImplicitConverter`'s type index returns -1 for
  non-primitive value types and callers test for it; `MemberElement`'s accessibility check
  returns false for member kinds it does not know.
- **Behaviour:** none on any path the tests reach; only paths that were already broken now throw a
  clear exception.
- **Verified:** tests green in Debug and Release on net8.0 and net10.0.
- **Discussed:** not needed (plan item).

### R-036: Operator binders: BindToMethod returns null as designed

- **Kind / phase:** fix / Phase 4
- **Commits:** the commit that adds this entry
- **What:** `CustomBinder` overrides `Binder.BindToMethod` and returns null, and the two derived
  binders (`BinaryOperatorBinder`, `ExplicitOperatorMethodBinder`) lose their forwarding override
  and the field it forwarded to.
- **Why:** The VB original's `CustomBinder.BindToMethod` returned Nothing. The conversion produced
  a separate, non-overriding method with a `ref` parameter, and each derived binder implemented
  the real override by calling `BindToMethod` on a field that was never assigned (warning CS0649),
  so a call would have thrown `NullReferenceException`. Flee only ever calls `SelectMethod` on
  these binders, so the path is not reached today.
- **Behaviour:** none on reachable paths.
- **Verified:** tests green in Debug and Release on net8.0 and net10.0.
- **Discussed:** not needed (bug fix).

### R-037: Unused variables and fields removed (library and tests)

- **Kind / phase:** cleanup / Phase 4
- **Commits:** the commit that adds this entry
- **What:** Unused `catch` variables in four literal elements and in `LongScriptTests`, the unused
  field `FleeILGenerator._brContext`, and a nullable warning in the test helper `TestData` (`Id`
  initialised to an empty string).
- **Why:** Plan item (clear the build warnings before treating them as errors).
- **Behaviour:** none.
- **Verified:** solution warnings 106 -> 74 (together with R-036); tests green.
- **Discussed:** not needed.

### R-038: Public API annotated for nullable reference types

- **Kind / phase:** api / Phase 4
- **Commits:** eb4a789 (`PublicTypes`, 21 files), 07d5d25 (`CalcEngine/PublicTypes`, 5 files),
  and the commit that adds this entry (test and benchmark call sites)
- **What:** The 26 files of the public API start with `#nullable enable` and are annotated to match
  what the code does: parameters the code rejects when null are non-nullable, values that can be
  null are marked `?` (for example `IDynamicExpression.Evaluate()` returns `object?`,
  `ExpressionOptions.ResultType` is `Type?`, `CalculationEngine.GetExpression` returns
  `IExpression?`, event-args results that start unset are nullable, and `VariableCollection`
  implements `IDictionary<string, object?>` because variables can hold null). `!` is used only where
  an invariant guarantees a value, each with a comment. Tests and benchmarks that cast
  `Evaluate()` results get `!` or nullable return types.
- **Why:** Decision R-017: files opt in once annotated, public API first, so that callers with
  nullable enabled see where null is allowed.
- **Behaviour:** none at run time; only nullability metadata is added. Method signatures are
  unchanged (reflection listing identical to upstream except `EmitToAssembly`'s `[Obsolete]`).
  Callers who enable nullable may see new warnings where they ignore a possible null, as intended.
  Debatable choices: `ResultType`'s setter shows as nullable although it rejects null
  (netstandard2.0 has no `[DisallowNull]`), `VariableCollection.Add` takes `object?` because the
  interface requires it, and `GetVariableValueInternal<T>` stays `T` while its two neighbours return
  `T?`.
- **Verified:** zero nullable warnings in the annotated files on all four targets; solution warnings
  back to 74; tests green in Debug and Release on net8.0 and net10.0. A before/after nullability
  listing of every public member was produced with `NullabilityInfoContext` (scratch tool, not in
  the repository).
- **Found on the way, not fixed here:** a name that is not an atom in a calculation-engine
  expression gives `ArgumentNullException` instead of a compile error; a failed
  `CalculationEngine.Add` leaves its name behind; `NamespaceImport.Equals` on an import not yet
  attached throws `NullReferenceException`. These are Phase 4 bug candidates.
- **Discussed:** decided by the maintainer on 2026-10-09 (R-017).

### R-039: Unknown names in calculation-engine expressions are compile errors

- **Kind / phase:** fix / Phase 4
- **Commits:** 2303840 (pinning tests), the commit that adds this entry
- **What:** `IdentifierElement` treats a name as a calculation-engine atom only when the engine
  has an atom of that name (`CalculationEngine.HasTail`); otherwise it reports the usual
  `UndefinedName` compile error.
- **Why:** For an expression added to an engine, every name that was not a variable or member was
  passed to the engine as a dependency, even if no such atom existed. The dependency manager then
  threw `ArgumentNullException` (parameter "key"), which says nothing about the expression. Found
  while annotating the public API (R-038).
- **Behaviour:** `engine.Add("a", "1 + zzz", context)` now throws `ExpressionCompileException`
  with reason `UndefinedName`, like a plain context, instead of `ArgumentNullException`. Valid
  expressions are unaffected; the batch loader adds atoms in dependency order, so references are
  known when they are compiled.
- **Verified:** `UnknownNameIsUndefinedNameCompileError` failed before and passes now; all
  calculation-engine tests and the full suite green.
- **Discussed:** not needed (bug fix). Callers catching `ArgumentNullException` here would need to
  catch `ExpressionCompileException` instead; worth a line in the migration notes.

### R-040: A failed CalculationEngine.Add leaves nothing behind

- **Kind / phase:** fix / Phase 4
- **Commits:** 2303840 (pinning test), the commit that adds this entry
- **What:** `CalculationEngine.Add` removes the atom it registered (the "temporary head") when
  compiling the expression throws, then rethrows the original exception.
- **Why:** `Add` registers the name before compiling, so that other atoms and circular references
  can be resolved. When compiling failed, the name stayed: `Contains` returned true,
  `GetExpression` returned null, `Recalculate` threw `NullReferenceException`, and adding the
  name again failed with "already exists". The batch loader already cleared the whole engine on a
  compile error, so it was only `Add` used directly that was affected.
- **Behaviour:** after a failed `Add` the engine is as it was before the call. The exception the
  caller sees is unchanged.
- **Verified:** `FailedAddLeavesNoAtomBehind` failed before and passes now; circular-reference and
  batch-load tests and the full suite green.
- **Discussed:** not needed (bug fix).

### R-041: Detached NamespaceImports can be compared

- **Kind / phase:** fix / Phase 4
- **Commits:** 2303840 (pinning test), the commit that adds this entry
- **What:** `NamespaceImport.EqualsInternal` uses the context's member string comparison when the
  import is attached, and case-insensitive ordinal comparison (the comparison of Flee's default
  options) when it is not.
- **Why:** It dereferenced the context unconditionally, so `Equals`, and `Contains` or `Remove` on a
  namespace with a `NamespaceImport` argument, threw `NullReferenceException` for imports not yet
  added to a context. Found while annotating the public API (R-038).
- **Behaviour:** comparing detached imports works; attached imports compare exactly as before.
- **Verified:** `DetachedNamespaceImportsCanBeCompared` failed before and passes now; full suite
  green in Debug and Release on net8.0 and net10.0.
- **Discussed:** not needed (bug fix).
