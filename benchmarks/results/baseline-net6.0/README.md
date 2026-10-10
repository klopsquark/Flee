# Baseline: untouched library on .NET 6

The Phase 2 baseline. Later phases compare against these numbers (threshold: 10 %, see
`benchmarks/README.md`).

| Item | Value |
| --- | --- |
| Date | 2026-10-09, 21:20 to 21:59 |
| Commit | `db49428` (library code identical to upstream `f3b4fe2` apart from `InternalsVisibleTo`) |
| Runtime | .NET 6.0.36, X64 RyuJIT x86-64-v3, concurrent workstation GC |
| SDK | .NET SDK 10.0.204 |
| BenchmarkDotNet | 0.15.8, default job |
| Machine | 12th Gen Intel Core i7-1260P (laptop, 12 cores, 16 threads), Windows 11 25H2, High Performance power plan during the run |
| Culture | Invariant (set by the benchmarks) |
| Duration | 39 minutes, 48 benchmarks |

The run was on the maintainer's laptop with its normal background load. BenchmarkDotNet flagged
multimodal distributions for a few Parse and Compile cases (LogicChain Parse has a StdDev of
18 %), so treat differences below the 10 % threshold in those rows as noise.

## Summary

| Vector | Parse | Compile | Evaluate | Compile allocates |
| --- | ---: | ---: | ---: | ---: |
| Constants | 27 µs | 40 µs | 6.7 ns | 41 KB |
| OwnerMembers | 31 µs | 47 µs | 7.0 ns | 43 KB |
| ArithmeticVariables | 62 µs | 83 µs | 50 ns | 59 KB |
| OnDemand | 52 µs | 76 µs | 171 ns | 56 KB |
| Strings | 65 µs | 93 µs | 109 ns | 65 KB |
| LegacySmall | 88 µs | 125 µs | 95 ns | 91 KB |
| MixedNumeric | 95 µs | 151 µs | 165 ns | 103 KB |
| Casts | 120 µs | 168 µs | 69 ns | 122 KB |
| InList | 193 µs | 242 µs | 27 ns | 195 KB |
| NestedIf | 253 µs | 293 µs | 92 ns | 200 KB |
| ManyVariables | 431 µs | 544 µs | 1,267 ns | 339 KB |
| LegacySmallBranching | 453 µs | 551 µs | 52 ns | 388 KB |
| LogicChain | 786 µs | 808 µs | 102 ns | 735 KB |
| Large | 11.4 ms | 16.7 ms | 12.2 µs | 8.2 MB |
| LegacyBig | 22.8 ms | 46.3 ms | 3.6 µs | 11.4 MB |

| Other | Time | Allocated |
| --- | ---: | ---: |
| CalculationEngine: load 100 dependent expressions | 16.2 ms | 5.4 MB |
| CalculationEngine: recalculate after one input changed | 98 µs | 64 KB |
| Write two variables and evaluate `a + b` | 100 ns | 72 B |

Full tables with error, standard deviation and GC counts are in the `*-report-github.md` files;
the `*-report.csv` files have the raw summary.

## First observations

These are readings of the numbers, not profiles.

- **Parsing dominates compiling.** Parse is 63 to 97 % of Compile for every vector except
  LegacyBig (49 %; its long branches may force the second emit pass, not checked). That points
  at the plan's parser-allocation candidate more than at the IL-emission ones.
- **Variable reads are slow at evaluation.** ManyVariables evaluates 50 variables in 1.27 µs,
  about 25 ns per read, against 7 ns for a whole owner-field expression. That matches the plan's
  first candidate (a dictionary lookup per read).
- **Allocation scales with expression size**: LegacyBig allocates 11.4 MB for one compile.
