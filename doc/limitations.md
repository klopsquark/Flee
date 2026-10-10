# Limitations

What Flee does not do, or does differently from C#. Items that may change later are listed in
[deferred.md](deferred.md) with their id.

## Platform

- **Runtime code generation.** Flee compiles every expression to IL with `DynamicMethod`. That
  needs a runtime with a JIT: it does not work under NativeAOT, on iOS (no JIT allowed), or in other
  environments that forbid dynamic code. There is no interpreter fallback.
- **Targets.** netstandard2.0, netstandard2.1, net8.0 and net10.0. .NET Framework 4.6.1+ and
  .NET 5 to 7 use the netstandard builds.
- **Saving generated code.** `ExpressionOptions.EmitToAssembly` has no effect (D-06).
- **First evaluation.** The runtime JIT-compiles each expression on its first call. On .NET 10
  that costs more than on .NET 8 for some expressions (D-07); compile and call expressions once
  at startup if the first call must be fast.

## Language

- One expression, no statements: no assignment, no loops, no local variables, no lambdas, no
  object creation (`new`), no string interpolation, no `switch` or `??`.
- `-2 ^ 2` is 4: the minus sign binds tighter than the power operator.
- Strings can be compared with `=` and `<>` only, not ordered with `<` or `>`.
- `not` and the minus sign cannot be repeated without parentheses (`not not x`, `--x`).
- Real literals with an exponent need a decimal point and a sign: `1.5e+3`, not `1e3`.
- A backslash cannot be written inside a string literal (`\\` is not an escape).
- Identifiers are ASCII letters, digits and underscores (D-01).
- Where C# reports an overloaded call as ambiguous, Flee may pick one overload (D-12).
- User-defined `&` and `|` operators are not used for `and` and `or` (D-10).

## Culture

The decimal separator, the argument separator and the date literal format default to the current
culture of the thread that creates the context (see the language reference, "Culture"). The same
expression text can therefore compile on one machine and fail on another. Set
`ExpressionOptions.ParseCulture` explicitly if expressions travel between machines.

## Threads

- One compiled expression must not be evaluated with different owners from several threads at
  once: the owner is stored in the expression (D-11).
- Variables are shared between a context and the expressions compiled from it. Changing a
  variable while another thread evaluates is not synchronised.
- Compiling from several threads with one shared context is not verified; use one context per
  thread.

## Variables

- A variable keeps the type it was created with. Assigning a value of another type through the
  indexer is not rejected and fails later (D-09); remove and add the variable instead.
