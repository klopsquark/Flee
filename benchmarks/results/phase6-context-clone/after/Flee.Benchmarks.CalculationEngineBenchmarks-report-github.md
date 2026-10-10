```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-1260P 2.10GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.204
  [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3


```
| Method                      | Mean         | Error      | StdDev     | Gen0     | Gen1     | Gen2    | Allocated  |
|---------------------------- |-------------:|-----------:|-----------:|---------:|---------:|--------:|-----------:|
| Load100                     | 13,482.04 μs | 269.545 μs | 472.086 μs | 562.5000 | 375.0000 | 62.5000 | 5323.88 KB |
| RecalculateAfterInputChange |     45.18 μs |   0.881 μs |   1.589 μs |   6.9580 |   0.8545 |       - |   64.23 KB |
