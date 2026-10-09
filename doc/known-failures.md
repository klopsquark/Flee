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
| `wrong-reason` | 62 | test expectation | Rejected correctly, but with a different `CompileExceptionReason` than the script says |
| `real-overflow-undetected` | 7 | runtime change | Out-of-range real literals become infinity instead of `ConstantOverflow` |
| `in-collection` | 7 | bug | `x in list` rejects non-generic `IList` and `Hashtable` |
| `crash` | 4 | bug | `GetType()` on a value-type field: the process dies |
| `debug-il-length` | 2 | bug, Debug only | IL length self-check fails for hex `Int64`/`UInt64` literals |
| `script-error` | 1 | test data | The script line is a valid expression in the invalid-expressions file |
| **Total** | **150** | | of 1,756 cases (1,606 pass) |

In a Release build the two `debug-il-length` cases pass, so the totals are 148 and 1,608.

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

### wrong-reason (62)

These expressions are rejected, as the script expects, but with another
`CompileExceptionReason`. The original harness parsed the expected reason and never compared it,
so the script's reasons were never checked. Main groups:

| Group | Script says | Library says | Example |
| --- | --- | --- | --- |
| No matching overload or function | TypeMismatch | UndefinedName | `math.cos("abc")` |
| Unknown type in `cast` | InvalidExplicitCast | UndefinedName | `cast(int32a, Guid)` (type not imported) |
| Integer literal too large for the result type | ConstantOverflow | TypeMismatch | `2147483648` as `Int32` |
| Wrong argument count for `if`, malformed tokens | TypeMismatch | SyntaxError | `if("a")`, `StringA.$a` |
| Indexer not found | TypeMismatch | UndefinedName | `bytea[0]` |
| Bad date or time-span literal | SyntaxError / InvalidFormat | InvalidFormat / SyntaxError | `#12/13/2008#` |

Phase 4 decides per group whether the script or the library is right. Most of these look like
the library's reason is reasonable and the script is out of date.

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

Related, from the upstream issue triage: on .NET 8 and later every Debug build fails this check,
because `Utility.GetILGeneratorLength` reads a private field of `ILGenerator` that no longer
exists. That matters for Phase 3.

### script-error (1)

`InvalidExpressions.txt` line 333, `System.int32;KeyboardA.structA["s", 100, 45.5];200`, has a
result value where the reason belongs. It looks like a line from `ValidExpressions.txt`.

## Other findings from Phase 1

- **Culture.** Flee's parser defaults (decimal separator, argument separator, date format) follow
  the current culture. On a German machine `1.5` and `max(1, 2)` do not parse; `1,5` and
  `max(1; 2)` do. All 8 tests that failed on the maintainer's machine failed for this reason.
  The suite now runs under en-GB, and `CultureTests` pins the behaviour for de-DE, en-GB, the
  invariant culture and Turkish (upstream #105: `IF` and `IN` fail to parse in tr-TR).
- **en-CA changed.** The original harness assumed en-CA as .NET Framework defined it
  (dd/MM/yyyy). On .NET 5 and later en-CA uses yyyy-MM-dd, which is why the suite uses en-GB.
