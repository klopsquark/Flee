```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-1260P 2.10GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.204
  [Host]    : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3


```
| Method                      | Job       | Runtime   | Mean         | Error      | StdDev     | Ratio | RatioSD | Gen0     | Gen1     | Gen2    | Allocated  | Alloc Ratio |
|---------------------------- |---------- |---------- |-------------:|-----------:|-----------:|------:|--------:|---------:|---------:|--------:|-----------:|------------:|
| Load100                     | .NET 10.0 | .NET 10.0 | 14,397.29 μs | 247.825 μs | 219.690 μs |  1.63 |    0.04 | 593.7500 | 406.2500 | 31.2500 | 5524.97 KB |        1.00 |
| Load100                     | .NET 8.0  | .NET 8.0  |  8,853.85 μs | 156.989 μs | 146.848 μs |  1.00 |    0.02 | 562.5000 | 312.5000 |       - | 5517.02 KB |        1.00 |
|                             |           |           |              |            |            |       |         |          |          |         |            |             |
| RecalculateAfterInputChange | .NET 10.0 | .NET 10.0 |     46.55 μs |   0.447 μs |   0.397 μs |  0.88 |    0.01 |   6.9580 |   0.8545 |       - |   64.23 KB |        1.00 |
| RecalculateAfterInputChange | .NET 8.0  | .NET 8.0  |     53.14 μs |   0.811 μs |   0.759 μs |  1.00 |    0.02 |   6.9580 |   0.9766 |       - |   64.27 KB |        1.00 |
