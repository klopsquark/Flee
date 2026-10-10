```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-1260P 2.10GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.204
  [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3


```
| Method                      | Mean         | Error      | StdDev     | Median       | Gen0     | Gen1     | Gen2     | Allocated  |
|---------------------------- |-------------:|-----------:|-----------:|-------------:|---------:|---------:|---------:|-----------:|
| Load100                     | 14,134.05 μs | 310.012 μs | 859.043 μs | 13,883.64 μs | 562.5000 | 406.2500 | 125.0000 | 5320.01 KB |
| RecalculateAfterInputChange |     44.62 μs |   0.824 μs |   0.770 μs |     44.61 μs |   6.9580 |   0.8545 |        - |   64.23 KB |
