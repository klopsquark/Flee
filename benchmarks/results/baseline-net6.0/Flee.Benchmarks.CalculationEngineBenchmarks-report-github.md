```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-1260P 2.10GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.204
  [Host]     : .NET 6.0.36 (6.0.36, 6.0.3624.51421), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 6.0.36 (6.0.36, 6.0.3624.51421), X64 RyuJIT x86-64-v3


```
| Method                      | Mean         | Error      | StdDev     | Gen0     | Gen1     | Allocated  |
|---------------------------- |-------------:|-----------:|-----------:|---------:|---------:|-----------:|
| Load100                     | 16,196.64 μs | 265.081 μs | 206.958 μs | 593.7500 | 281.2500 | 5505.33 KB |
| RecalculateAfterInputChange |     97.87 μs |   1.892 μs |   2.890 μs |   6.9580 |   0.8545 |    64.3 KB |
