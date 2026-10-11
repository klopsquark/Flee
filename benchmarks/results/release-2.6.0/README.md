# Release 2.6.0: all benchmarks on .NET 6, 8 and 10

The release check of the plan: the library as released, against the Phase 2 baseline
([baseline-net6.0](../baseline-net6.0/README.md), upstream code on .NET 6). What the vectors and
stages measure, and how to run the benchmarks, is in [benchmarks/README.md](../../README.md);
the threshold for a regression is 10 %.

All numbers come from one laptop under normal desktop conditions, not from a dedicated benchmark
machine. Compare them with each other (runtimes, baseline and release on the same machine), not
with numbers from other hardware; differences below about 10 % are within the noise of such runs.

| Item | Value |
| --- | --- |
| Date | 2026-10-11, 00:35 to 01:35 |
| Commit | `a5b30ea` (develop plus the pre-release review and R-061) |
| Runtimes | .NET 6.0.36 (Flee's netstandard2.1 build), .NET 8.0.31 and .NET 10.0.8 (net8.0 and net10.0 builds), X64 RyuJIT x86-64-v3 |
| SDK | .NET SDK 10.0.204 |
| BenchmarkDotNet | 0.15.8, default job, `--filter * --runtimes net6.0 net8.0 net10.0` |
| Machine | The maintainer's laptop: 12th Gen Intel Core i7-1260P (12 cores, 16 threads), Windows 11 25H2, High Performance power plan during the run, no other work started during the run |
| Duration | 60 minutes, 189 benchmarks |

A first run of the same kind (2026-10-10, 21:30 to 22:34, commit `401a769`) found that
evaluation of four vectors allocated up to 80 % more than the baseline; the cause was R-053, fixed
by R-061. That run also measured 25 ms for loading the calculation engine on .NET 10 with a large
spread; this run measured 13.5 ms, in line with Phase 4 and Phase 6. Only this second run is kept.

## Summary

Geometric mean over the 15 vectors, time relative to the baseline (lower is faster):

| Stage | .NET 6 | .NET 8 | .NET 10 |
| --- | ---: | ---: | ---: |
| Parse | 0.70 | 0.52 | 0.48 |
| Compile | 0.67 | 0.53 | 0.47 |
| Evaluate | 0.84 | 0.63 | 0.42 |

| Other | Baseline (.NET 6) | .NET 6 | .NET 8 | .NET 10 |
| --- | ---: | ---: | ---: | ---: |
| Calculation engine: load 100 expressions | 16.2 ms | 11.5 ms | 9.1 ms | 13.5 ms |
| Calculation engine: recalculate | 97.9 µs | 72 µs | 56 µs | 46 µs |
| Write two variables and evaluate | 99.7 ns | 80 ns | 62 ns | 40 ns |

## Per vector on .NET 10

Baseline (.NET 6, upstream code) → this release on .NET 10; medians where BenchmarkDotNet reports
them, otherwise means. The first call has no baseline (the stage was added in Phase 4).

| Vector | Parse | Compile | Evaluate | First call (compile + evaluate) |
| --- | ---: | ---: | ---: | ---: |
| Constants | 27.3 µs → 12.6 µs | 39.6 µs → 17.5 µs | 6.7 ns → 6.9 ns | 48.9 µs |
| OwnerMembers | 30.6 µs → 13.8 µs | 47.1 µs → 23.2 µs | 7.0 ns → 7.7 ns | 71.6 µs |
| ArithmeticVariables | 62.1 µs → 26.2 µs | 82.9 µs → 34.0 µs | 50.0 ns → 18.9 ns | 102 µs |
| OnDemand | 52.1 µs → 22.8 µs | 76.0 µs → 33.1 µs | 171 ns → 60.2 ns | 157 µs |
| Strings | 65.2 µs → 30.1 µs | 93.4 µs → 39.2 µs | 109 ns → 49.2 ns | 507 µs |
| MixedNumeric | 94.5 µs → 39.8 µs | 151 µs → 70.2 µs | 165 ns → 54.6 ns | 209 µs |
| Casts | 120 µs → 60.0 µs | 168 µs → 86.6 µs | 69.1 ns → 26.5 ns | 222 µs |
| InList | 193 µs → 157 µs | 242 µs → 132 µs | 27.4 ns → 13.7 ns | 272 µs |
| NestedIf | 253 µs → 115 µs | 293 µs → 128 µs | 92.2 ns → 32.3 ns | 331 µs |
| LogicChain | 786 µs → 446 µs | 808 µs → 541 µs | 102 ns → 44.2 ns | 919 µs |
| ManyVariables | 431 µs → 178 µs | 544 µs → 219 µs | 1.27 µs → 526 ns | 531 µs |
| LegacySmall | 87.7 µs → 34.8 µs | 125 µs → 57.5 µs | 95.3 ns → 28.8 ns | 162 µs |
| LegacySmallBranching | 453 µs → 220 µs | 551 µs → 263 µs | 52.1 ns → 17.6 ns | 449 µs |
| LegacyBig | 22.8 ms → 14.4 ms | 46.3 ms → 19.4 ms | 3.60 µs → 895 ns | 25.7 ms |
| Large | 11.4 ms → 4.9 ms | 16.7 ms → 7.5 ms | 12.2 µs → 4.32 µs | 12.6 ms |

## Against the threshold

- No stage of any vector is more than 10 % slower than the baseline on .NET 10.
- Evaluating `Constants` (`1 + 2 * 3`) takes 9.5 ns on .NET 8 and 7.5 ns on .NET 6 against
  6.7 ns in the baseline. That is 1 to 3 ns on a delegate call, with a standard deviation of
  0.5 ns, and does not appear on .NET 10 (6.9 ns); D-08 covers it.
- Allocations equal the baseline on .NET 6. On .NET 8 and 10, compiling `Casts` allocates
  17 to 19 % more; Phase 3 measured the same before any library change, so it comes from the
  runtime, not from Flee.
- The first call is slower on .NET 10 (507 µs) and .NET 8 (426 µs) than on .NET 6 (167 µs) for
  `Strings`. Phase 4 measured the same on .NET 8 and 10 before its changes (about 400 and 480 µs),
  so it is not caused by them; the reason is not investigated and belongs to Phase 7 (D-33).
