using System.Globalization;
using BenchmarkDotNet.Attributes;
using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;

namespace Flee.Benchmarks
{
    /// <summary>
    /// Calculation engine with 100 dependent expressions: loading them, and recalculating after
    /// one input changed.
    /// </summary>
    [MemoryDiagnoser]
    public class CalculationEngineBenchmarks
    {
        private const int AtomCount = 100;
        private CalculationEngine _engine = null!;
        private ExpressionContext _context = null!;
        private int _x;

        [GlobalSetup]
        public void Setup()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            (_engine, _context) = Load();
        }

        private static (CalculationEngine, ExpressionContext) Load()
        {
            var engine = new CalculationEngine();
            var context = new ExpressionContext();
            context.Variables["x"] = 1;
            engine.Add("a0", "x * 2", context);
            for (int i = 1; i < AtomCount; i++)
            {
                // Each atom depends on its predecessor and on one further back.
                engine.Add($"a{i}", $"a{i - 1} + a{i / 2} % 7 + {i}", context);
            }
            return (engine, context);
        }

        [Benchmark]
        public CalculationEngine Load100() => Load().Item1;

        [Benchmark]
        public int RecalculateAfterInputChange()
        {
            _context.Variables["x"] = ++_x;
            _engine.Recalculate("a0");
            return _engine.GetResult<int>($"a{AtomCount - 1}");
        }
    }

    /// <summary>
    /// Writing two variables and evaluating, the loop of the former TestFastVariables test.
    /// </summary>
    [MemoryDiagnoser]
    public class VariableBenchmarks
    {
        private VariableCollection _variables = null!;
        private IDynamicExpression _expression = null!;

        [GlobalSetup]
        public void Setup()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var context = new ExpressionContext();
            _variables = context.Variables;
            _variables.DefineVariable("a", typeof(int));
            _variables.DefineVariable("b", typeof(int));
            _expression = context.CompileDynamic("a + b");
        }

        [Benchmark]
        public object? SetVariablesAndEvaluate()
        {
            _variables["a"] = 200;
            _variables["b"] = 300;
            return _expression.Evaluate();
        }
    }
}
