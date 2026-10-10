# Known failures

Script cases that fail on the unchanged library (upstream `f3b4fe2`), as recorded in Phase 1.
The authoritative list is `test/Flee.Test/TestScripts/KnownFailures.txt`; this page explains it.
Phase 4 works through it: each category is fixed, or kept and documented as a limitation.

Measured on 2026-10-09 with net6.0 on Windows, test culture en-GB. Since Phase 3 the tests run
on net8.0 and net10.0 with exactly the same list.

## Overview

| Category | Cases | Kind | Short cause |
| --- | ---: | --- | --- |
| `unsigned-literal` | 67 | bug | UInt32/UInt64 constants above the signed maximum cannot be emitted |
| `real-overflow-undetected` | 7 | runtime change | Out-of-range real literals become infinity instead of `ConstantOverflow` |
| `in-collection` | 7 | bug | `x in list` rejects non-generic `IList` and `Hashtable` |
| `crash` | 4 | bug | `GetType()` on a value-type field: the process dies |
| `debug-il-length` | 2 | bug, Debug only | IL length self-check fails for hex `Int64`/`UInt64` literals |
| **Total** | **87** | | of 1,756 cases (1,669 pass) |

In a Release build the two `debug-il-length` cases pass, so the totals are 85 and 1,671.

The list started with 150 cases. 63 left it in Phase 4 when the expected reasons in
`InvalidExpressions.txt` were updated (categories `wrong-reason` and `script-error`, see the last
section).

## Categories

### unsigned-literal (67)

Any `uint` value above `Int32.MaxValue` or `ulong` value above `Int64.MaxValue` fails to compile
with `OverflowException`, whether written as a literal (`4294967295U`, `0xFFFFFFFF`) or reached
through a constant field (`uint.maxvalue`). The literal elements convert the value with a checked
conversion before emitting it:

- `src/Flee/ExpressionElements/Literals/Integral/UInt32.cs:29`: `Convert.ToInt32(_myValue)`
- `src/Flee/ExpressionElements/Literals/Integral/UInt64.cs:29`: `Convert.ToInt64(_myValue)`

An unchecked reinterpretation (`unchecked((int)_myValue)`) is what the IL needs. Upstream #83
fixed the same pattern in one place (`LiteralElement`), not in these two. All CheckedTests
entries in this category fail for the same reason: the compile error surfaces as an
`OverflowException` that the test reads as an arithmetic overflow.

### real-overflow-undetected (7)

`1.7976931348623157E+309` (and the single-precision equivalents) compile and evaluate to
infinity. Since .NET Core 3.0, `double.Parse` and `float.Parse` return infinity for out-of-range
input instead of throwing `OverflowException`, so the `catch` in
`src/Flee/ExpressionElements/Literals/Real/Double.cs:32` and `Single.cs:31` never runs. A fix
checks for infinity after parsing.

### in-collection (7)

`"a" in list` with a non-generic `IList`, and `100 in Dict` with a `Hashtable`, fail with
"Search argument type is not a known collection type". The fallback check compares against the
open generic types `IList<>` and `IDictionary<,>`, which `IsAssignableFrom` never matches:
`src/Flee/ExpressionElements/In.cs:99` and `:103`. The comment above it ("a regular IList or
IDictionary") suggests the non-generic interfaces were meant, probably lost in the VB-to-C#
conversion (inferred, not checked against the VB source). The existing test
`IN_OperatorTest` only covers generic collections.

### crash (4)

`DateTimeA.GetType().Name` (an instance field of type `DateTime`) crashes the test process with
an access violation. `mouse.shareddt.gettype().name` (a static `DateTime` field) threw
`InvalidProgramException` on .NET 6; on .NET 8 and 10 it crashes the process with an internal
CLR error, so it moved from its own category `valuetype-gettype` to `crash` in Phase 3. Both
point at calling `Object.GetType()` on an unboxed value type. Not analysed further yet. Crash
cases are never run, because they would take the whole test run down.

### debug-il-length (2)

`0xFFFFFFFFL` and `0xFFFFFFFFUL` trip `Debug.Assert(Length == ILGeneratorLength)` in
`src/Flee/InternalTypes/FleeILGenerator.cs:260`: Flee's own IL length bookkeeping disagrees
with the real IL for these literals. Release builds skip the check and evaluate correctly.

Related: on .NET 8 and later every Debug build failed this check, because
`Utility.GetILGeneratorLength` read a private field of `ILGenerator` that no longer exists. Fixed
in Phase 3 (R-010).

## Other findings from Phase 1

- **Culture.** Flee's parser defaults (decimal separator, argument separator, date format) follow
  the current culture. On a German machine `1.5` and `max(1, 2)` do not parse; `1,5` and
  `max(1; 2)` do. All 8 tests that failed on the maintainer's machine failed for this reason.
  The suite now runs under en-GB, and `CultureTests` pins the behaviour for de-DE, en-GB, the
  invariant culture and Turkish (upstream #105: `IF` and `IN` fail to parse in tr-TR).
- **en-CA changed.** The original harness assumed en-CA as .NET Framework defined it
  (dd/MM/yyyy). On .NET 5 and later en-CA uses yyyy-MM-dd, which is why the suite uses en-GB.

## Expected reasons updated in Phase 4

The original harness never compared the `CompileExceptionReason` in `InvalidExpressions.txt`
with the actual one, so 63 lines carried reasons the library has not produced for a long time,
or perhaps ever. In every case the expression is still rejected; only the reason differs. The
maintainer decided (2026-10-10) to update the script to today's reasons and record the
difference here. The library was not changed.

| Line | Expression | Reason in the 2007 script | Reason today | Why |
| ---: | --- | --- | --- | --- |
| 3 | `2147483648` | ConstantOverflow | TypeMismatch | Literal too big for the result type: typed as the next larger integer type, then not convertible |
| 4 | `2147483649` | ConstantOverflow | TypeMismatch | Literal too big for the result type: typed as the next larger integer type, then not convertible |
| 5 | `400000000000` | ConstantOverflow | TypeMismatch | Literal too big for the result type: typed as the next larger integer type, then not convertible |
| 6 | `-2147483649` | ConstantOverflow | TypeMismatch | Literal too big for the result type: typed as the next larger integer type, then not convertible |
| 7 | `-3000000000` | ConstantOverflow | TypeMismatch | Literal too big for the result type: typed as the next larger integer type, then not convertible |
| 18 | `4294967296U` | ConstantOverflow | TypeMismatch | Literal too big for the result type: typed as the next larger integer type, then not convertible |
| 19 | `10000000000U` | ConstantOverflow | TypeMismatch | Literal too big for the result type: typed as the next larger integer type, then not convertible |
| 21 | `9223372036854775808L` | ConstantOverflow | TypeMismatch | Literal too big for the result type: typed as the next larger integer type, then not convertible |
| 22 | `-9223372036854775809L` | ConstantOverflow | TypeMismatch | Negating the UInt64 literal is not defined |
| 23 | `10000000000000000000L` | ConstantOverflow | TypeMismatch | Literal too big for the result type: typed as the next larger integer type, then not convertible |
| 24 | `-10000000000000000000L` | ConstantOverflow | TypeMismatch | Negating the UInt64 literal is not defined |
| 61 | `-uint64.maxvalue` | TypeMismatch | UndefinedName | No such member |
| 88 | `uint64.minvalue = int64.minvalue` | TypeMismatch | UndefinedName | No such member |
| 140 | `string.length` | TypeMismatch | UndefinedName | No such member |
| 143 | `string.alignConst` | TypeMismatch | UndefinedName | No such member |
| 152 | `math.max(1,2,3)` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 153 | `math.cos()` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 159 | `math.cos("abc")` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 160 | `math.cos(true)` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 161 | `string.copy(100)` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 162 | `string.copy(100)` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 163 | `Doubleit(3.45)` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 164 | `Doubleit()` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 169 | `if()` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 170 | `if("a")` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 171 | `if("a",100)` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 173 | `if("a",100,200,300)` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 189 | `cast(100, blah)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 197 | `cast(datetimea, Guid)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 198 | `cast(int32a, Guid)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 201 | `cast(22.34, Version)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 204 | `cast("abc", Guid)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 205 | `cast(ICollectionA, Guid)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 208 | `cast("abc",Version)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 210 | `cast("abc", IDisposable)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 217 | `cast("abc", Array)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 221 | `cast(stringarr, Version)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 223 | `cast(stringarr, iComparable)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 225 | `cast(arraya, IComparable)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 230 | `cast(delegateanull, Icomparable)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 234 | `cast(delegatea, Icomparable)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 241 | `cast("abc", System.text.encoding)` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 242 | `cast(stringarr, System.text.encoding[])` | InvalidExplicitCast | UndefinedName | Cast target type unknown in this context (not imported) |
| 247 | `bytea[0]` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 252 | `stringdict[100]` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 260 | `cast('^',string)` | TypeMismatch | InvalidExplicitCast | Rejected as an invalid cast |
| 277 | `"\z"` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 278 | `"\Uzzzz"` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 298 | `cast(null, int)` | TypeMismatch | InvalidExplicitCast | Rejected as an invalid cast |
| 300 | `Math.cos(null)` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 305 | `sum4(100.24, 13.4)` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 306 | `sum4(100, "a")` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 307 | `sum4("a")` | TypeMismatch | UndefinedName | No function or indexer overload matches the arguments |
| 309 | `StringA.$a` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 310 | `$a.$b` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 312 | `100 in 100` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 316 | `'a' in ()` | TypeMismatch | SyntaxError | Rejected by the parser before type checking |
| 323 | `#123/11/2008#` | SyntaxError | InvalidFormat | Malformed date or time-span literal: reported by the parser or by the literal |
| 324 | `#12/13/2008#` | SyntaxError | InvalidFormat | Malformed date or time-span literal: reported by the parser or by the literal |
| 325 | `#45/06/2008#` | SyntaxError | InvalidFormat | Malformed date or time-span literal: reported by the parser or by the literal |
| 327 | `##1.44:30#` | SyntaxError | InvalidFormat | Malformed date or time-span literal: reported by the parser or by the literal |
| 331 | `##0.00:44.12345678#` | InvalidFormat | SyntaxError | Malformed date or time-span literal: reported by the parser or by the literal |
| 333 | `KeyboardA.structA["s", 100, 45.5]` | 200 | UndefinedName | The script line had a result value where the reason belongs |
