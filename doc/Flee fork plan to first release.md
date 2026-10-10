# Flee fork: plan to first release

Oct 9, 2026 · @Robert

## Goal and ground rules

The first release is a fork of Flee 2.0.0 that builds on a current .NET SDK, runs a real regression suite, is documented, and depends on nobody else's maintenance. Performance work is a stretch goal for that release and does not block it.

- **Pin behaviour before changing it.** Tests record what the library does today, including its bugs, before any fix.
- **Measure before optimizing.** The benchmark baseline exists before the SDK or any code changes.
- **One kind of change per commit.** Mechanical cleanup, bug fixes and API changes never share a commit.
- **Every phase ends with a green CI run.**

Basis: upstream `mparlak/Flee` at commit `f3b4fe2` (March 2022). Figures in this plan come from reading that commit. Nothing was built or run, so build and test behaviour is unverified.

Work that is postponed, and alternatives that were not taken, are listed in `doc/deferred.md`.

## Phases at a glance

The phases run strictly in this order; each one protects the next.

| Phase | Content | Done when |
| --- | --- | --- |
| 0. Fork setup | Repository, license file, identity, compatibility stance | Decisions are written down and the baseline commit is tagged |
| 1. Build and tests | Unchanged code builds; script-driven tests run; CI | CI is green and every failing case is on a known-failures list |
| 2. Benchmark baseline | Benchmark project, test vectors, first numbers | Baseline results are committed for the old runtime |
| 3. SDK modernization | Target frameworks, dependencies, packaging, warnings | Builds without warnings, tests green, benchmarks compared with baseline |
| 4. Bug fixes and cleanup | Known failures, converter leftovers, mechanical cleanup | Known-failures list is empty or each entry is consciously deferred |
| 5. Own adjustments | Your API changes and extensions | Each change has tests and a changelog entry |
| 6. Documentation | README, language reference, API guide, migration notes | A new user can install, write an expression and extend it from the docs alone |
| 7. Performance (stretch) | Optimizations chosen from benchmark evidence | Time box is used up, or the chosen items show measured gains |
| Release | Checklist, package, tag | Package is published |

## Phase 0: Fork setup

This phase settles the choices that are expensive to change later. No code changes yet.

- [x] Fork `mparlak/Flee` with full history and tag `f3b4fe2` as the baseline. Tag: `upstream-baseline`.
- [x] Add a `LICENSE` file. The grammar header says LGPL 2.1 or later; the repository has no license file today. Keep the existing copyright notices. Check the terms yourself if you distribute commercially.
- [x] Choose a package ID and decide whether the root namespace stays `Flee`. Decided 2026-10-09: package ID stays `Flee`, namespace stays `Flee`.
- [x] Decide the compatibility stance: drop-in replacement for Flee 2.0.0, or free to break. Suggested: source-compatible for the first release, with every deviation documented. Decided 2026-10-09: source-compatible with Flee 2.0.0.
- [ ] Keep the fixes from your local copy aside. They go in during Phase 4, once tests can prove them. None supplied yet; tracked as D-22 in `doc/deferred.md`.
- [x] Skim the 75 open upstream issues and 2 open pull requests. Collect those with a reproducible case as candidates for Phase 4. Result: `doc/upstream-issues.md`, 8 reproducible bugs still present.

## Phase 1: Build and test sanitation

The unchanged library gets a regression suite that actually runs. Today the test project has 48 test methods, while 1,756 expression cases sit in four script files that no test reads.

| Script file | Cases | Line format |
| --- | --- | --- |
| `ValidExpressions.txt` | 1,198 | result type; expression; expected result |
| `InvalidExpressions.txt` | 251 | result type; expression; expected compile error |
| `ValidCasts.txt` | 172 | result type; cast expression; expected result |
| `CheckedTests.txt` | 135 | expression; checked; should overflow |

- [x] Build the solution with the smallest change that works. Touch only the test project's target if the old runtime is not installed. Note the warning count. Result: builds unchanged with the .NET 10 SDK and the .NET 6 runtime; 2,412 warnings (Debug, whole solution), almost all nullable warnings.
- [x] Rebuild the script harness as data-driven NUnit tests, one test case per script line. `test/Flee.Test/ScriptTests`.
- [x] Reconstruct the expression owner the scripts expect. Found and ported: the original VB.NET test project survives in `george-playstudiosasia/PlayStudios.Flee` (LGPL 2.1). Its individual and calc-engine tests were ported too (R-009). They use members such as `bytea` and `sbytea`, and that fixture is not in this repository or its history. The original VB.NET Flee source is the place to look; I have not checked that it is still obtainable.
- [x] Run everything and put each failing case on a known-failures list, marked as a category. Do not fix anything yet. 150 of 1,756 script cases, see `doc/known-failures.md`.
- [x] Clean the existing tests: `SimpleCalcEngineTests.TestScripts` is empty, `LongScriptTests` has commented-out file loading, and the timing tests in `Benchmarks.cs` belong in Phase 2.
- [x] Set up CI (build and test) on Windows and Linux. `.github/workflows/ci.yml`, Debug and Release.
- [x] Record line coverage as a starting figure. The coverage collector is already referenced. 66.8 % (8,553 of 12,797 lines) after the script tests, before the ported individual tests.

The owner fixture is the main risk of this phase. If it cannot be recovered, it has to be inferred from the scripts, which is slower but possible.

## Phase 2: Benchmark baseline

A separate benchmark project records how fast the untouched library is, so later phases can be judged against it. It comes before the SDK change because a new runtime shifts the numbers on its own.

- [x] Create a `Benchmarks` project with BenchmarkDotNet and move the timing tests out of the test project. `benchmarks/Flee.Benchmarks`.
- [x] Measure three stages separately: parse only, full compile (parse, emit, delegate creation) and evaluate. Parse-only needs access to internals from the benchmark project.
- [x] Record time and allocations for every vector.
- [x] Commit the results as the baseline, together with machine and runtime details. `benchmarks/results/baseline-net6.0`.
- [x] Set a regression threshold for later phases. Suggested start: investigate anything slower by more than 10 %. Adopted, see `benchmarks/README.md`.
- [x] Keep benchmarks out of the CI gate. Shared build machines are too noisy for that; run them by hand before and after relevant changes.

### Test vectors

Each vector is a small set of expressions run through all three stages.

| Vector | Example | What it exercises |
| --- | --- | --- |
| Constants | `1 + 2 * 3` | Parser and emit floor |
| Arithmetic with variables | `sqrt(a^2 + b^2)` | Variable reads, imported functions, power |
| Many variables | Sum of 50 variables | Variable lookup cost at evaluation |
| Owner members | `Price * Quantity` on an owner object | Field and property access |
| Mixed numeric types | `int * double + long` style mixes | Type promotion and conversions |
| Strings | Concatenation and comparison | String handling |
| Logic and conditionals | Long `and`/`or` chains, nested `if` | Short-circuit code, long-branch second emit pass |
| `in` lists | `x in (1, 2, 3, ...)` | List membership code |
| Casts | `cast(x, long)` | Explicit conversions |
| On-demand variables and functions | Values supplied through events | Event-based resolution path |
| Large expression | Several hundred terms | Scaling of parse and compile |
| Calculation engine | 100 dependent expressions, one input changed | Dependency ordering and recalculation |
| Your own expressions | Taken from your applications | The workload that matters most to you |

The examples are placeholders. The last row deserves the most care, because it decides which optimizations are worth anything to you.

## Phase 3: SDK modernization

The project moves to a current SDK and supported targets, with tests and benchmarks proving nothing else changed. Today it targets net6.0, net5.0, netstandard2.1 and netstandard2.0.

- [x] Choose the targets: the current long-term-support .NET, plus netstandard2.0 only if you need .NET Framework consumers. Check the support dates when you decide. Decided 2026-10-09: netstandard2.0, netstandard2.1, net8.0, net10.0 (R-011).
- [x] Remove the `System.Reflection.Emit` 4.x and related package references wherever the target framework already provides them. (R-012)
- [x] Decide on nullable annotations. They are switched on over code that was never annotated. Suggested: switch off project-wide, then enable file by file, public API first. Decided 2026-10-09: off project-wide now; files opt in with `#nullable enable` once annotated; public API in Phase 4, the rest after the release, the parser last; finally enable project-wide with nullable warnings as errors (R-017).
- [x] Update the test packages. NUnit stays on 3.14; moving to NUnit 4+ is open (R-013).
- [x] Fix the package metadata: license expression, real project URLs, README in the package, source link, symbols. The license and icon URL fields currently just point at the repository. (R-014)
- [x] Decide what happens to `EmitToAssembly`. The save call is commented out, so the README's "IL can be saved to an assembly" is no longer true. Remove the option or reimplement it. Decided 2026-10-09: kept as an obsolete no-op (R-015).
- [x] Add an `.editorconfig` and analyzers, and treat warnings as errors once the build is clean. `.editorconfig` added (R-018); the SDK analyzers run at their default level. The build still has 450 warnings, so warnings as errors moves to the end of Phase 4.
- [x] Run the benchmarks on the old and the new runtime and compare both with the baseline. This separates the runtime's effect from yours. `benchmarks/results/phase3-runtimes`: .NET 10 about 45 % faster overall; calculation-engine loading 24 times slower on .NET 10 (Phase 4 item).

## Phase 4: Cheap bug fixes and cleanup

With tests and a baseline in place, the low-cost defects and the conversion leftovers go. Every fix starts with a failing test.

### Bug fixes

- [x] Work through the known-failures list from Phase 1. Empty: all 150 fixed or corrected (R-020 to R-025).
- [x] Check the four `break; // TODO: might not be correct` markers against the intended loop behaviour: two in `InvocationList.cs`, one each in `CalculationEngine.cs` and `ExpressionImports.cs`. All correct (R-031).
- [x] Review the roughly 29 places that combine booleans with `&` or `|`. Both sides are always evaluated there, which is wrong wherever the right side relies on the left. 71 found, none relies on the left; all converted to `&&`/`||` (R-032).
- [x] Fix static fields that are assigned in instance constructors, as in `ArithmeticElement`. The only case (R-033).
- [x] Go through the 32 `Debug.Assert` checks and turn those guarding real error conditions into exceptions. They vanish in release builds. 39 found; 16 now throw, 23 kept with reasons (R-035).
- [x] Bring in the fixes from your local copy and the upstream issues collected in Phase 0. Upstream: #64, #111, #105 fixed (R-026, R-042); the rest deferred (`doc/upstream-issues.md`, status table). Local copy: none supplied (D-22).

- [x] Find out why loading the calculation engine is 24 times slower on .NET 10 than on .NET 8 (`benchmarks/results/phase3-runtimes`). JIT inlining of helpers; fixed (R-019).
- [x] Clear the remaining build warnings (obsolete `PropertyDictionary` uses, unused variables), then treat warnings as errors. Done (R-029, R-036, R-037, R-044); the parser's warnings are suggestions until D-03.

### Mechanical cleanup

- [x] Annotate the public API for nullable reference types (`PublicTypes`, `CalcEngine/PublicTypes`, about 25 types), file by file with `#nullable enable`. Done (R-038); the internals follow after the release (D-05). Real null bugs found on the way get a test and their own fix.

These change no behaviour and each gets its own commit.

- [x] Remove the 227 `== true` and `== false` comparisons. 260 found and removed outside the parser (R-030).
- [x] Split the four `Miscellaneous.cs` files into one type per file and make file names match type names. Done for the whole library outside the parser (R-034).
- [ ] Replace the non-generic collections (about 69 uses) between analyzer and elements with typed ones. Deferred: crosses into the parser (D-04).
- [x] Decide whether to rename the `_my` and `_our` field prefixes, about 1,100 occurrences. It is a matter of taste; if you do it, do it in a single commit. Decided 2026-10-10: renamed to common conventions (R-028); the five parser fields stay.
- [x] Resolve `PropertyDictionary`: it is marked obsolete but still backs three public classes. Drop the attribute now; replacing it belongs to Phase 5. Dropped, with eleven other misplaced attributes (R-029).

## Phase 5: Adjustments to your needs

This is where the fork starts to differ from Flee on purpose. The concrete list is yours and is still open.

- [ ] Write down the API changes and extensions you want, and mark each as additive or breaking.
- [ ] Add a public API baseline file so that every change to the public surface shows up in review. The surface is small, about 25 types.
- [ ] Give each change its tests and a changelog entry.

The code review turned up four structural items that would make extensions easier. They are candidates, not commitments.

- **String-keyed options.** `ExpressionOptions`, `ExpressionParserOptions` and `ExpressionContext` store their settings in a dictionary keyed by name. Plain typed fields would be simpler and safer.
- **Leaked parser types.** `ParseException` and its error enums are public and come from the bundled parser runtime. Wrapping them in an exception type of your own now keeps later parser experiments from breaking users.
- **Element creation.** Elements are created through `Activator.CreateInstance` from a `Type`. Factories or direct construction would make adding operators more explicit.
- **Compile-time plumbing.** Elements fetch their context from a service container, 15 lookups in total. A typed compile context object would be easier to follow.

## Phase 6: Documentation

Documentation is written after the API settles, so it describes what ships. Upstream offers a short README and a wiki page of examples.

- [ ] **README:** what the fork is, how it relates to Flee, status, installation and one worked example.
- [ ] **Language reference:** operators and precedence, literal forms, `if`, `cast` and `in`, type promotion rules, case-insensitivity, the configurable decimal and argument separators. `Expression.grammar` covers 40 tokens and 29 productions; the rest is defined in code and has to be written down.
- [ ] **API guide:** contexts, imports, variables, on-demand variables and functions, expression owners, options and the calculation engine.
- [ ] **XML comments on every public type.** `Resources/DocComments.xml` holds about 1,200 lines of original API documentation with examples and is a good source.
- [ ] **Architecture note:** the pipeline from parser to element tree to IL emission, for your future self.
- [ ] **Limitations:** no NativeAOT or iOS because of runtime IL generation, and what is and is not thread-safe.
- [ ] **Migration notes from Flee 2.0.0** and a changelog.
- [ ] Compile the documentation examples as tests so they cannot go stale.

## Phase 7: Performance optimization (stretch)

Optimization is the last, optional goal for the first release: it gets a fixed time box, and the release ships without it if the box runs out. Each change needs before-and-after numbers from the Phase 2 vectors and must leave behaviour untouched.

Start by profiling the vectors, then pick from the evidence. From reading the code, these are the candidates, most promising first:

1. **Variable reads at evaluation.** Each read is a dictionary lookup by name plus an interface call. Resolving the variable once at compile time would remove the lookup from the hot path.
2. **Compile path.** Expressions with long branches are emitted twice, elements are created by reflection, and some helper methods are looked up by name on every compile. Caching and direct construction are cheap wins if compile time matters to you.
3. **Context cloning and locking.** Every compiled expression clones its context, and parsing takes a lock per context. This matters for compiling many expressions or compiling from several threads.
4. **Parser allocations.** The tokenizer and parser allocate heavily. Only worth touching if the parse-only benchmark shows a large share of compile time, and it overlaps with the parser experiments below.

This ranking is a reading of the code, not a measurement. The profile may reorder it.

## First release checklist

- [ ] CI is green on every target.
- [ ] The known-failures list is empty, or each remaining entry is documented as a limitation.
- [ ] Benchmarks show no regression beyond the threshold against the baseline.
- [ ] Documentation, changelog and migration notes are complete.
- [ ] Package ID, version number and license metadata are final.
- [ ] The package is published and the release is tagged.

## After the first release: parser and grammar experiments

These are deliberately unplanned and start only once the release is out. They are noted here so the earlier phases do not close the door on them.

- **The seam is narrow.** Only three files outside the `Parsing` folder touch the parser, and the contract is one call that turns a string into an element tree.
- **Candidates:** a hand-written recursive-descent or Pratt parser, ANTLR 4 as the shipped parser, or ANTLR 4 only as a workbench for the grammar.
- **What the earlier phases already provide:** the script corpus as a specification (Phase 1), the parse-only benchmark (Phase 2), your own parse exception type (Phase 5) and the written language reference (Phase 6).
- **How to stay safe:** keep the current parser behind a switch and run both over every valid and invalid test expression until they agree.

Other post-release ideas:

- **Reimplement `EmitToAssembly` on net10.0** with `PersistedAssemblyBuilder` (.NET 9+), so the generated IL can be saved and inspected with ILSpy or ILVerify. netstandard and net8.0 have no such API. The saved assembly is for inspection only, since expressions may read private owner members.

## Open decisions

| Decision | Needed by | Suggestion |
| --- | --- | --- |
| Package ID and root namespace | Phase 0 | **Decided 2026-10-09:** package ID `Flee`, namespace `Flee` |
| Drop-in compatible with Flee 2.0.0, or free to break | Phase 0 | **Decided 2026-10-09:** source-compatible with Flee 2.0.0 |
| Public NuGet package or private use only | Phase 0 | **Decided 2026-10-09:** private feed only, nothing on nuget.org |
| Target frameworks, and whether netstandard2.0 stays | Phase 3 | **Decided 2026-10-09:** netstandard2.0, netstandard2.1, net8.0, net10.0 |
| Keep or remove `EmitToAssembly` | Phase 3 | **Decided 2026-10-09:** keep as an obsolete no-op |
| Nullable reference types | Phase 3 | **Decided 2026-10-09:** off now, files opt in once annotated, public API first (Phase 4) |
| Rename the `_my` and `_our` prefixes | Phase 4 | **Decided 2026-10-10:** rename to common C# conventions |
| Your list of API changes and extensions | Phase 5 | Open |
| Time box for performance work | Phase 7 | Open |
