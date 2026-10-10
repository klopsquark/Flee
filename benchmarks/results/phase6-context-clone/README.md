# Phase 6: context clone fix (R-054)

Compile cost before and after the fix for cloned contexts (D-28), which copies the namespace
imports on every compile and gives each copy of the options its own owner.

- **Before:** commit cd55425. **After:** commit 4076879 (cd55425 plus the fix).
- **Machine:** the maintainer's desktop, Windows 11, .NET 10.0, default BenchmarkDotNet job.
  The two runs ran one after the other, before first.
- **Filter:** `*StageBenchmarks.Compile*` (Compile and CompileAndEvaluate) and
  `*CalculationEngineBenchmarks*`.

## Result

Every benchmark is within the 10 % threshold (`benchmarks/README.md`). Medians or means, after
against before:

```
Compile             ArithmeticVariables        33.56 μs      35.54 μs   +5.9%      57.7 KB   57.82 KB  +0.2%
CompileAndEvaluate  ArithmeticVariables       101.82 μs     101.78 μs   -0.0%     57.63 KB   57.84 KB  +0.4%
Compile             Casts                      82.90 μs      90.23 μs   +8.8%    142.36 KB  145.07 KB  +1.9%
CompileAndEvaluate  Casts                     197.08 μs     212.32 μs   +7.7%     142.7 KB  145.08 KB  +1.7%
Compile             Constants                  16.93 μs      17.45 μs   +3.1%     40.01 KB   40.04 KB  +0.1%
CompileAndEvaluate  Constants                  43.84 μs      46.32 μs   +5.7%     40.03 KB   40.06 KB  +0.1%
Compile             InList                    105.31 μs     103.17 μs   -2.0%    192.54 KB  192.58 KB  +0.0%
CompileAndEvaluate  InList                    252.46 μs     252.73 μs   +0.1%    192.57 KB   192.6 KB  +0.0%
Compile             Large                   7,101.73 μs   7,395.75 μs   +4.1%   8643.09 KB 8646.96 KB  +0.0%
CompileAndEvaluate  Large                  11,823.08 μs  12,411.25 μs   +5.0%   8647.06 KB 8645.16 KB  -0.0%
Compile             LegacyBig              16,864.34 μs  17,518.99 μs   +3.9%   11354.4 KB 11354.39 KB  -0.0%
CompileAndEvaluate  LegacyBig              24,695.15 μs  25,746.41 μs   +4.3%   11363.17 KB 11363.2 KB  +0.0%
Compile             LegacySmall                52.69 μs      56.21 μs   +6.7%     98.44 KB   98.49 KB  +0.1%
CompileAndEvaluate  LegacySmall               156.69 μs     159.43 μs   +1.7%     98.77 KB   98.79 KB  +0.0%
Compile             LegacySmallBranching      246.63 μs     266.22 μs   +7.9%    407.07 KB  407.18 KB  +0.0%
CompileAndEvaluate  LegacySmallBranching      421.13 μs     440.54 μs   +4.6%    407.21 KB   407.3 KB  +0.0%
Compile             LogicChain                517.22 μs     545.75 μs   +5.5%    712.46 KB  712.49 KB  +0.0%
CompileAndEvaluate  LogicChain                880.54 μs     919.83 μs   +4.5%    712.48 KB  712.51 KB  +0.0%
Compile             ManyVariables             218.25 μs     215.66 μs   -1.2%    294.39 KB  294.42 KB  +0.0%
CompileAndEvaluate  ManyVariables             523.33 μs     522.56 μs   -0.1%    294.41 KB  294.49 KB  +0.0%
Compile             MixedNumeric               68.58 μs      68.95 μs   +0.5%    113.13 KB  113.22 KB  +0.1%
CompileAndEvaluate  MixedNumeric              210.16 μs     211.55 μs   +0.7%    113.23 KB  113.32 KB  +0.1%
Compile             NestedIf                  126.60 μs     124.87 μs   -1.4%     198.4 KB  198.51 KB  +0.1%
CompileAndEvaluate  NestedIf                  317.32 μs     321.64 μs   +1.4%    198.31 KB  198.37 KB  +0.0%
Compile             OnDemand                   30.84 μs      30.91 μs   +0.2%     55.05 KB   55.08 KB  +0.1%
CompileAndEvaluate  OnDemand                  153.84 μs     155.16 μs   +0.9%     55.53 KB   55.55 KB  +0.0%
Compile             OwnerMembers               21.94 μs      22.05 μs   +0.5%     45.13 KB   45.03 KB  -0.2%
CompileAndEvaluate  OwnerMembers               67.19 μs      70.79 μs   +5.4%     45.04 KB   45.14 KB  +0.2%
Compile             Strings                    35.75 μs      38.64 μs   +8.1%     62.33 KB   62.36 KB  +0.0%
CompileAndEvaluate  Strings                   490.37 μs     531.15 μs   +8.3%     62.48 KB   62.51 KB  +0.0%
Load100                                    13,883.64 μs  13,482.04 μs   -2.9%   5320.01 KB 5323.88 KB  +0.1%
RecalculateAfterInputChange                            44.61 μs      45.18 μs   +1.3%     64.23 KB   64.23 KB  +0.0%
```

The fix adds one small list and one object per imported namespace or type to each compile
(about 0.1 KB; 2.7 KB for the Casts vector, which imports all built-in types with `ImportBuiltinTypes`). The time differences
of up to 9 % also appear for the Large and LegacyBig vectors, where the copy is a vanishing share
of 7 to 17 ms, so they are mostly run-to-run variation rather than the fix.
