# Flee (klopsquark fork)

Flee, the Fast Lightweight Expression Evaluator, is a .NET library that evaluates expressions given
as text at run time, such as `sqrt(a^2 + b^2)` or `price * quantity > 100 and country = "DE"`. It
type-checks each expression and compiles it to IL, so evaluating a compiled expression costs one
delegate call.

This wiki is the handbook of the fork maintained at
[klopsquark/Flee](https://github.com/klopsquark/Flee). The fork continues
[mparlak/Flee](https://github.com/mparlak/Flee), which has been inactive since Flee 2.0.0 (March
2022). It is source-compatible with Flee 2.0.0, builds on current .NET, is covered by about 2,000
tests, and is documented here.

## Features

- Compiles expressions to IL with `DynamicMethod`: fast evaluation, nothing left in memory once an
  expression is no longer used.
- Strongly typed: arithmetic, comparison, logical, bitwise and shift operators with C#'s type
  promotion, plus `if(...)`, `cast(...)` and `in`.
- Literals for integers (signed, unsigned, 64-bit, hex), reals (double, single, decimal), strings,
  chars, booleans, `null`, dates and time spans.
- Variables of any type, including compiled expressions as variables; on-demand variables and
  functions through events.
- Imported static methods and types, instance members of values, indexers and arrays, extension
  methods, user-defined operators.
- Expression owners: an expression behaves like a method of an object you choose and may use its
  private members.
- A calculation engine that tracks dependencies between named expressions and recalculates in
  natural order.
- Fine-grained control over what an expression can reach, culture-aware number and argument syntax,
  optional overflow checking.

## Where to start

| You want to | Read |
| --- | --- |
| Evaluate your first expression | [Getting Started](Getting-Started) |
| See common tasks solved | [Examples](Examples) |
| Know what the expression language accepts | [Language Reference](Language-Reference) |
| Use the C# API in depth | [API Guide](API-Guide) |
| Know what Flee cannot do | [Limitations](Limitations) |
| Upgrade from Flee 2.0.0 | [Migration Guide](Migration-Guide) and the [Changelog](Changelog) |
| Understand how it works inside | [Internals](Internals) |
| Know how fast it is | [Benchmarks](Benchmarks) |
| Build, test or change the library | [Development](Development) |
| Know what has been done and what is open | [Project Status](Project-Status) and [Deferred Work](Deferred-Work) |
| Check an issue reported upstream | [Upstream Issues](Upstream-Issues) and [Upstream Wiki](Upstream-Wiki) |

## Status

Version 2.6.0 is prepared and not yet released. The package keeps the ID `Flee` and is published to
a private feed only, not to nuget.org, where `Flee` belongs to upstream. Details:
[Project Status](Project-Status).

## How this fork was made

Most of the work in this fork was done by Claude, Anthropic's AI coding assistant, working as an
agent: analysing the code, writing tests, fixing bugs, cleaning up, measuring and writing this
documentation. klopsquark, the maintainer, set the goals and priorities, made the decisions,
reviewed the results and merged them. Every change is recorded with its reason and how it was
verified (`doc/CHANGE-RATIONALE.md`), and behaviour is pinned by about 2,000 tests, so the work can
be checked independently of who did it.

## Licence

LGPL 2.1 or later. Flee was written by Eugene Ciloci in VB.NET (2007), converted to C# and
maintained by the contributors of mparlak/Flee; this fork is maintained by klopsquark.
