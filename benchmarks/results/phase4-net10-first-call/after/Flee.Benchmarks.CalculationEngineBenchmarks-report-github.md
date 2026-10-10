```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-1260P 2.10GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.204
  [Host]    : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3


```
| Method                      | Job       | Runtime   | Mean          | Error        | StdDev       | Ratio | RatioSD | Gen0     | Gen1     | Allocated  | Alloc Ratio |
|---------------------------- |---------- |---------- |--------------:|-------------:|-------------:|------:|--------:|---------:|---------:|-----------:|------------:|
| Load100                     | .NET 10.0 | .NET 10.0 | 183,664.76 μs | 3,189.635 μs | 5,501.964 μs | 14.12 |    0.47 | 500.0000 | 250.0000 | 5528.58 KB |        1.00 |
| Load100                     | .NET 8.0  | .NET 8.0  |  13,009.80 μs |   226.950 μs |   201.185 μs |  1.00 |    0.02 | 562.5000 | 312.5000 | 5517.02 KB |        1.00 |
|                             |           |           |               |              |              |       |         |          |          |            |             |
| RecalculateAfterInputChange | .NET 10.0 | .NET 10.0 |      45.65 μs |     0.903 μs |     1.323 μs |  0.84 |    0.03 |   6.9580 |   0.8545 |   64.23 KB |        1.00 |
| RecalculateAfterInputChange | .NET 8.0  | .NET 8.0  |      54.40 μs |     1.055 μs |     1.215 μs |  1.00 |    0.03 |   6.9580 |   0.9766 |   64.27 KB |        1.00 |
