# Flee (klopsquark fork)

Flee, the Fast Lightweight Expression Evaluator, parses expressions such as `sqrt(a^2 + b^2)`
at runtime, type-checks them and compiles them straight to IL, so evaluating a compiled
expression costs one delegate call.

This repository is a maintained fork of [mparlak/Flee](https://github.com/mparlak/Flee).
Upstream has been inactive since March 2022 (Flee 2.0.0). The fork starts from that code
(upstream commit `f3b4fe2`, tagged `upstream-baseline`) and aims for a release that builds on a
current .NET SDK, is covered by a real regression suite, and is documented.

Upstream's original README is kept unchanged as [README.markdown](README.markdown).

## Status

Pre-release. Nothing is published yet, and the package name is not decided.

The road to the first release is in [doc/Flee fork plan to first release.md](doc/Flee%20fork%20plan%20to%20first%20release.md).
In short:

| Phase | Content |
| --- | --- |
| 0 | Fork setup: baseline, licence, identity, compatibility stance |
| 1 | Build and tests: the 1,756 script cases run as NUnit tests, CI on Windows and Linux |
| 2 | Benchmark baseline with BenchmarkDotNet |
| 3 | SDK modernization |
| 4 | Bug fixes and mechanical cleanup |
| 6 | Documentation |
| 7 | Performance (stretch goal) |

Every code change, with its reason and how it was verified, is recorded in
[doc/CHANGE-RATIONALE.md](doc/CHANGE-RATIONALE.md).

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

## Building from source

You need a .NET SDK that can build net6.0 (the .NET 10 SDK works) and the .NET 6 runtime to
run the tests.

```
dotnet tool restore
dotnet build Flee.sln
dotnet test Flee.sln
```

Build output goes to `build/`. Pass `/p:SkipSbom=true` to skip the SBOM step.

## License

Flee is licensed under the GNU Lesser General Public License, version 2.1 or (at your option)
any later version. See [LICENSE](LICENSE). The original copyright notices are kept.

Flee was written by Eugene Ciloci in VB.NET (2007), then converted to C# and maintained by the
contributors of [mparlak/Flee](https://github.com/mparlak/Flee). This fork is maintained by
klopsquark.
