# Flee expression language reference

This page describes the expressions Flee compiles: literals, operators, conversions and how
members of .NET types are reached. The API that sets up contexts, variables and imports is in
[api-guide.md](api-guide.md).

Every example in the tables below runs as a test (`test/Flee.Test/DocumentationTests`), so the
results shown are what the library returns. The tables use this context:

```csharp
var context = new ExpressionContext();
context.Imports.AddType(typeof(Math));          // Math members without prefix: sqrt(16)
context.Imports.AddType(typeof(Math), "math");  // and with a prefix: math.cos(0)
context.Imports.ImportBuiltinTypes();           // int, double, string, ... for cast and int.MaxValue
context.Variables["a"] = 3;                     // int
context.Variables["b"] = 4.5;                   // double
context.Variables["s"] = "hello";               // string
context.Variables["d"] = new DateTime(2026, 10, 10);
context.Variables["list"] = new List<int> { 1, 2, 3 };
```

The culture is en-GB: `.` is the decimal separator, `,` separates arguments and date literals are
written day/month/year. See [Culture](#culture) for what changes under other cultures.

In the tables, **Result** is the value converted to text with the invariant culture (dates as
`yyyy-MM-dd HH:mm:ss`), **Type** is the .NET type of the result. "error: X" means the expression
does not compile and the `ExpressionCompileException` has reason X; "throws: X" means it compiles
but evaluating it throws X.

## General rules

- Keywords (`and`, `or`, `xor`, `not`, `in`, `if`, `cast`, `true`, `false`, `null`) and names are
  case-insensitive by default; `ExpressionOptions.CaseSensitive` changes that for names.
- Whitespace between tokens is ignored, including line breaks.
- An expression is a single expression. There are no statements, assignments, lambdas, object
  creation (`new`) or string interpolation.
- Types are checked when the expression is compiled. Evaluating a compiled expression runs IL and
  costs one delegate call.

## Literals

| Expression | Result | Type |
| --- | --- | --- |
| `42` | 42 | Int32 |
| `2147483648` | 2147483648 | UInt32 |
| `4294967296` | 4294967296 | Int64 |
| `9223372036854775808` | 9223372036854775808 | UInt64 |
| `100u` | 100 | UInt32 |
| `100L` | 100 | Int64 |
| `100UL` | 100 | UInt64 |
| `0xFF` | 255 | Int32 |
| `0xFFFFFFFF` | 4294967295 | UInt32 |
| `1.5` | 1.5 | Double |
| `.5` | 0.5 | Double |
| `1.5f` | 1.5 | Single |
| `1.4d` | 1.4 | Double |
| `100.25M` | 100.25 | Decimal |
| `1.5e+3` | 1500 | Double |
| `1.5E-2` | 0.015 | Double |
| `1e3` | error: SyntaxError | |
| `'x'` | x | Char |
| `"it\'s"` | it's | String |
| `"quote\"q"` | quote"q | String |
| `"A"` | A | String |
| `"line\r\nbreak".Length` | 11 | Int32 |
| `true` | True | Boolean |
| `False` | False | Boolean |
| `null` | null | |
| `#10/10/2026#` | 2026-10-10 00:00:00 | DateTime |
| `##1.02:03:04#` | 1.02:03:04 | TimeSpan |
| `##02:03#` | 02:03:00 | TimeSpan |

Details:

- **Integers** take the smallest of `Int32`, `UInt32`, `Int64`, `UInt64` that holds the value.
  The suffixes `u`, `l`, `ul` (or `lu`) choose the type. Hexadecimal literals start with `0x`.
- **Reals** need a decimal point: `1.5`, `.5`. An exponent needs a sign (`1.5e+3`, not `1.5e3`
  or `1e3`). Without a suffix the type is `Double`, or what `ExpressionOptions.RealLiteralDataType`
  says. `f` gives `Single`, `d` `Double`, `m` `Decimal`. With
  `ExpressionOptions.IntegersAsDoubles`, integer literals become `Double` too.
- **Strings** use double quotes, **chars** single quotes. Escapes: `\"`, `\'`, `\t`, `\r`, `\n`,
  `\uXXXX`. A backslash itself (`\\`) is not accepted inside a string.
- **Dates** are written between `#` in the format of `ExpressionParserOptions.DateTimeFormat`,
  which defaults to the current culture's short date pattern (dd/MM/yyyy here).
- **Time spans** are written `##[d.]hh:mm[:ss[.fffffff]]#`.
- A literal out of its type's range is a compile error with reason `ConstantOverflow`
  (`1.7976931348623157E+309`).

## Operators

From lowest to highest precedence:

| Level | Operators | Meaning |
| --- | --- | --- |
| 1 | `xor` | Exclusive or: logical on `Boolean`, bitwise on integers |
| 2 | `or` | Logical or (short-circuit) on `Boolean`, bitwise on integers |
| 3 | `and` | Logical and (short-circuit) on `Boolean`, bitwise on integers |
| 4 | `not` | Logical not on `Boolean`, bitwise complement on integers (prefix, once) |
| 5 | `in` | Membership in a list or collection |
| 6 | `=` `<>` `<` `>` `<=` `>=` | Comparison |
| 7 | `<<` `>>` | Shift |
| 8 | `+` `-` | Addition, subtraction, string concatenation |
| 9 | `*` `/` `%` | Multiplication, division, remainder |
| 10 | `^` | Power |
| 11 | `-` | Negation (prefix, once) |
| 12 | `.` `[]` `()` | Member access, indexing, call |

| Expression | Result | Type |
| --- | --- | --- |
| `1 + 2 * 3` | 7 | Int32 |
| `(1 + 2) * 3` | 9 | Int32 |
| `7 / 2` | 3 | Int32 |
| `7.0 / 2` | 3.5 | Double |
| `7 % 3` | 1 | Int32 |
| `2 ^ 3` | 8 | Int32 |
| `2 ^ 0.5` | 1.4142135623730951 | Double |
| `-2 ^ 2` | 4 | Int32 |
| `3 - -2` | 5 | Int32 |
| `--a` | error: SyntaxError | |
| `1 << 4` | 16 | Int32 |
| `256 >> 2` | 64 | Int32 |
| `5 and 3` | 1 | Int32 |
| `5 or 2` | 7 | Int32 |
| `5 xor 1` | 4 | Int32 |
| `not 5` | -6 | Int32 |
| `true and false` | False | Boolean |
| `true or false and false` | True | Boolean |
| `not true or true` | True | Boolean |
| `not not true` | error: SyntaxError | |
| `true and 1` | error: TypeMismatch | |
| `1 = 1 or 1 / 0 = 0` | True | Boolean |
| `3 / 0` | throws: DivideByZeroException | |
| `3.0 / 0` | Infinity | Double |
| `2000000000 * 2` | -294967296 | Int32 |
| `2000000000L * 2` | 4000000000 | Int64 |

Things to know:

- **Negation binds tighter than power**: `-2 ^ 2` is `(-2) ^ 2 = 4`, unlike in mathematics.
- **Integer division** truncates; dividing an integer by zero throws `DivideByZeroException` when
  the expression is evaluated, not when it is compiled.
- **Overflow** of integer arithmetic wraps around unless `ExpressionOptions.Checked` is on; then it
  throws `OverflowException`.
- `and` and `or` short-circuit on `Boolean` operands. On integers they work bit by bit.
- `^` on two integers gives an integer when the exponent is an integer literal, otherwise `Double`.
- `not` and the minus sign cannot be repeated (`not not x`, `--x`); use parentheses: `-(-a)`.

## Comparison

| Expression | Result | Type |
| --- | --- | --- |
| `1 = 1` | True | Boolean |
| `1 <> 2` | True | Boolean |
| `1.0 = 1` | True | Boolean |
| `1 + 2 = 3` | True | Boolean |
| `5 > 3 = true` | True | Boolean |
| `"abc" = "ABC"` | False | Boolean |
| `"a" < "b"` | error: TypeMismatch | |
| `'a' < 'b'` | True | Boolean |
| `'a' = "a"` | error: TypeMismatch | |
| `s = null` | False | Boolean |
| `null = null` | True | Boolean |
| `d > #01/01/2026#` | True | Boolean |

- Numbers of different types compare after conversion to a common type (see
  [Type conversion](#type-conversion)).
- Strings support `=` and `<>` only, with `ExpressionOptions.StringComparison` (default
  `Ordinal`, so case matters). Use `string.Compare` or `CompareTo` for ordering.
- `=` is equality, not assignment.

## Strings

| Expression | Result | Type |
| --- | --- | --- |
| `s + " world"` | hello world | String |
| `s + 1` | hello1 | String |
| `1 + s` | 1hello | String |
| `"Hello" + 'X'` | HelloX | String |
| `"a" + null` | a | String |
| `s.Length` | 5 | Int32 |
| `s.ToUpper()` | HELLO | String |
| `s.Substring(1, 3)` | ell | String |
| `s.Replace("l", "L")` | heLLo | String |
| `s[0]` | h | Char |
| `"abc".Contains("b")` | True | Boolean |

`+` concatenates as soon as one operand is a string; the other is converted with `ToString`.

## Conditional: `if`

`if(condition, whenTrue, whenFalse)` evaluates only the branch it needs. Both branches are
converted to a common type.

| Expression | Result | Type |
| --- | --- | --- |
| `if(a > 2, "big", "small")` | big | String |
| `if(a > 2, 1, 2.5)` | 1 | Double |
| `if(true, null, "x")` | null | |
| `if(s = "hello", d, #01/01/2000#)` | 2026-10-10 00:00:00 | DateTime |

## Type conversion

**Implicit** conversions follow C#: integers widen to larger integers, to `Single`, `Double` and
`Decimal`; `Single` widens to `Double`; `char` widens to integers; user-defined implicit
operators are used. Mixed arithmetic converts both operands to the wider type.

| Expression | Result | Type |
| --- | --- | --- |
| `a + b` | 7.5 | Double |
| `a * 2` | 6 | Int32 |
| `10L * 3` | 30 | Int64 |
| `1 + 2.5M` | 3.5 | Decimal |
| `1.5 + 2.5M` | error: TypeMismatch | |
| `'a' + 1` | 98 | Int32 |

As in C#, `double` and `decimal` do not mix without a cast.

**Explicit** conversions use `cast(value, type)`. The type is a built-in name (`boolean`, `byte`,
`sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `single`, `double`, `decimal`,
`char`, `object`, `string`, available after `ImportBuiltinTypes()`) or an imported type; arrays
are written `type[]`.

| Expression | Result | Type |
| --- | --- | --- |
| `cast(b, int)` | 4 | Int32 |
| `cast(3.7, int)` | 3 | Int32 |
| `cast(a, double) / 2` | 1.5 | Double |
| `cast(1.5, decimal) + 2.5M` | 4.0 | Decimal |
| `cast(65, char)` | A | Char |
| `cast('A', int)` | 65 | Int32 |
| `cast(255, byte)` | 255 | Byte |
| `cast(256, byte)` | 0 | Byte |
| `cast(-1, uint)` | 4294967295 | UInt32 |
| `cast(s, object)` | hello | String |
| `cast(1, string)` | error: InvalidExplicitCast | |

Numeric casts truncate toward zero and wrap around on overflow, unless
`ExpressionOptions.Checked` is on; then an out-of-range value throws `OverflowException`. Use
`ToString()` to turn a value into a string.

## Membership: `in`

`value in (item, item, ...)` tests against a list; `value in collection` against anything that
implements `ICollection<T>`, `IDictionary<K,V>` (keys), `IList` or `IDictionary`.

| Expression | Result | Type |
| --- | --- | --- |
| `a in (1, 2, 3)` | True | Boolean |
| `"x" in ("a", "b")` | False | Boolean |
| `2 in list` | True | Boolean |
| `1 in (1, 2) and true` | True | Boolean |

## Names: variables, members and functions

A name without a preceding value is resolved in this order:

1. a field or property of the **expression owner** (the object passed to the
   `ExpressionContext` constructor; non-public ones only if `ExpressionOptions.OwnerMemberAccess`
   allows) or a static field or property of an **imported** type;
2. a **variable** of the context (`context.Variables`), including on-demand variables;
3. an **atom** of the calculation engine, for expressions added to a `CalculationEngine`.

A name followed by `(...)` is a method: an owner method, an imported static method or an
on-demand function.

After a value, `.` reaches its instance members, `[...]` its indexer or array elements, and
`name(...)` calls a method. Overloads are chosen as in C#, with one difference: where C# reports a
call as ambiguous because two conversions are equally good, Flee may pick one (see
`doc/deferred.md`, D-12).

| Expression | Result | Type |
| --- | --- | --- |
| `sqrt(16)` | 4 | Double |
| `math.cos(0)` | 1 | Double |
| `max(a, 10)` | 10 | Int32 |
| `min(1.5, 2)` | 1.5 | Double |
| `round(2.567, 2)` | 2.57 | Double |
| `pow(2, 10)` | 1024 | Double |
| `abs(-3)` | 3 | Int32 |
| `int.MaxValue` | 2147483647 | Int32 |
| `uint.maxvalue` | 4294967295 | UInt32 |
| `int.Parse("42")` | 42 | Int32 |
| `string.Concat("a", "b")` | ab | String |
| `d.Year` | 2026 | Int32 |
| `d.AddDays(1)` | 2026-10-11 00:00:00 | DateTime |
| `d + ##1.00:00#` | 2026-10-11 00:00:00 | DateTime |
| `d - #01/01/2026#` | 282.00:00:00 | TimeSpan |
| `#10/10/2026#.DayOfWeek` | Saturday | DayOfWeek |
| `list[1]` | 2 | Int32 |
| `list.Count` | 3 | Int32 |
| `list.Contains(2)` | True | Boolean |
| `a.ToString()` | 3 | String |
| `a.GetType().Name` | Int32 | String |
| `zzz + 1` | error: UndefinedName | |

Only types that are imported, or that are reached through a value, can be used; nothing else of
.NET is visible to an expression.

## Culture

Three parser settings default to the **current culture** of the thread that creates the
`ExpressionContext`:

| Setting | en-GB | de-DE | Option |
| --- | --- | --- | --- |
| Decimal separator | `.` | `,` | `ExpressionParserOptions.DecimalSeparator` |
| Argument separator | `,` | `;` | `ExpressionParserOptions.FunctionArgumentSeparator` |
| Date literal format | `dd/MM/yyyy` | `dd.MM.yyyy` | `ExpressionParserOptions.DateTimeFormat` |

So under de-DE, `max(1,5; 2)` is valid and `max(1.5, 2)` is not. To make expressions independent
of the machine, set the culture explicitly before compiling:

```csharp
context.Options.ParseCulture = CultureInfo.InvariantCulture;   // '.', ',' and MM/dd/yyyy
```

Setting `ParseCulture` updates the three settings and rebuilds the parser; the individual
settings can also be changed afterwards (then call `context.ParserOptions.RecreateParser()`).

Keywords are recognised the same way under every culture (upstream issue #105 is fixed in this
fork).
