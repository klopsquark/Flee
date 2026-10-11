# Flee (klopsquark fork)

Flee, the Fast Lightweight Expression Evaluator, parses expressions such as `sqrt(a^2 + b^2)`
at runtime, type-checks them and compiles them straight to IL, so evaluating a compiled
expression costs one delegate call.

This repository is a maintained fork of [mparlak/Flee](https://github.com/mparlak/Flee).
Upstream has been inactive since March 2022 (Flee 2.0.0). The fork starts from that code
(upstream commit `f3b4fe2`, tagged `upstream-baseline`) and aims for a release that builds on a
current .NET SDK, is covered by a real regression suite, and is documented.

Upstream's original README is kept unchanged as [LEGACY.README.markdown](LEGACY.README.markdown).

## Status

Pre-release, version 2.6.0. All planned bug fixes and cleanup for the first release are done; see
[CHANGELOG.md](CHANGELOG.md). The package keeps the ID `Flee` and goes to a private feed only;
nothing is published on nuget.org. The release is source-compatible with Flee 2.0.0; what you may
notice when upgrading is in [doc/migration-from-flee-2.0.md](doc/migration-from-flee-2.0.md).

The road to the first release is in [doc/Flee fork plan to first release.md](doc/Flee%20fork%20plan%20to%20first%20release.md).
Every code change, with its reason and how it was verified, is recorded in
[doc/CHANGE-RATIONALE.md](doc/CHANGE-RATIONALE.md); work postponed until after the release is in
[doc/deferred.md](doc/deferred.md).

## Installation

Add the private feed that holds the package (a local folder works as well) and reference `Flee`:

```
dotnet nuget add source C:\dev\nuget --name local
dotnet add package Flee --version 2.6.0-*
```

The package targets netstandard2.0, netstandard2.1, net8.0 and net10.0, so it runs on .NET
Framework 4.6.1+ and .NET Core 2.0+. It needs a runtime with a JIT: no NativeAOT, no iOS (see
[doc/limitations.md](doc/limitations.md)).

## Example

```csharp
using Flee.PublicTypes;

var context = new ExpressionContext();
context.Imports.AddType(typeof(Math));
context.Variables["a"] = 3.0;
context.Variables["b"] = 4.0;

IGenericExpression<double> e = context.CompileGeneric<double>("sqrt(a^2 + b^2)");
double result = e.Evaluate();   // 5
```

## Documentation

| Document | Content |
| --- | --- |
| [Language reference](doc/language-reference.md) | Operators, literals, `if`, `cast`, `in`, type rules, culture |
| [API guide](doc/api-guide.md) | Contexts, imports, variables, owners, options, errors, the calculation engine |
| [Limitations](doc/limitations.md) | Platforms, language gaps, culture and threading |
| [Migration from Flee 2.0.0](doc/migration-from-flee-2.0.md) | What changes when you upgrade |
| [Internals](doc/architecture.md) | From text to IL: parser, elements, IL, names, calculation engine, threads |

The examples in the language reference and the API guide run as tests, so they match the library.
The same documents, with a getting-started page, examples, the project status and a development
guide, form the [wiki](https://github.com/klopsquark/Flee/wiki).

## Building from source

You need the .NET 10 SDK and the .NET 8 runtime (the tests run on net8.0 and net10.0). The
benchmarks also run on net6.0 and need the .NET 6 runtime for that.

```
dotnet tool restore
dotnet build Flee.sln
dotnet test Flee.sln
```

Build output goes to `build/`. Pass `/p:SkipSbom=true` to skip the SBOM step. [CLAUDE.md](CLAUDE.md)
has the details for contributors.

## How this fork was made

Most of the work in this fork was done by Claude, Anthropic's AI coding assistant, working as an
agent: analysing the code, writing tests, fixing bugs, cleaning up, measuring and writing the
documentation. klopsquark, the maintainer, set the goals and priorities, made the decisions,
reviewed the results and merged them. Every change is recorded with its reason and how it was
verified in [doc/CHANGE-RATIONALE.md](doc/CHANGE-RATIONALE.md), and behaviour is pinned by the
test suite, so the work can be checked independently of who did it.

## License

Flee is licensed under the GNU Lesser General Public License, version 2.1 or (at your option)
any later version. See [LICENSE](LICENSE). The original copyright notices are kept.

Flee was written by Eugene Ciloci in VB.NET (2007), then converted to C# and maintained by the
contributors of [mparlak/Flee](https://github.com/mparlak/Flee). This fork is maintained by
klopsquark.
