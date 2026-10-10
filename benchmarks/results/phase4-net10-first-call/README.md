# Phase 4: .NET 10 first-call cost of compiled expressions

Before and after R-019 (`[MethodImpl(MethodImplOptions.NoInlining)]` on the helpers that
generated code calls).

## What was wrong

On .NET 10, the first call of every compiled expression that reads a variable, calls an
on-demand function or references a calculation-engine atom took about 1.4 ms instead of about
0.05 ms on .NET 8. The JIT, with tiered PGO, inlined Flee's runtime helpers
(`VariableCollection.GetVariableValueInternal<T>` and its siblings,
`CalculationEngine.GetResult<T>`, the `ExpressionContext.CalculationEngine` getter) into each
generated method. `DOTNET_TieredPGO=0` made the cost disappear, and so did marking the helpers
`NoInlining`. The Phase 2 and 3 benchmarks missed it, because their Compile stage never calls the
expression; the new `CompileAndEvaluate` stage does.

## Runs

| Run | Folder | Library state |
| --- | --- | --- |
| Before | `before/` | commit `63a7928` |
| After | `after/` | variable, function and virtual-property helpers and `GetResult<T>` marked `NoInlining` |
| After, calculation engine | `after-calcengine/` | plus the `ExpressionContext.CalculationEngine` getter |

All on the maintainer's laptop (i7-1260P), BenchmarkDotNet 0.15.8, .NET 8.0.31 and 10.0.8,
2026-10-10.

## Calculation engine

| Benchmark | Runtime | Before | After |
| --- | --- | ---: | ---: |
| Load 100 dependent expressions | .NET 10 | 444 ms | 14.4 ms |
| Load 100 dependent expressions | .NET 8 | 17.1 ms | 8.9 ms |
| Recalculate after one input changed | .NET 10 | 55.3 µs | 46.6 µs |
| Recalculate after one input changed | .NET 8 | 62.7 µs | 53.1 µs |

Loading is now faster than the .NET 6 baseline (16.2 ms) on both runtimes, but .NET 10 still
takes 1.6 times as long as .NET 8. With `DOTNET_TieredPGO=0` the gap closes, so some PGO cost
remains; not investigated further.

## Stage benchmarks

Steady-state evaluation did not get slower; for the vectors that read variables it got faster,
because the inlined helper had made the generated code larger.

| Method | Vector | Runtime | Before | After | Change |
| --- | --- | --- | ---: | ---: | ---: |
| CompileAndEvaluate | ArithmeticVariables | .NET 10.0 | 2.18 ms | 107.6 µs | -95 % |
| CompileAndEvaluate | ArithmeticVariables | .NET 8.0 | 92.6 µs | 74.6 µs | -19 % |
| CompileAndEvaluate | Casts | .NET 10.0 | 3.02 ms | 210.4 µs | -93 % |
| CompileAndEvaluate | Casts | .NET 8.0 | 191.3 µs | 156.6 µs | -18 % |
| CompileAndEvaluate | Constants | .NET 10.0 | 55.3 µs | 46.5 µs | -16 % |
| CompileAndEvaluate | Constants | .NET 8.0 | 38.7 µs | 33.0 µs | -15 % |
| CompileAndEvaluate | InList | .NET 10.0 | 313.0 µs | 252.2 µs | -19 % |
| CompileAndEvaluate | InList | .NET 8.0 | 249.8 µs | 226.0 µs | -10 % |
| CompileAndEvaluate | Large | .NET 10.0 | 15.27 ms | 13.20 ms | -14 % |
| CompileAndEvaluate | Large | .NET 8.0 | 15.92 ms | 12.24 ms | -23 % |
| CompileAndEvaluate | LegacyBig | .NET 10.0 | 29.24 ms | 28.66 ms | -2 % |
| CompileAndEvaluate | LegacyBig | .NET 8.0 | 34.83 ms | 28.98 ms | -17 % |
| CompileAndEvaluate | LegacySmall | .NET 10.0 | 2.21 ms | 171.2 µs | -92 % |
| CompileAndEvaluate | LegacySmall | .NET 8.0 | 123.2 µs | 128.5 µs | +4 % |
| CompileAndEvaluate | LegacySmallBranching | .NET 10.0 | 458.5 µs | 450.1 µs | -2 % |
| CompileAndEvaluate | LegacySmallBranching | .NET 8.0 | 418.1 µs | 422.5 µs | +1 % |
| CompileAndEvaluate | LogicChain | .NET 10.0 | 905.6 µs | 930.2 µs | +3 % |
| CompileAndEvaluate | LogicChain | .NET 8.0 | 883.9 µs | 902.8 µs | +2 % |
| CompileAndEvaluate | ManyVariables | .NET 10.0 | 547.0 µs | 545.8 µs | -0 % |
| CompileAndEvaluate | ManyVariables | .NET 8.0 | 486.8 µs | 482.6 µs | -1 % |
| CompileAndEvaluate | MixedNumeric | .NET 10.0 | 3.80 ms | 214.5 µs | -94 % |
| CompileAndEvaluate | MixedNumeric | .NET 8.0 | 166.0 µs | 162.3 µs | -2 % |
| CompileAndEvaluate | NestedIf | .NET 10.0 | 334.0 µs | 307.8 µs | -8 % |
| CompileAndEvaluate | NestedIf | .NET 8.0 | 266.2 µs | 246.9 µs | -7 % |
| CompileAndEvaluate | OnDemand | .NET 10.0 | 2.80 ms | 154.4 µs | -94 % |
| CompileAndEvaluate | OnDemand | .NET 8.0 | 225.3 µs | 94.7 µs | -58 % |
| CompileAndEvaluate | OwnerMembers | .NET 10.0 | 71.3 µs | 67.0 µs | -6 % |
| CompileAndEvaluate | OwnerMembers | .NET 8.0 | 48.1 µs | 46.1 µs | -4 % |
| CompileAndEvaluate | Strings | .NET 10.0 | 479.6 µs | 480.8 µs | +0 % |
| CompileAndEvaluate | Strings | .NET 8.0 | 407.3 µs | 398.4 µs | -2 % |
| Evaluate | ArithmeticVariables | .NET 10.0 | 32.6 ns | 17.6 ns | -46 % |
| Evaluate | ArithmeticVariables | .NET 8.0 | 37.8 ns | 33.2 ns | -12 % |
| Evaluate | Casts | .NET 10.0 | 41.6 ns | 25.5 ns | -39 % |
| Evaluate | Casts | .NET 8.0 | 49.4 ns | 44.7 ns | -10 % |
| Evaluate | Constants | .NET 10.0 | 8.2 ns | 6.6 ns | -19 % |
| Evaluate | Constants | .NET 8.0 | 7.8 ns | 6.4 ns | -18 % |
| Evaluate | InList | .NET 10.0 | 12.6 ns | 11.1 ns | -12 % |
| Evaluate | InList | .NET 8.0 | 20.4 ns | 18.2 ns | -11 % |
| Evaluate | Large | .NET 10.0 | 4.4 µs | 4.3 µs | -3 % |
| Evaluate | Large | .NET 8.0 | 8.4 µs | 7.9 µs | -6 % |
| Evaluate | LegacyBig | .NET 10.0 | 1.1 µs | 861.4 ns | -23 % |
| Evaluate | LegacyBig | .NET 8.0 | 1.6 µs | 1.2 µs | -22 % |
| Evaluate | LegacySmall | .NET 10.0 | 47.7 ns | 28.3 ns | -41 % |
| Evaluate | LegacySmall | .NET 8.0 | 58.0 ns | 37.9 ns | -35 % |
| Evaluate | LegacySmallBranching | .NET 10.0 | 17.6 ns | 17.5 ns | -0 % |
| Evaluate | LegacySmallBranching | .NET 8.0 | 20.7 ns | 21.6 ns | +4 % |
| Evaluate | LogicChain | .NET 10.0 | 42.6 ns | 43.2 ns | +1 % |
| Evaluate | LogicChain | .NET 8.0 | 81.8 ns | 82.2 ns | +1 % |
| Evaluate | ManyVariables | .NET 10.0 | 510.2 ns | 513.8 ns | +1 % |
| Evaluate | ManyVariables | .NET 8.0 | 752.1 ns | 765.7 ns | +2 % |
| Evaluate | MixedNumeric | .NET 10.0 | 83.0 ns | 49.3 ns | -41 % |
| Evaluate | MixedNumeric | .NET 8.0 | 98.5 ns | 95.7 ns | -3 % |
| Evaluate | NestedIf | .NET 10.0 | 30.5 ns | 28.8 ns | -6 % |
| Evaluate | NestedIf | .NET 8.0 | 62.7 ns | 55.6 ns | -11 % |
| Evaluate | OnDemand | .NET 10.0 | 71.7 ns | 53.2 ns | -26 % |
| Evaluate | OnDemand | .NET 8.0 | 70.7 ns | 68.0 ns | -4 % |
| Evaluate | OwnerMembers | .NET 10.0 | 6.5 ns | 6.3 ns | -3 % |
| Evaluate | OwnerMembers | .NET 8.0 | 6.9 ns | 6.9 ns | +0 % |
| Evaluate | Strings | .NET 10.0 | 44.8 ns | 44.2 ns | -1 % |
| Evaluate | Strings | .NET 8.0 | 59.3 ns | 58.7 ns | -1 % |
