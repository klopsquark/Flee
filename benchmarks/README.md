# Benchmarks

BenchmarkDotNet measurements of Flee, so that later changes (new SDK, bug fixes, optimizations)
can be judged against the untouched library. Plan: Phase 2.

## What is measured

`StageBenchmarks` runs every test vector (`Vectors.cs`) through three stages, each measured on
its own, with time and allocations:

| Stage | What it covers |
| --- | --- |
| `Parse` | Tokenizing, parsing and building the element tree, including name and type resolution. Uses library internals (`InternalsVisibleTo`). |
| `Compile` | The public `CompileDynamic`: context clone, parse, IL emission (twice for long branches), delegate creation. |
| `Evaluate` | One call of an already compiled expression. |

| Vector | Exercises |
| --- | --- |
| Constants | Parser and emit floor |
| ArithmeticVariables | Variable reads, an imported function, power |
| ManyVariables | Sum of 50 variables: variable lookup cost |
| OwnerMembers | Field access on an expression owner |
| MixedNumeric | Type promotion and conversions |
| Strings | Concatenation, comparison, a property |
| LogicChain | 20 `and`/`or` terms: short-circuit code |
| NestedIf | Nested conditionals |
| InList | `x in (...)` with 20 items |
| Casts | Explicit conversions |
| OnDemand | Variables and a function supplied through events |
| Large | 300 groups of `a * k - b / m` (about 1,200 operands): scaling of parse and compile |
| LegacyBig, LegacySmall, LegacySmallBranching | The expressions of the former timing tests |

`CalculationEngineBenchmarks` loads 100 dependent expressions and recalculates after one input
changed. `VariableBenchmarks` writes two variables and evaluates (the former
`TestFastVariables` test).

The plan also asks for a vector with expressions from the maintainer's own applications. It is
not there yet.

## Running

Always in Release, on a quiet machine (close other work, plugged in). The project targets
net6.0 (the baseline runtime; Flee comes from its netstandard2.1 build there), net8.0 and
net10.0. One runtime:

```
dotnet run -c Release -f net10.0 --project benchmarks/Flee.Benchmarks -- --filter *
```

Several runtimes in one run, with ratios against the first:

```
dotnet run -c Release -f net10.0 --project benchmarks/Flee.Benchmarks -- --filter * --runtimes net6.0 net8.0 net10.0
```

`--filter *Stage*` or `--filter *Large*` narrows the run; `--job short` gives a quick, rough
answer. A full run takes about half an hour. BenchmarkDotNet switches Windows to the High
Performance power plan while it runs and switches back afterwards.

Results land in `BenchmarkDotNet.Artifacts/results` (git-ignored).

## Recording results

Results worth keeping go to `benchmarks/results/<name>/`: the `*-report-github.md` and
`*-report.csv` files, plus a short `README.md` with the commit, machine, runtime and anything
unusual about the run. The baseline is `benchmarks/results/baseline-net6.0`.

## Regression threshold

A change that makes any stage of any vector more than **10 %** slower, or that allocates
noticeably more, is investigated before it is merged. Differences below that are usually noise
on a desktop machine. Compare medians from runs on the same machine.

Benchmarks are not part of CI: shared build machines are too noisy. Run them by hand before
and after relevant changes.
