using BenchmarkDotNet.Running;

namespace Flee.Benchmarks
{
    public static class Program
    {
        // dotnet run -c Release --project benchmarks/Flee.Benchmarks -- --filter *
        // See benchmarks/README.md for options and how results are recorded.
        public static void Main(string[] args) =>
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
