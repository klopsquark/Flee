# Internals

How Flee turns expression text into a delegate, for whoever works on the library or wants to know
what happens behind the API. Figures and the history of the code base are in
[flee-project-findings.md](flee-project-findings.md).

## Overview

```
text ──parse──▶ element tree ──emit──▶ IL in a DynamicMethod ──▶ delegate ──▶ Evaluate()
       (Parsing)  (ExpressionElements)   (FleeILGenerator)        ExpressionEvaluator<T>
```

Everything that can be decided at compile time is: names are resolved, overloads chosen, types
checked and conversions inserted while the expression is compiled. Evaluating is one delegate call
into IL that the runtime has JIT-compiled like any other method. That is why Flee evaluates fast and
compiles comparatively slowly (tens of microseconds to milliseconds, see `benchmarks/results`).

| Folder | Content |
| --- | --- |
| `src/Flee/PublicTypes` | `ExpressionContext`, options, imports, `VariableCollection`, events, exceptions |
| `src/Flee/InternalTypes` | `Expression<T>`, `FleeILGenerator`, `BranchManager`, `ImplicitConverter`, binders, variable classes |
| `src/Flee/ExpressionElements` | One class per language construct, each emitting its own IL |
| `src/Flee/CalcEngine` | `CalculationEngine`, `SimpleCalcEngine`, `BatchLoader`, `DependencyManager` |
| `src/Flee/Parsing` | Bundled Grammatica runtime, the generated parser and Flee's analyzer |
| `src/Flee/Resources` | Error messages and element names (`.resx`) |

## Compiling an expression

`ExpressionContext.CompileDynamic` and `CompileGeneric<T>` create an `Expression<T>`
(`InternalTypes/Expression.cs`); its constructor does all the work:

1. **Copy the context.** `ExpressionContext.CloneInternal(false)` copies the options, the parser
   options and the imports; the variables stay shared. Later changes to the original context do
   not affect the compiled expression, and changes to the copy do not reach the original (R-054).
   The calculation engines set `NoClone` because they pass a context they copied themselves.
2. **Set up options.** The result type is recorded (`T` for generic expressions), and the owner's
   type is imported (`ExpressionImports.ImportOwner`) so that its members resolve. Without an owner,
   a private `DefaultExpressionOwner` stands in.
3. **Parse.** `ExpressionContext.Parse` runs the parser under a per-context lock. The analyzer
   (`Parsing/CustomExpressionAnalyzer.cs`, class `FleeExpressionAnalyzer`) builds expression
   elements directly in its callbacks, so there is no separate syntax tree. Element constructors
   resolve names and check types as they go, so a type error surfaces during parsing.
4. **Wrap.** The top element is wrapped in a `RootExpressionElement`, which converts the result to
   the requested result type (or boxes it for dynamic expressions).
5. **Emit.** A `DynamicMethod` with the signature
   `(object owner, ExpressionContext context, VariableCollection variables)` is created, owned by
   the owner's type so that IL may access its private members. The tree emits IL through
   `FleeILGenerator`.
6. **Second pass if needed.** Branches are first emitted in their short form. If a jump turns out
   to be too far for a short branch, `BranchManager` records it and the whole tree is emitted again
   into a fresh method with long branches where needed.
7. **Delegate.** The method becomes an `ExpressionEvaluator<T>`. `Evaluate()` calls it with the
   current owner, the context and the variables.

The runtime JIT-compiles the method on its first call, not in step 5. The benchmarks therefore
measure `Compile` and `CompileAndEvaluate` separately.

Compile-time information reaches the elements through an `IServiceProvider` holding the
`ExpressionOptions`, the `ExpressionParserOptions`, the `ExpressionContext`, the `IExpression` and
its `ExpressionInfo` (`Expression.AddServices`).

## The parser

`src/Flee/Parsing` is a private copy of the Grammatica runtime (tokenizer, regular-expression
engine, LL parser), plus four files generated from `Expression.grammar`
(`ExpressionParser`, `ExpressionTokenizer`, `ExpressionAnalyzer`, `ExpressionConstants`) and two
hand-written files: `CustomExpressionAnalyzer.cs` and `CustomTokenPatterns.cs`.

- The grammar has 40 tokens and 29 productions. Operator precedence is encoded in the productions,
  from `XOR` (lowest) to member access (highest); the language reference has the table.
- What the grammar does not capture is built at run time from the parser options: the real-number
  pattern (decimal separator, `RequireDigitsBeforeDecimalPoint`), the argument separator, and the
  date format used for `#...#` literals. That is why changing a parser option needs
  `ParserOptions.RecreateParser()`.
- Two parsers exist per context: the expression parser, and an identifier parser that only
  collects names. The calculation engines use the second to find which other atoms an expression
  refers to before compiling it (`CalcEngine/InternalTypes/IdentifierAnalyzer.cs`).
- Syntax errors come out of the parser as `ParserLogException` and are wrapped in
  `ExpressionCompileException` with reason `SyntaxError` (R-051 made that hold for the identifier
  parser too).

The parser is not changed before the first release (the two approved exceptions are R-042 and
R-050). Parser work waits in [deferred.md](deferred.md): non-ASCII identifiers (D-01), parser
experiments behind a switch (D-02), cleanup (D-03) and typed collections (D-04).

## Expression elements

- `ExpressionElement` (`ExpressionElements/Base`) is the base class: `ResultType` and
  `Emit(ilg, services)`. Intermediate bases: `BinaryExpressionElement`, `UnaryElement`,
  `MemberElement` and `LiteralElement`.
- One class per construct: `ArithmeticElement`, `CompareElement`, `AndOrElement`, `XorElement`,
  `NotElement`, `NegateElement`, `ShiftElement`, `InElement`, `ConditionalElement` (`if`),
  `CastElement`, one literal class per type, and the member elements (`IdentifierElement`,
  `FunctionCallElement`, `IndexerElement`, chained by `InvocationListElement`).
- **Types.** The result type of mixed arithmetic and every implicit conversion come from tables in
  `ImplicitConverter`, which follow C#'s rules for primitive types. User-defined operators and
  conversions are found through custom reflection binders (`BinaryOperatorBinder`,
  `ExplicitOperatorMethodBinder`).
- **Overloads.** Each candidate method gets a score: the average cost of converting each argument
  to its parameter type, lower is better (`CustomMethodInfo`). `params` arrays and extension
  methods have their own scoring. Where C# would report a call as ambiguous, Flee picks one; this
  is recorded behaviour (`TestOverloadResolution`, D-12).
- **Short-circuit logic.** `and`/`or` chains are emitted as jumps (`ShortCircuitInfo`), which is
  where the branch manager matters.
- **Checked arithmetic.** With `Options.Checked`, arithmetic and conversions use the overflow-checking
  opcodes.

## Names: members, variables, functions, atoms

`IdentifierElement.ResolveInternal` looks a name up in this order:

1. fields and properties of the owner and of imported types (or, after a dot, of the value before
   it), including virtual properties from a `TypeDescriptor`;
2. variables in `VariableCollection`, including on-demand variables through the
   `ResolveVariableType` event (only as the first name in a chain);
3. atoms of the calculation engine the context belongs to.

Anything else is a compile error with reason `UndefinedName`. Function calls resolve the same way
against methods, then on-demand functions (`ResolveFunction`).

At run time the generated IL reads values through helper methods, which are marked `NoInlining`
because .NET 10's JIT otherwise copies them into every generated method and makes each first call
slow (R-019):

| Value | Read through |
| --- | --- |
| Variable | `VariableCollection.GetVariableValueInternal<T>` |
| On-demand variable | `ResolveVariableValue` event, from the same helper |
| On-demand function | `VariableCollection.GetFunctionResultInternal<T>` (`InvokeFunction` event) |
| Virtual property | `VariableCollection.GetVirtualPropertyValueInternal<T>` |
| Calculation-engine atom | `ExpressionContext.CalculationEngine`, then `CalculationEngine.GetResult<T>` |

Owner fields, properties and methods are emitted as direct IL loads and calls, which is why an
owner is faster than variables.

## Variables

Each variable is an `IVariable`: `GenericVariable<T>` for plain values, `GenericExpressionVariable<T>`
and `DynamicExpressionVariable<T>` for compiled expressions used as values (they evaluate the inner
expression on every read). The variable's type is fixed when it is created; `DefineVariable`
creates one holding the type's default (R-053). Setting a value of another type later is not
checked (D-09).

## Calculation engine

`CalculationEngine` keeps atoms in a `DependencyManager<string>`: a graph from each atom to the
atoms that use it.

- `Add` registers a temporary head for the new name, compiles the expression with the engine
  attached to the context (so names of other atoms resolve to them and are recorded as
  dependencies), then replaces the head with the typed result pair and evaluates it. If compiling
  fails, the name is removed again (R-040).
- `Recalculate` evaluates the given atoms and everything that depends on them, in topological
  order. A cycle raises `CircularReferenceException`.
- `BatchLoader` parses each expression with the identifier parser to learn its references, sorts
  the batch by dependency, and adds the atoms in that order; names outside the batch are left to
  the compiler (R-052).
- `SimpleCalcEngine` has no graph: each expression gets a copy of the context in which the
  expressions it references are variables.

## Threads

- A compiled expression keeps its owner in a field, so evaluating one expression with different
  owners from several threads at once is not safe (D-11).
- Variables are shared objects without synchronisation.
- Parsing locks the context. Compiling copies the context first, and since R-054 the copy no longer
  shares options or imports with the original. Compiling from several threads with one context has
  still not been tested; one context per thread is the safe choice.

## Build, packaging and tests

- `Directory.Build.props` sends all output to `build/`, turns warnings into errors, copies the
  library to `build/runtime/Flee/<framework>/`, writes a CycloneDX SBOM there, and packs a NuGet
  package to `C:\dev\nuget\` (when it exists) or `build/nuget/`. Versions come from GitVersion.
- The package targets netstandard2.0, netstandard2.1, net8.0 and net10.0 and ships XML
  documentation (`Flee.xml`), symbols and Source Link.
- Tests (NUnit, net8.0 and net10.0, en-GB culture):
  - `ScriptTests`: the 1,756 cases from `TestScripts`, one test each, with
    `KnownFailures.txt` for cases that fail on purpose (empty since Phase 4);
  - `DocumentationTests`: the examples of the language reference, the API guide, the wiki and the
    upstream wiki, so the documentation cannot go stale;
  - fixture tests, many ported from the original VB.NET test project.
- `benchmarks/Flee.Benchmarks` measures parse, compile, compile-and-first-call and evaluate per
  vector on .NET 6, 8 and 10; results are kept in `benchmarks/results`.
