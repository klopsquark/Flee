# Phase 3: .NET 6, 8 and 10 compared

Same benchmarks as the baseline, after the Phase 3 target change, on three runtimes in one run.

| Item | Value |
| --- | --- |
| Date | 2026-10-09, 22:19 to 23:25 |
| Commit | `bf2f858` (library behaviour unchanged since the baseline; see R-010 to R-016) |
| Runtimes | .NET 6.0.36 (Flee's netstandard2.1 build), .NET 8.0.31 and .NET 10.0.8 (Flee's net8.0 and net10.0 builds) |
| BenchmarkDotNet | 0.15.8, default job, `--runtimes net6.0 net8.0 net10.0` |
| Machine | Same laptop as the baseline (i7-1260P, Windows 11), High Performance power plan during the run |
| Duration | 66 minutes, 144 benchmarks |

Ratios within this run (one runtime against another) are more reliable than comparisons with
the baseline, which ran two hours earlier. The .NET 6 column of this run is faster
than the baseline for the same runtime (geometric means 14 to 21 %), which is probably machine state rather than code: the library
code did not change in any way that runs in Release.

## Summary

Geometric mean over the 15 vectors, time relative to the baseline:

| Stage | .NET 6 (this run) | .NET 8 | .NET 10 |
| --- | ---: | ---: | ---: |
| Parse | 0.83 | 0.60 | 0.55 |
| Compile | 0.79 | 0.63 | 0.54 |
| Evaluate | 0.86 | 0.68 | 0.54 |

| Other | .NET 6 | .NET 8 | .NET 10 |
| --- | ---: | ---: | ---: |
| CalculationEngine: load 100 expressions | 17.8 ms | 13.6 ms | **433 ms** |
| CalculationEngine: recalculate | 71 µs | 55 µs | 51 µs |
| Write two variables and evaluate | 79 ns | 62 ns | 48 ns |

Allocations are unchanged within a few percent on every runtime.

## Two regressions on .NET 10

- **Loading the calculation engine is 24 times slower.** Confirmed in a second run (351 ms
  against 14.5 ms on .NET 8) and with a stopwatch outside BenchmarkDotNet: each
  `CalculationEngine.Add` takes about 2 ms on .NET 10 against 0.3 ms on .NET 8, with no
  first-chance exceptions. Compiling single expressions is faster on .NET 10, so the cause sits
  in the calculation engine's own path (atom references, dependency handling). Not analysed yet;
  recorded as a Phase 4 item.
- **Evaluating the constant expression is slower**: 6.7 ns in the baseline, 11.9 ns on .NET 10
  (+79 %), 8.7 ns on .NET 8. The absolute difference is 5 ns per call and the other 14 vectors
  evaluate 13 to 69 % faster on .NET 10, so this looks like call overhead of a trivially small
  delegate rather than a Flee problem. Kept in view, not investigated.

Everything else is faster on .NET 8 and faster still on .NET 10.

## All stage results

| Stage | Vector | Baseline (.NET 6, net6.0 build) | .NET 6 (netstandard2.1 build) | .NET 8 | .NET 10 | .NET 8 vs baseline | .NET 10 vs baseline |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Parse | ArithmeticVariables | 62.1 µs | 64.1 µs | 47.3 µs | 27.4 µs | -24 % | -56 % |
| Parse | Casts | 119.8 µs | 122.7 µs | 105.0 µs | 61.2 µs | -12 % | -49 % |
| Parse | Constants | 27.3 µs | 29.7 µs | 21.4 µs | 24.5 µs | -21 % | -10 % |
| Parse | InList | 193.0 µs | 204.2 µs | 154.9 µs | 139.2 µs | -20 % | -28 % |
| Parse | Large | 11.41 ms | 11.74 ms | 9.23 ms | 8.74 ms | -19 % | -23 % |
| Parse | LegacyBig | 22.77 ms | 16.58 ms | 14.61 ms | 19.81 ms | -36 % | -13 % |
| Parse | LegacySmall | 87.7 µs | 55.8 µs | 40.3 µs | 37.1 µs | -54 % | -58 % |
| Parse | LegacySmallBranching | 453.3 µs | 328.8 µs | 240.0 µs | 215.8 µs | -47 % | -52 % |
| Parse | LogicChain | 786.3 µs | 680.4 µs | 494.6 µs | 427.2 µs | -37 % | -46 % |
| Parse | ManyVariables | 430.6 µs | 332.1 µs | 221.2 µs | 198.4 µs | -49 % | -54 % |
| Parse | MixedNumeric | 94.5 µs | 66.0 µs | 47.4 µs | 41.8 µs | -50 % | -56 % |
| Parse | NestedIf | 253.5 µs | 231.4 µs | 128.9 µs | 171.1 µs | -49 % | -33 % |
| Parse | OnDemand | 52.1 µs | 35.3 µs | 24.6 µs | 22.4 µs | -53 % | -57 % |
| Parse | OwnerMembers | 30.6 µs | 20.4 µs | 14.5 µs | 13.1 µs | -53 % | -57 % |
| Parse | Strings | 65.2 µs | 48.5 µs | 33.9 µs | 31.1 µs | -48 % | -52 % |
| Compile | ArithmeticVariables | 82.9 µs | 84.2 µs | 71.9 µs | 37.6 µs | -13 % | -55 % |
| Compile | Casts | 167.6 µs | 177.5 µs | 170.9 µs | 89.5 µs | +2 % | -47 % |
| Compile | Constants | 39.6 µs | 41.3 µs | 33.4 µs | 31.9 µs | -16 % | -19 % |
| Compile | InList | 241.6 µs | 250.7 µs | 188.2 µs | 169.2 µs | -22 % | -30 % |
| Compile | Large | 16.74 ms | 16.63 ms | 12.61 ms | 12.40 ms | -25 % | -26 % |
| Compile | LegacyBig | 46.30 ms | 36.06 ms | 20.32 ms | 26.97 ms | -56 % | -42 % |
| Compile | LegacySmall | 125.2 µs | 76.8 µs | 67.7 µs | 60.2 µs | -46 % | -52 % |
| Compile | LegacySmallBranching | 551.0 µs | 372.3 µs | 297.8 µs | 263.5 µs | -46 % | -52 % |
| Compile | LogicChain | 807.9 µs | 835.9 µs | 602.5 µs | 524.4 µs | -25 % | -35 % |
| Compile | ManyVariables | 543.8 µs | 402.3 µs | 260.6 µs | 248.6 µs | -52 % | -54 % |
| Compile | MixedNumeric | 151.5 µs | 99.2 µs | 128.5 µs | 72.6 µs | -15 % | -52 % |
| Compile | NestedIf | 293.4 µs | 200.3 µs | 135.2 µs | 207.6 µs | -54 % | -29 % |
| Compile | OnDemand | 76.0 µs | 46.9 µs | 34.5 µs | 30.8 µs | -55 % | -59 % |
| Compile | OwnerMembers | 47.1 µs | 29.1 µs | 25.0 µs | 21.7 µs | -47 % | -54 % |
| Compile | Strings | 93.4 µs | 59.8 µs | 43.1 µs | 40.3 µs | -54 % | -57 % |
| Evaluate | ArithmeticVariables | 50.0 ns | 52.7 ns | 44.3 ns | 29.6 ns | -11 % | -41 % |
| Evaluate | Casts | 69.1 ns | 73.8 ns | 64.4 ns | 56.3 ns | -7 % | -18 % |
| Evaluate | Constants | 6.7 ns | 7.0 ns | 8.7 ns | 11.9 ns | +31 % | +79 % |
| Evaluate | InList | 27.4 ns | 31.3 ns | 25.8 ns | 19.0 ns | -6 % | -31 % |
| Evaluate | Large | 12.2 µs | 12.4 µs | 10.3 µs | 6.2 µs | -16 % | -49 % |
| Evaluate | LegacyBig | 3.6 µs | 2.4 µs | 1.3 µs | 1.1 µs | -65 % | -69 % |
| Evaluate | LegacySmall | 95.3 ns | 67.5 ns | 36.2 ns | 38.3 ns | -62 % | -60 % |
| Evaluate | LegacySmallBranching | 52.1 ns | 35.7 ns | 20.7 ns | 16.6 ns | -60 % | -68 % |
| Evaluate | LogicChain | 101.7 ns | 105.0 ns | 85.8 ns | 42.2 ns | -16 % | -59 % |
| Evaluate | ManyVariables | 1.3 µs | 1.1 µs | 758.8 ns | 543.0 ns | -40 % | -57 % |
| Evaluate | MixedNumeric | 165.3 ns | 121.5 ns | 131.5 ns | 82.8 ns | -20 % | -50 % |
| Evaluate | NestedIf | 92.2 ns | 72.2 ns | 57.7 ns | 47.9 ns | -37 % | -48 % |
| Evaluate | OnDemand | 170.8 ns | 117.0 ns | 68.6 ns | 65.7 ns | -60 % | -62 % |
| Evaluate | OwnerMembers | 7.0 ns | 5.8 ns | 7.3 ns | 6.1 ns | +4 % | -13 % |
| Evaluate | Strings | 108.9 ns | 89.1 ns | 64.6 ns | 48.1 ns | -41 % | -56 % |

The full BenchmarkDotNet tables are in the `*-report-github.md` and `*-report.csv` files.
