```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-1260P 2.10GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.204
  [Host]    : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  .NET 6.0  : .NET 6.0.36 (6.0.36, 6.0.3624.51421), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3


```
| Method                  | Job       | Runtime   | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |---------- |---------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| SetVariablesAndEvaluate | .NET 10.0 | .NET 10.0 | 48.12 ns | 0.764 ns | 0.677 ns |  0.61 |    0.01 | 0.0076 |      72 B |        1.00 |
| SetVariablesAndEvaluate | .NET 6.0  | .NET 6.0  | 79.36 ns | 1.348 ns | 1.261 ns |  1.00 |    0.02 | 0.0076 |      72 B |        1.00 |
| SetVariablesAndEvaluate | .NET 8.0  | .NET 8.0  | 62.02 ns | 0.863 ns | 0.807 ns |  0.78 |    0.02 | 0.0076 |      72 B |        1.00 |
