using System.Globalization;
using System.Text;
using Flee.PublicTypes;

namespace Flee.Benchmarks
{
    /// <summary>
    /// A test vector: one expression plus the context it needs. Each vector runs through the
    /// three stages in <see cref="StageBenchmarks"/>. The set follows the table in the plan
    /// (Phase 2, "Test vectors"); the three "Legacy" vectors are the expressions of the timing
    /// tests that used to live in the test project (Benchmarks.cs).
    /// </summary>
    public sealed class Vector
    {
        private readonly Func<ExpressionContext> _createContext;

        public Vector(string name, string expression, Func<ExpressionContext> createContext)
        {
            Name = name;
            Expression = expression;
            _createContext = createContext;
        }

        public string Name { get; }
        public string Expression { get; }

        /// <summary>
        /// Creates the context. Parser defaults follow the current culture, and the expressions
        /// use '.' and ',', so the culture is fixed first.
        /// </summary>
        public ExpressionContext CreateContext()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            return _createContext();
        }

        // BenchmarkDotNet shows parameters by ToString().
        public override string ToString() => Name;
    }

    public class Order
    {
        public double Price = 12.5;
        public int Quantity = 4;
        public string Customer { get; } = "ACME";
    }

    public static class Vectors
    {
        public static IEnumerable<Vector> All()
        {
            yield return new Vector("Constants", "1 + 2 * 3", () => new ExpressionContext());

            yield return new Vector("ArithmeticVariables", "sqrt(a^2 + b^2)", () =>
            {
                var c = WithMath();
                c.Variables["a"] = 3.0;
                c.Variables["b"] = 4.0;
                return c;
            });

            yield return new Vector("ManyVariables", Join(" + ", 50, i => $"v{i}"), () =>
            {
                var c = new ExpressionContext();
                for (int i = 0; i < 50; i++)
                {
                    c.Variables[$"v{i}"] = i;
                }
                return c;
            });

            yield return new Vector("OwnerMembers", "Price * Quantity", () => new ExpressionContext(new Order()));

            yield return new Vector("MixedNumeric", "i * d + l - f / i + cast(l, double) * i", () =>
            {
                var c = new ExpressionContext();
                c.Variables["i"] = 7;
                c.Variables["d"] = 2.5;
                c.Variables["l"] = 10000000000L;
                c.Variables["f"] = 1.5f;
                return c;
            });

            yield return new Vector("Strings", "s1 + \" \" + s2 = \"hello world\" and s1.Length > 3", () =>
            {
                var c = new ExpressionContext();
                c.Variables["s1"] = "hello";
                c.Variables["s2"] = "world";
                return c;
            });

            yield return new Vector("LogicChain",
                Join(" or ", 20, i => $"(x > {i} and y < {i * 2})"),
                () =>
                {
                    var c = new ExpressionContext();
                    c.Variables["x"] = 5;
                    c.Variables["y"] = 3;
                    return c;
                });

            yield return new Vector("NestedIf",
                "if(x > 10, if(y > 10, 1, 2), if(y > 5, if(x = y, 3, 4), if(x < 0, 5, if(y < 0, 6, 7))))",
                () =>
                {
                    var c = new ExpressionContext();
                    c.Variables["x"] = 5;
                    c.Variables["y"] = 3;
                    return c;
                });

            yield return new Vector("InList", "x in (" + Join(", ", 20, i => (i * 3).ToString(CultureInfo.InvariantCulture)) + ")", () =>
            {
                var c = new ExpressionContext();
                c.Variables["x"] = 42;
                return c;
            });

            yield return new Vector("Casts", "cast(d, int) + cast(i, long) + cast(cast(l, double) / 3, single)", () =>
            {
                var c = new ExpressionContext();
                c.Imports.ImportBuiltinTypes();
                c.Variables["d"] = 2.5;
                c.Variables["i"] = 7;
                c.Variables["l"] = 9L;
                return c;
            });

            yield return new Vector("OnDemand", "va + vb * twice(vc)", () =>
            {
                var c = new ExpressionContext();
                c.Variables.ResolveVariableType += (s, e) => e.VariableType = typeof(int);
                c.Variables.ResolveVariableValue += (s, e) => e.VariableValue = 3;
                c.Variables.ResolveFunction += (s, e) => e.ReturnType = typeof(int);
                c.Variables.InvokeFunction += (s, e) => e.Result = 2 * (int)e.Arguments[0];
                return c;
            });

            yield return new Vector("Large", Join(" + ", 300, i => $"a * {i % 7 + 1} - b / {i % 5 + 1}"), () =>
            {
                var c = new ExpressionContext();
                c.Variables["a"] = 3.0;
                c.Variables["b"] = 4.0;
                return c;
            });

            yield return new Vector("LegacyBig", LegacyExpressions.Big, LegacyExpressions.CreateContext);
            yield return new Vector("LegacySmall", LegacyExpressions.Small, LegacyExpressions.CreateContext);
            yield return new Vector("LegacySmallBranching", LegacyExpressions.SmallBranching, LegacyExpressions.CreateContext);
        }

        private static ExpressionContext WithMath()
        {
            var c = new ExpressionContext();
            c.Imports.AddType(typeof(Math));
            return c;
        }

        private static string Join(string separator, int count, Func<int, string> term)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(separator);
                sb.Append(term(i));
            }
            return sb.ToString();
        }
    }
}
