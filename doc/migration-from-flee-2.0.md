# Migrating from Flee 2.0.0

The fork is source-compatible with Flee 2.0.0 (upstream `mparlak/Flee`): code that compiled
against 2.0.0 compiles against the fork. This page lists what you may notice anyway. Every item
links to its entry in [CHANGE-RATIONALE.md](CHANGE-RATIONALE.md).

## Package

- **ID and namespace** stay `Flee`. The fork's packages start at version **2.6.0** and are
  published to a private feed only, not to nuget.org (R-001).
- **Targets:** netstandard2.0, netstandard2.1, net8.0 and net10.0 (R-011). Upstream shipped net5.0
  and net6.0 builds; apps on .NET 5 to 7 now get the netstandard2.1 build, which behaves the same.
- netstandard2.0 still depends on the `System.Reflection.Emit` 4.7.0 packages; the other targets
  have no dependencies (R-012).
- The package carries an SPDX licence expression (LGPL-2.1-or-later), the README, symbols and Source
  Link (R-014).
- The package ships XML documentation (`Flee.xml`) for IntelliSense. It no longer adds
  `Resources/DocComments.xml` to your project; delete that file if an earlier version left it
  there (R-049).

## Compile-time differences for your code

- **`ExpressionOptions.EmitToAssembly`** is marked `[Obsolete]` and does nothing (R-015). Code that
  sets it gets warning CS0618; remove the line.
- **Nullable annotations** (R-038). If your project enables nullable reference types, you may see
  new warnings where you use a result that can be null, for example
  `(int)expression.Evaluate()` (`Evaluate()` returns `object?`). Nothing changes at run time and
  nothing changes for projects without nullable enabled.

## Behaviour changes

All of these fix bugs; most turn an exception or a crash into a working result.

| Area | Flee 2.0.0 | Fork | Entry |
| --- | --- | --- | --- |
| `uint`/`ulong` constants above the signed maximum (`4294967295U`, `uint.maxvalue`) | `OverflowException` while compiling | Compile and evaluate correctly | R-021 |
| `x in list` with `ArrayList`, `Hashtable` and other non-generic collections | Compile error "not a known collection type" | Works | R-022 |
| Real literals out of range (`1.7976931348623157E+309`) on .NET Core | Compiled to infinity | Compile error `ConstantOverflow`, as on .NET Framework | R-023 |
| `GetType()` on a value-type member (`DateTimeA.GetType()`) | Crashed the process or `InvalidProgramException` | Works | R-024 |
| Member access on a `DateTime`/`TimeSpan` calculation-engine atom (#64, #111) | `NullReferenceException`, `InvalidProgramException` or crash | Works | R-026 |
| Unknown name in a calculation-engine expression | `ArgumentNullException` | `ExpressionCompileException` with `UndefinedName` | R-039 |
| Failed `CalculationEngine.Add` | Left the name behind; later `Recalculate` threw | Engine unchanged after the failure | R-040 |
| Upper-case `IF`, `IN`, `NOT` under a Turkish culture (#105) | Syntax error | Parse | R-042 |
| `cast(x, char)` | No conversion: no wrapping, no overflow check | Converts like C# `(char)`; `Checked` throws on overflow | R-045 |
| Error messages of three element types | Missing the element name | Include it | R-027 |
| Comparing `NamespaceImport` objects not added to a context | `NullReferenceException` | Works | R-041 |
| Debug builds of Flee on .NET 8 and later | Every compile failed | Work | R-010 |
| Broken internal invariants (should never happen) | Wrong IL, failing later | `InvalidOperationException("Flee internal error: ...")` | R-035 |
| Syntax error in `BatchLoader.Add`, `SimpleCalcEngine.AddDynamic`/`AddGeneric` | Internal `ParserLogException` | `ExpressionCompileException` with `SyntaxError` | R-051 |
| Batch expression with an unknown name | `KeyNotFoundException` from `BatchLoad` | `BatchLoadCompileException`, inner reason `UndefinedName` | R-052 |
| Reading a `DefineVariable` variable before setting it | `NullReferenceException` for value types | The type's default value | R-053 |
| Changing options, parser options or imports of a cloned context | Also changed the original | Changes the clone only | R-054 |
| `RecreateParser` after changing separators | Calculation engines kept parsing names with the old separators | Use the new ones | R-054 |
| Failed `SimpleCalcEngine` add | Cleared `Context.Variables` | Keeps them | R-055 |

## Performance

- On .NET 10 the first evaluation of an expression that reads variables is about 20 times faster
  than with an unmodified Flee 2.0.0 on .NET 10, and loading a calculation engine about 30 times
  (R-019). Steady-state evaluation is unchanged or faster.
- Compared with Flee 2.0.0 on .NET 6, the fork on .NET 10 parses, compiles and evaluates about 45 %
  faster (`benchmarks/results/phase3-runtimes`).

## Unchanged on purpose

- Overload resolution, including cases C# calls ambiguous (`doc/deferred.md`, D-12).
- Parser defaults that follow the current culture (decimal and argument separators, date format).
- The public API surface: every type and member of 2.0.0 is still there with the same signature.
