using System.ComponentModel.Design;
using BenchmarkDotNet.Attributes;
using Flee.PublicTypes;

namespace Flee.Benchmarks
{
    /// <summary>
    /// Every vector through the three stages, measured separately:
    /// Parse (tokenize, parse, build the element tree), Compile (the full public compile:
    /// context clone, parse, IL emission, delegate creation) and Evaluate (one call of a
    /// compiled expression).
    /// </summary>
    [MemoryDiagnoser]
    public class StageBenchmarks
    {
        private ExpressionContext _context = null!;
        private IDynamicExpression _compiled = null!;

        public static IEnumerable<Vector> Vectors() => Benchmarks.Vectors.All();

        [ParamsSource(nameof(Vectors))]
        public Vector Vector { get; set; } = null!;

        [GlobalSetup]
        public void Setup()
        {
            _context = Vector.CreateContext();
            _compiled = _context.CompileDynamic(Vector.Expression);
        }

        /// <summary>
        /// Parse only. Mirrors what Expression&lt;T&gt; does before Parse (clone the context, set up
        /// options and compile services), using internals made visible to this assembly.
        /// Element construction resolves names and types, so this is parse plus binding,
        /// without IL emission.
        /// </summary>
        [Benchmark]
        public object? Parse()
        {
            ExpressionContext context = _context.CloneInternal(false);
            ExpressionOptions options = context.Options;
            options.IsGeneric = false;
            options.SetOwnerType(context.ExpressionOwner.GetType());
            context.Imports.ImportOwner(options.OwnerType!);

            var services = new ServiceContainer();
            services.AddService(typeof(ExpressionOptions), options);
            services.AddService(typeof(ExpressionParserOptions), context.ParserOptions);
            services.AddService(typeof(ExpressionContext), context);
            services.AddService(typeof(ExpressionInfo), new ExpressionInfo());

            return context.Parse(Vector.Expression, services);
        }

        [Benchmark]
        public IDynamicExpression Compile() => _context.CompileDynamic(Vector.Expression);

        [Benchmark]
        public object? Evaluate() => _compiled.Evaluate();

        /// <summary>
        /// Compile plus the first evaluation, which is when the runtime JIT-compiles the generated
        /// method. This is the cost an application pays once per expression at startup. Added in
        /// Phase 4 after .NET 10 turned out to make that first call much slower (R-019).
        /// </summary>
        [Benchmark]
        public object? CompileAndEvaluate() => _context.CompileDynamic(Vector.Expression).Evaluate();
    }
}
