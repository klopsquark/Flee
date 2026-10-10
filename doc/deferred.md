# Deferred work and alternatives

Everything that was postponed, and every alternative that was considered and not taken, so it can
be picked up when its time comes. The maintainer asked for this list on 2026-10-10.

How to use it:

- Each entry has an id (`D-nn`), where it came from, why it waits, and when to pick it up.
- When an entry is done, mark it **Done** with the change-log id (`R-nnn`) instead of deleting it.
- New deferrals go here in the same form, in the same pull request that defers them.

## Overview

| Id | Item | Pick up | Status |
| --- | --- | --- | --- |
| D-01 | Identifiers with non-ASCII letters (upstream #76, #70) | After the first release | Open |
| D-02 | Parser and grammar experiments | After the first release | Open |
| D-03 | Mechanical cleanup inside `src/Flee/Parsing` | After the first release | Open |
| D-04 | Typed collections between parser and elements | After the first release | Open |
| D-05 | Nullable annotations for the internals, then project-wide | After the first release | Open |
| D-06 | `EmitToAssembly` on net10.0 with `PersistedAssemblyBuilder` | After the first release | Open |
| D-07 | Remaining .NET 10 gap when loading the calculation engine | Phase 7 or later | Open |
| D-08 | Constant expression evaluates 5 ns slower on .NET 10 | Phase 7 or later | Open |
| D-09 | Changing a variable's type through the indexer (upstream #26) | Phase 5 | Open |
| D-10 | User-defined `&` and `\|` operators for `and`/`or` (upstream #54) | Phase 5 | Open |
| D-11 | Evaluating one expression with different owners in parallel (upstream #99, #23) | Phase 5 | Open |
| D-12 | Overload ambiguity: report it as C# does | Phase 5 or a breaking release | Alternative not taken |
| D-13 | Overload scoring for `params` methods uses integer division (upstream #84) | Phase 5, together with D-12 | Open |
| D-14 | Public API baseline file | Phase 5 | Open |
| D-15 | Structural candidates from the code review | Phase 5 | Open |
| D-16 | Setters that accept null and fail later | Phase 5 | Open |
| D-17 | `GetVariableValueInternal<T>` returns `T`, its siblings `T?` | Phase 5 | Open |
| D-18 | Two theoretical null dereferences | When touched | Open |
| D-19 | Obsolete serialization members of `ExpressionCompileException` | Next breaking release | Open |
| D-20 | NUnit 4 or 5 for the test project | When convenient | Open |
| D-21 | Benchmark vector with the maintainer's own expressions | Phase 7, or earlier if expressions are supplied | Open |
| D-22 | Fixes from the maintainer's local Flee copy | When supplied | Open |
| D-23 | `Resources/DocComments.xml` is shipped as a content file | Phase 6 | Open |
| D-24 | Other names the IDE may flag | When convenient | Open |

## Entries

### D-01: Identifiers with non-ASCII letters (upstream #76, #70)

- **What:** `Motörhead` or Cyrillic names such as `Москва` do not parse as identifiers. The token
  pattern is `[a-z_]\w*`, and the bundled regular-expression engine's `\w` only accepts ASCII
  (`NFAWordTransition` in `src/Flee/Parsing/TokenNFA.cs`).
- **Why deferred:** it changes the language and lives in the parser, which stays untouched before
  the release (decided 2026-10-10).
- **Pick up:** after the first release, as part of D-02 or on its own behind the old/new parser
  switch; add the upstream cases as script tests first.

### D-02: Parser and grammar experiments

- **What:** a hand-written parser, ANTLR 4, or ANTLR 4 as a grammar workbench.
- **Where:** the plan, section "After the first release: parser and grammar experiments".
- **Pick up:** after the first release. The parse-only benchmark and the 1,756 script cases are the
  safety net.

### D-03: Mechanical cleanup inside `src/Flee/Parsing`

- **What:** everything Phase 4 did outside the parser but not inside it:
  - the misplaced `[Obsolete("...")]` attributes on `Analyzer` and `Parser` (class descriptions the
    VB conversion turned into attributes, as in R-029); they cause most of the remaining build
    warnings, including one in `CalcEngine/InternalTypes/IdentifierAnalyzer.cs`;
  - the five fields with `_my` prefixes in `CustomExpressionAnalyzer.cs` and
    `ExpressionTokenizer.cs` (R-028 skipped them);
  - `== true`/`== false` and `&`/`|` on booleans (R-030, R-032 skipped the parser);
  - an unused catch variable in `LookAheadReader.cs` and a rethrow that loses the stack trace
    in `ReaderBuffer.cs` (CA2200);
  - one type per file (R-034 skipped the parser).
- **Why deferred:** the parser is not touched before the release; the Turkish keyword fix (#105)
  was the one agreed exception.
- **Pick up:** after the first release, before or with D-02. Then the remaining warnings can be
  made errors everywhere.

### D-04: Typed collections between parser and elements

- **What:** about 69 uses of non-generic collections (`IList`, `ArrayList`, `Stack`, `Hashtable`)
  pass values between the hand-written analyzer (`src/Flee/Parsing/CustomExpressionAnalyzer.cs`)
  and the expression elements. Typed collections would be clearer and safer.
- **Why deferred:** the change crosses into the parser. Plan item in Phase 4.
- **Pick up:** after the first release, with D-03.

### D-05: Nullable annotations for the internals, then project-wide

- **What:** the public API is annotated (R-038). The internals are still `#nullable disable`.
  When every file has opted in, switch the project to `<Nullable>enable</Nullable>`, remove the
  per-file lines and make nullable warnings errors (decision R-017).
- **Pick up:** after the first release, file by file; the parser last.

### D-06: `EmitToAssembly` on net10.0 with `PersistedAssemblyBuilder`

- **What:** the option is an obsolete no-op (R-015). .NET 9 added `PersistedAssemblyBuilder`, which
  can save generated IL to a DLL. On the net10.0 build the option could save the IL for inspection
  with ILSpy or ILVerify. netstandard2.0/2.1 and net8.0 have no such API. The saved assembly is for
  inspection only, because expressions may read private owner members that a separate assembly
  cannot.
- **Estimate:** about a day; the second emit pass that the option used already exists.
- **Pick up:** after the first release, if inspecting generated IL is wanted.

### D-07: Remaining .NET 10 gap when loading the calculation engine

- **What:** after R-019, loading 100 atoms takes 14.4 ms on .NET 10 against 8.9 ms on .NET 8, both
  faster than the .NET 6 baseline (16.2 ms). With `DOTNET_TieredPGO=0` the gap closes, so some
  tiered-PGO cost remains in compiling many small dynamic methods.
- **Where:** `benchmarks/results/phase4-net10-first-call/README.md`.
- **Pick up:** Phase 7, with a profile; possibly report to dotnet/runtime with the minimal repro
  (a `DynamicMethod` calling a non-trivial generic method).

### D-08: Constant expression evaluates 5 ns slower on .NET 10

- **What:** `1 + 2 * 3` evaluates in 11.9 ns on .NET 10 against 6.7 ns in the baseline; every
  other vector is faster on .NET 10. Probably call overhead of a trivially small delegate.
- **Where:** `benchmarks/results/phase3-runtimes/README.md`.
- **Pick up:** Phase 7, only if it matters in a real workload.

### D-09: Changing a variable's type through the indexer (upstream #26)

- **What:** `Variables["x"] = "1"; Variables["x"] = 1;` keeps the variable typed as `string`;
  compiled expressions then fail later. Options: throw at the assignment, or replace the variable
  with one of the new type (expressions compiled against the old type would need recompiling).
- **Why deferred:** a behaviour change, so Phase 5 (decided 2026-10-10).

### D-10: User-defined `&` and `|` operators for `and`/`or` (upstream #54)

- **What:** `a and b` on a type with `operator &` reports "operation not defined"; `+` and `-`
  overloads work. `AndOrElement` has no overloaded-operator lookup. Upstream PR #67 (2019) adds one
  and is a useful reference, not a merge candidate: it targets the old project layout and also
  changes how ambiguous operator overloads are resolved.
- **Why deferred:** new capability, so Phase 5 (decided 2026-10-10).

### D-11: Evaluating one expression with different owners in parallel (upstream #99, #23)

- **What:** `Evaluate()` reads a shared owner field, so setting `Owner` and evaluating from several
  threads mixes results. The issue proposes an `Evaluate(object owner)` overload.
- **Why deferred:** API addition, so Phase 5 (decided 2026-10-10).

### D-12: Overload ambiguity: report it as C# does

- **What:** with overloads `f(double)` and `f(decimal)`, `f(100)` picks the `decimal` overload,
  because `decimal`'s user-defined implicit conversion from `int` scores better than the built-in
  `int` to `double` widening (`ImplicitConverter.GetImplicitConvertScore`). C# reports the call as
  ambiguous. The ported original test `TestOverloadResolution` expected an ambiguity error; it now
  records today's choice instead.
- **Alternative not taken:** raise `AmbiguousMatch` like C#. Expressions that compile today would
  stop compiling.
- **Decision:** keep today's behaviour for source compatibility and document it (2026-10-10).
- **Pick up:** Phase 5 or a release that may break, together with D-13.

### D-13: Overload scoring for `params` methods uses integer division (upstream #84)

- **What:** `CustomMethodInfo.ComputeScoreForParamArray` divides integers, so scores for
  `params` overloads lose precision; upstream fixed half of #84. Fixing it can change which overload
  an existing expression calls.
- **Pick up:** with D-12; first add script cases that show today's choices.

### D-14: Public API baseline file

- **What:** a checked-in listing of the public API, compared in CI, so every change to the public
  surface shows up in review. Phase 4 already compares by reflection by hand (R-028, R-034,
  R-038), with a scratch tool; the listing format exists.
- **Pick up:** Phase 5, before the first API change.

### D-15: Structural candidates from the code review

- **What:** string-keyed options, leaked parser types (`ParseException`), element creation through
  `Activator.CreateInstance`, compile context through a service container.
- **Where:** the plan, Phase 5.

### D-16: Setters that accept null and fail later

- **What:** `SimpleCalcEngine.Context = null` is accepted and the next `AddDynamic` throws
  `NullReferenceException`; `ExpressionParserOptions.DateTimeFormat = null` is accepted and every
  date literal then fails to compile. Validating in the setter would be clearer.
- **Why deferred:** behaviour change (a new exception at assignment), so Phase 5.

### D-17: `GetVariableValueInternal<T>` returns `T`, its siblings `T?`

- **What:** after R-038, `GetFunctionResultInternal<T>` and `GetVirtualPropertyValueInternal<T>`
  return `T?`, while `GetVariableValueInternal<T>` returns `T` although it can return null for a
  reference-type `T`. Public because generated IL calls them; not meant for callers.
- **Pick up:** Phase 5, possibly by hiding these members (an API change).

### D-18: Two theoretical null dereferences

- **What:** `ExpressionImports.AddMethod` and `MethodImport.Validate` dereference
  `MethodInfo.ReflectedType`, which is null for a module-level (global) method; C# cannot declare
  one. `VariableCollection.GetVirtualPropertyValueInternal` assumes the instance-level
  `TypeDescriptor` properties contain the property found at compile time on the type; a custom
  `ICustomTypeDescriptor` could break that.
- **Pick up:** when these files are touched for other reasons.

### D-19: Obsolete serialization members of `ExpressionCompileException`

- **What:** the serialization constructor and `GetObjectData` override use formatter-based
  serialization APIs that .NET marks obsolete (SYSLIB0051, CS0672 on net8.0 and net10.0). Removing
  them changes the public API.
- **Pick up:** the next release that may break source compatibility.

### D-20: NUnit 4 or 5 for the test project

- **What:** the tests use NUnit 3.14 (R-013). NUnit 4 and 5 move the classic asserts
  (`Assert.AreEqual`, `Assert.IsTrue`, ...) to `ClassicAssert`, which touches nearly every test.
- **Pick up:** when convenient; a mechanical change of its own.

### D-21: Benchmark vector with the maintainer's own expressions

- **What:** the plan asks for a vector built from the maintainer's own applications; it decides
  which optimizations are worth anything.
- **Pick up:** Phase 7, or earlier as soon as sample expressions are supplied.

### D-22: Fixes from the maintainer's local Flee copy

- **What:** the plan (Phase 0) mentions fixes in a local copy, to bring in during Phase 4 once tests
  can prove them. None have been supplied yet.
- **Pick up:** when supplied; each with a failing test first.

### D-23: `Resources/DocComments.xml` is shipped as a content file

- **What:** the NuGet package lists `Resources/DocComments.xml` under `contentFiles`, so NuGet adds
  it to every consuming project. Its text (about 1,200 lines of original API documentation) belongs
  in XML documentation comments instead (plan, Phase 6).
- **Pick up:** Phase 6.

### D-24: Other names the IDE may flag

- **What:** R-028 renamed the `_my`/`_our`/`My`/`Our` prefixes. A few other names may still not
  match the IDE's defaults, for example private fields in PascalCase such as
  `ShortCircuitInfo.Labels`.
- **Pick up:** when convenient, with the same Roslyn-based rename.
