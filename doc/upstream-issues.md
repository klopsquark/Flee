# Upstream issues and pull requests (mparlak/Flee)

- Date: 2026-10-09
- Source: GitHub REST API, `GET /repos/mparlak/Flee/issues?state=open&per_page=100` (unauthenticated), plus comments for selected issues and the two PRs' file lists
- Baseline for code checks: commit `f3b4fe2` (only `src/Flee/Flee.csproj` differs between `f3b4fe2` and the current `develop`)
- Counts: 77 open items = **75 open issues + 2 open PRs**
- Reproducible (concrete expression or snippet plus expected vs actual): **26**
  - 8 still present at `f3b4fe2` (real fix candidates)
  - 9 already fixed or largely fixed at `f3b4fe2`
  - 9 reproducible, but by design, user error or a documentation problem
- Partial repro (symptom or stack trace, but no complete case): 16; no repro: 1; the rest are feature requests, questions, docs, packaging or PRs

Code checks were done by reading and grepping `src/Flee` only. Nothing was built or run, so "present" and "fixed" are based on reading the code.

## Status in the fork

| Issue | Status |
| --- | --- |
| #64, #111 | Fixed (R-026) |
| #105 | Fixed (R-042) |
| #76, #70 | Deferred until after the release (`doc/deferred.md`, D-01) |
| #54, #99 (#23), #26 | Phase 5 (D-10, D-11, D-09) |
| #84 (remaining half) | With the overload decisions (D-13) |
| #110, #82 (Debug builds on .NET 8+) | Fixed (R-010, the same change as upstream PR #117) |

## Reproducible candidates still present at f3b4fe2

| Issue | Title | Area | Repro (expected vs actual) | Code at f3b4fe2 |
|---|---|---|---|---|
| [#64](https://github.com/mparlak/Flee/issues/64) | NullReferenceException when trying to call DateTime methods | calc engine, IL emit | `CalculationEngine`: variable `now` = `DateTime.Now`; atom `d` = `"now"`; atom `res` = `"d.AddDays(1)"`. Expected now + 1 day, actual NullReferenceException in the generated method. `"now.AddDays(1)"` inside a single atom works. | Present. `IdentifierElement.EmitReferenceLoad` (`ExpressionElements/MemberElements/Identifier.cs`) calls `CalculationEngine.EmitLoad` but never loads the address of a value-type result when `NextRequiresAddress` is true. The variable path (`EmitVariableLoad` via `Member.EmitMethodCall`) does this. |
| [#111](https://github.com/mparlak/Flee/issues/111) | Accessing TimeSpan.TotalHours gives "invalid program" | calc engine, IL emit | Atom `Duration` = `"endtimestamp-starttimestamp"` (DateTime - DateTime = TimeSpan), atom `Hours` = `"Duration.TotalHours"`. Expected a double, actual `InvalidProgramException` on recalculate. | Present. Same root cause as #64 (value-type atom result followed by member access). |
| [#105](https://github.com/mparlak/Flee/issues/105) | Bug in TokenStringDFA | culture, parser | `CurrentCulture = tr-TR`; `CompileGeneric<double>("IF (R < 0.8; 1; 2)")`. Expected to compile, actual `ParseException: unexpected character 'I'` (capital I lowercases to dotless i). | Present. `Char.ToLower` (culture-sensitive) in `Parsing/TokenStringDFA.cs` (4 places), also `TokenNFA.cs`, `Automaton.cs`, `CharacterSetElement.cs`, `StringElement.cs`, `RegExp.cs`. |
| [#76](https://github.com/mparlak/Flee/issues/76) | Umlaut in variable name | parser | `Variables.Add("Motörhead", true)`; `CompileGeneric<bool>("Motörhead")`. Expected true, actual `ParseException: unexpected character 'ö'`. | Present. IDENTIFIER is `[a-z_]\w*` and the NFA `\w` (`NFAWordTransition.Match` in `TokenNFA.cs`) only accepts ASCII. |
| [#70](https://github.com/mparlak/Flee/issues/70) | Exception for non-Latin identifier | parser | `CompileDynamic("test1.Москва")`. Expected member access, actual `SyntaxError: unexpected character 'М'`. | Present. Same cause as #76. |
| [#54](https://github.com/mparlak/Flee/issues/54) | Overloading AND and OR operators gives an exception | operators | Class with `operator &` and `operator |`; variables `a`, `b` of that type; `"a and b"`. Expected the overload to be called, actual `AndOrElement: Operation 'And' is not defined for types 'MyClass' and 'MyClass'`. `+` and `-` overloads work. | Present. `ExpressionElements/LogicalBitwise/AndOr.cs` has no overloaded operator lookup. PR #67 adds it. |
| [#99](https://github.com/mparlak/Flee/issues/99) | Support switching owner in a multithreaded environment | threading | One compiled `"Number % 2 = 1"`, 1000 tasks each set `e1.Owner = new Owner{Number=i}` then `Evaluate()`. Expected each result to match its owner, actual cross-talk between threads. Proposed fix: `Evaluate(object owner)` overload. | Present. `Expression<T>.Evaluate()` reads the shared `_myOwner` field (`InternalTypes/Expression.cs`), no per-call owner overload. Also covers #23. |
| [#26](https://github.com/mparlak/Flee/issues/26) | Changing type of variables without removing them | type conversion | `Variables["salary"] = "70000"; Variables["salary"] = 70000;` then compile `"salary * 1.1"`. Expected numeric result, actual exception (variable stays typed as String). Remove then re-add works. | Present. `VariableCollection` indexer setter assigns `ValueAsObject` with no type check, and `GenericVariable<T>.MyValue` is `object`, so a mismatched value is stored silently and fails later. |

## Reproducible, but fixed at f3b4fe2 or not a bug

| Issue | Title | Area | Repro (expected vs actual) | Status at f3b4fe2 |
|---|---|---|---|---|
| [#86](https://github.com/mparlak/Flee/issues/86) | Illegal one-byte branch | IL emit / long branches | Nested `IF(2.1<>2.1, IF(2.1>2.1, ..., IF(... AND ...)))`. Expected a value, actual `NotSupportedException: Illegal one-byte branch`. | Fixed. Branch handling reworked (`BranchManager.ComputeBranches`), regression test `NestedConditionalsForLongBranches` in `LongScriptTests.cs`. |
| [#83](https://github.com/mparlak/Flee/issues/83) | Long compares above UInt32.MaxValue fail | literals | `"2432696330L = 2432696330L"`. Expected true, actual `Convert.ToInt32` overflow. | Fixed. `LiteralElement` emits `unchecked((int)Convert.ToUInt32(value))`, test in `ExpressionBuildingTest.cs`. |
| [#13](https://github.com/mparlak/Flee/issues/13) | Expressions as variables cause InvalidCastException | IL emit | Wiki sample: `e1 = "cos(a) ^ 2"`, `e2 = "sin(a) ^ 2"`, add both as variables, evaluate `"a + b"`. Expected about 1.0, actual `InvalidCastException`. | Fixed. `IsOptimizablePower` checks `MyRightChild is Int32LiteralElement`, the wiki sample is a test in `ExpressionBuildingTest.cs`. |
| [#50](https://github.com/mparlak/Flee/issues/50) | Expressions as variables don't work | IL emit | Same wiki sample as #13, same `InvalidCastException`. | Fixed (duplicate of #13). |
| [#73](https://github.com/mparlak/Flee/issues/73) | ExpressionContext exception in constructor | parser | `new ExpressionContext()` with first-chance exceptions enabled shows `RegExpException: invalid repeat count '{1,3}'`, `'{4}'`, `'{2}'`. Caught internally, but costly when contexts are created often. | Likely fixed. The REAL, STRING_LITERAL, CHAR_LITERAL and TIMESPAN patterns (the ones with `{n,m}`) are now added with `AddPattern(pattern, false)` and skip the NFA (`Parsing/ExpressionTokenizer.cs`). Confirm with a first-chance exception check. |
| [#93](https://github.com/mparlak/Flee/issues/93) | RegExpException invalid repeat count | parser | `new CalculationEngine(); new ExpressionContext(); ... CreateBatchLoader()` with CLR exceptions enabled. | Likely fixed (duplicate of #73). |
| [#39](https://github.com/mparlak/Flee/issues/39) | ExpressionContext doesn't work | parser | Constructing `ExpressionContext` throws in `TokenRegExpParser` on `{1,3}`. | Likely fixed (duplicate of #73). |
| [#46](https://github.com/mparlak/Flee/issues/46) | RegExpException in Flee.NetStandard20.dll | parser | `new ExpressionContext()` always throws `RegExpException` (first chance). | Likely fixed (duplicate of #73). |
| [#75](https://github.com/mparlak/Flee/issues/75) | Memory leak in Expression Clone | other (memory) | Clone a prototype context many times; each clone's `VariableCollection` hooks `CaseSensitiveChanged` on the prototype's options, so the prototype keeps all clones alive. | Largely fixed: `CloneInternal` now builds `new VariableCollection(context)` on the clone. Residual: `ExpressionOptions.Clone` uses `MemberwiseClone`, so the cloned options still carry the prototype's event subscribers. |
| [#72](https://github.com/mparlak/Flee/issues/72) | Evaluate throws if culture is not US | culture | On de-DE, `MyFunction(1, 2)` gives `unexpected character ','`. | By design: `ParseCulture` defaults to `CurrentCulture`, so de-DE uses `;` as the argument separator and `,` as the decimal separator. The wiki page on culture-sensitive expressions is outdated. Possible fork decision: default to invariant culture. |
| [#108](https://github.com/mparlak/Flee/issues/108) | Boolean expression with decimals | type conversion | Decimal variable, `"myDecVar >= 10.0"`. Actual: `Operation 'GreaterThanOrEqual' is not defined for types 'Decimal' and 'Double'`. | By design (same rule as C#). Workaround: `Options.RealLiteralDataType = Decimal` or the `10.0M` suffix. |
| [#40](https://github.com/mparlak/Flee/issues/40) | Mixing decimal and double in calculations | type conversion | Decimal `vdec`, `"vdec * 1.23"`. Actual: `Operation 'Multiply' is not defined for types 'Decimal' and 'Double'`. | By design. `Options.RealLiteralDataType = RealLiteralDataType.Decimal` exists. |
| [#116](https://github.com/mparlak/Flee/issues/116) | Equality/inequality operations not working | parser | `"2021 == 2022"` fails with an invalid literal error; `"-123.Equals(0)"` fails in NegateElement. | Not a bug: Flee uses `=` and `<>`, not `==`/`!=`, and unary minus binds looser than member access (as in C#). |
| [#81](https://github.com/mparlak/Flee/issues/81) | sqrt function not working | other | `"sqrt(16)"` gives `Could find not function 'sqrt(Int32)'`. | Not a bug: needs `context.Imports.AddType(typeof(Math))` (confirmed in the comments). |
| [#112](https://github.com/mparlak/Flee/issues/112) | How to update an expression in CalculationEngine | calc engine | Atoms `x`, `y = sqrt(x)`, `z = y * 3`; `Remove("x")`, re-add `x`, `GetResult("y")` gives `No expression is associated with the name 'y'`. | By design: `CalculationEngine.Remove` also removes all dependents. Could be a feature request for in-place replace. |
| [#104](https://github.com/mparlak/Flee/issues/104) | Cannot add variable with null value | type conversion | `Variables.Add("startDate", null)` gives `ArgumentNullException`. | By design: `Add` needs the value's type (`Utility.AssertNotNull`). `Variables.DefineVariable(name, type)` is the workaround. |
| [#65](https://github.com/mparlak/Flee/issues/65) | Substring | culture | `"\"TEST\".Substring((0,2)"` gives `Could find not function 'Substring(Double)'`. | Not a bug: typo `((` plus a culture with `,` as decimal separator turns `0,2` into a double. |
| [#36](https://github.com/mparlak/Flee/issues/36) | Unexpected token if there is a backslash in a string | parser | `"RegExToColumns(x, \"([A-Za-z]+)\s(...)\")"` fails with unexpected character. | By design: string literals accept only C# style escapes (`\\ \" \' \t \r \n \uXXXX`), so `\s` must be written `\\s`. |

## Open pull requests

- [#117](https://github.com/mparlak/Flee/pull/117) "Support for .NET 9 and .NET 10" (caiosm1005, 2026-05-01, mergeable): adds `net10.0;net9.0;net8.0;net7.0` to `TargetFrameworks`, moves the test project to `net8.0`, and replaces the reflection read of the private `ILGenerator.m_length` field (`Utility.GetILGeneratorLength`) with the public `ILGenerator.ILOffset`. The reflection read returns a null `FieldInfo` on .NET 8 and later (and probably Mono/Unity), which throws NullReferenceException. At `f3b4fe2` this only runs from `FleeILGenerator.ValidateLength`, which is `[Conditional("DEBUG")]`, so it breaks Debug builds of Flee (for example the test suite) and not the Release package. Builds on the closed PR #115 (.NET 8). Check that `ILOffset` is available on the `netstandard2.0` target before taking it.
- [#67](https://github.com/mparlak/Flee/pull/67) "Overload for And, Or operators. Custom error messages" (riskfirst, 2019-06-24, has merge conflicts): targets the old `Flee.Net45` / `Flee.NetStandard20` layout, so it cannot be merged as is. Contents: overloaded `&` / `|` operator support in `AndOrElement` (fixes #54); `ConditionalElement` accepts conditions implicitly convertible to bool; ambiguous overloaded binary operators pick the left operand's method instead of throwing; a bounds check in `BranchManager.IsLongBranch`; `Evaluate` wraps runtime errors in a new `ExpressionEvaluationException` that lists the variable values; `ExpressionCompileException.Arguments`; test path fixes. Some of this (the `IsOptimizablePower` guard, the `CompareElement` cast removal) is already at `f3b4fe2`. Useful as a reference for #54, not as a merge.

## All remaining items

Repro: `partial` = symptom or stack trace but no complete case, `no` = nothing to reproduce, `n/a` = feature request or question.

| Item | Kind | Repro | Summary |
|---|---|---|---|
| [#117](https://github.com/mparlak/Flee/pull/117) | PR | n/a | Adds .NET 7 to 10 targets and replaces the `m_length` reflection with `ILOffset` (see above). |
| [#114](https://github.com/mparlak/Flee/issues/114) | feature request | n/a | Wants a hook to override primitive conversions so `1 > "4"` compiles. |
| [#113](https://github.com/mparlak/Flee/issues/113) | feature request | n/a | LINQ lambdas (`x.Select(p => ...)`) are not supported. |
| [#110](https://github.com/mparlak/Flee/issues/110) | packaging | partial | Asks for .NET 8. Comments: the `m_length` reflection fails on .NET 8, fixed by PR #117. |
| [#109](https://github.com/mparlak/Flee/issues/109) | question | n/a | Wants assignment (`variable1 = variable2*15`); `=` is comparison. |
| [#107](https://github.com/mparlak/Flee/issues/107) | question | partial | Passing a `string[]` to a `params string[]` function fails. Likely user error: the type was imported under a namespace, and the direct-array match path exists at `f3b4fe2`. |
| [#106](https://github.com/mparlak/Flee/issues/106) | question | n/a | Access the calling ExpressionContext inside a custom function. |
| [#103](https://github.com/mparlak/Flee/issues/103) | bug | no | `.ToString(string)` broke in 2.0.0 compared to 1.2.2; no expression given. |
| [#102](https://github.com/mparlak/Flee/issues/102) | feature request | n/a | Pass lambdas or delegates (`item => item > 1`) to custom functions. |
| [#101](https://github.com/mparlak/Flee/issues/101) | bug | partial | OutOfMemoryException in parser lookahead (`LookAheadSet`) on 1.2.2; formula not given. |
| [#100](https://github.com/mparlak/Flee/issues/100) | feature request | n/a | Ternary `a ? b : c` is not in the grammar (`unexpected character '?'`); `IF(a, b, c)` exists. |
| [#98](https://github.com/mparlak/Flee/issues/98) | feature request | n/a | Make `IntegersAsDoubles = true` the default. |
| [#97](https://github.com/mparlak/Flee/issues/97) | question | n/a | `round(0.5)` returns 0 (banker's rounding); asks for AwayFromZero. |
| [#96](https://github.com/mparlak/Flee/issues/96) | other (docs) | n/a | Wiki typo: ExpressionCompileExpression should be ExpressionCompileException. |
| [#95](https://github.com/mparlak/Flee/issues/95) | feature request | n/a | Allow calling void methods for side effects. |
| [#91](https://github.com/mparlak/Flee/issues/91) | other (license) | n/a | Asks which LGPL version applies; original source package says LGPL-2.1-only. |
| [#90](https://github.com/mparlak/Flee/issues/90) | bug (platform) | partial | UWP with .NET Native: `DefaultExpressionOwner` not accessible. Platform limit. |
| [#87](https://github.com/mparlak/Flee/issues/87) | bug (platform) | partial | UWP release build: `PlatformNotSupportedException` for `DynamicMethod`. No Reflection.Emit there. |
| [#85](https://github.com/mparlak/Flee/issues/85) | other (docs) | n/a | Asks for syntax documentation. |
| [#84](https://github.com/mparlak/Flee/issues/84) | bug | partial | Overload scoring used integer division. Regular path now uses float at `f3b4fe2`; `ComputeScoreForParamArray` in `InternalTypes/Miscellaneous.cs` still divides integers. |
| [#82](https://github.com/mparlak/Flee/issues/82) | bug | partial | NullReferenceException on Unity (screenshots only). Probably the same `m_length` reflection as #110 in a Debug build. |
| [#80](https://github.com/mparlak/Flee/issues/80) | bug | partial | IIS process crash on deep expressions (stack overflow in the recursive parser). `ExpressionParser` now derives from the non-recursive `StackParser`, so possibly fixed. |
| [#78](https://github.com/mparlak/Flee/issues/78) | feature request | n/a | Use a `DynamicObject` as expression owner. |
| [#77](https://github.com/mparlak/Flee/issues/77) | packaging | n/a | Asks for a NuGet release with the extension method support. |
| [#74](https://github.com/mparlak/Flee/issues/74) | question | n/a | Wants DivideByZeroException with `IntegersAsDoubles` (doubles give Infinity by design). |
| [#71](https://github.com/mparlak/Flee/issues/71) | feature request | n/a | Import a type whose constructor needs parameters (instance imports). |
| [#69](https://github.com/mparlak/Flee/issues/69) | packaging | partial | Old multi-assembly package (Net45 vs NetStandard20) mismatch; obsolete with the single `Flee` project. |
| [#68](https://github.com/mparlak/Flee/issues/68) | feature request | n/a | Comparisons with `NULL` (for example `NULL <= #2019-07-20#`) should return false. |
| [#67](https://github.com/mparlak/Flee/pull/67) | PR | n/a | And/Or operator overloads and custom error messages on the old layout (see above). |
| [#63](https://github.com/mparlak/Flee/issues/63) | question | n/a | Caching compiled expressions. |
| [#61](https://github.com/mparlak/Flee/issues/61) | bug | partial | ArgumentNullException in CalculationEngine; details only in an attached stack trace. |
| [#59](https://github.com/mparlak/Flee/issues/59) | other (docs) | n/a | List of built-in functions and operators. |
| [#58](https://github.com/mparlak/Flee/issues/58) | question | n/a | Asks for short-circuit OR; AND/OR already short-circuit (`ShortCircuitInfo` in `AndOr.cs`). |
| [#57](https://github.com/mparlak/Flee/issues/57) | bug | partial | "Value was too large or too small for an unsigned byte" in long-branch code; repro only in a zip. Likely the same as #86, fixed. |
| [#53](https://github.com/mparlak/Flee/issues/53) | feature request | n/a | Extension methods (`s.Quote()`); support exists at `f3b4fe2` (`CustomMethodInfo.IsExtensionMethod`). |
| [#48](https://github.com/mparlak/Flee/issues/48) | question | n/a | Is Substring usable? |
| [#47](https://github.com/mparlak/Flee/issues/47) | bug | partial | `in` list only worked with Int32 items due to a hard cast in `CompareElement.Initialize`. Cast removed at `f3b4fe2`. |
| [#44](https://github.com/mparlak/Flee/issues/44) | bug | partial | Ambiguous call error with overloaded methods; no code given. |
| [#43](https://github.com/mparlak/Flee/issues/43) | question | n/a | `"Object.Width = 0.20"` compares and does not assign. |
| [#41](https://github.com/mparlak/Flee/issues/41) | packaging | partial | Versions after 1.0.5 add a System.Reflection binding redirect that breaks test discovery on .NET Framework 4.7.1. |
| [#38](https://github.com/mparlak/Flee/issues/38) | feature request | n/a | Nested variables (`a.f`) or a VariableCollection as a variable. |
| [#37](https://github.com/mparlak/Flee/issues/37) | question | n/a | `Path.GetFileName()` fails while `GetFileName()` works; import namespace question. |
| [#33](https://github.com/mparlak/Flee/issues/33) | question | n/a | Hooks for function entry and exit to change owner state. |
| [#30](https://github.com/mparlak/Flee/issues/30) | bug | partial | IndexOutOfRangeException when compiling long expressions (about 200 chars, nested IF). Likely the long-branch problem, probably fixed with #86. |
| [#23](https://github.com/mparlak/Flee/issues/23) | feature request | partial | Evaluate one compiled expression on different owners in parallel; duplicate of #99. |
| [#21](https://github.com/mparlak/Flee/issues/21) | feature request | n/a | Variable names with dots (`FieldSet1.Q1`). |
| [#18](https://github.com/mparlak/Flee/issues/18) | question | n/a | Get a `Predicate<T>` or `Expression<Func<T,bool>>` from a string. |
| [#16](https://github.com/mparlak/Flee/issues/16) | feature request | n/a | Lambda sub-expressions resolved at runtime (`c => c.ParentKey == Key`). |
| [#12](https://github.com/mparlak/Flee/issues/12) | question | n/a | Compile an expression to a delegate for `customers.Where(...)`. |
| [#5](https://github.com/mparlak/Flee/issues/5) | feature request | n/a | Make the `ImportBase` constructor public for custom import kinds. |
| [#4](https://github.com/mparlak/Flee/issues/4) | question | n/a | Access the ExpressionContext from inside a custom function (same as #106). |
