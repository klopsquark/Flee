#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.DocumentationTests
{
    /// <summary>
    /// The code examples of doc/api-guide.md. Each region named "doc:..." is copied verbatim into
    /// the guide; ApiGuideSyncTests checks that guide and regions match, and the asserts after each
    /// region check that the example does what the guide says.
    /// </summary>
    [TestFixture]
    public class ApiGuideExamples
    {
        #region doc:OrderClass
        public class Order
        {
            public double Price = 12.5;
            public int Quantity = 4;
            private string _customer = "ACME";
            public string Customer => _customer;
        }
        #endregion

        [Test]
        public void QuickStart()
        {
            #region doc:QuickStart
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Variables["a"] = 3.0;
            context.Variables["b"] = 4.0;

            IGenericExpression<double> e = context.CompileGeneric<double>("sqrt(a^2 + b^2)");
            double result = e.Evaluate();   // 5
            #endregion

            Assert.AreEqual(5.0, result);
        }

        [Test]
        public void DynamicAndGeneric()
        {
            #region doc:DynamicAndGeneric
            var context = new ExpressionContext();

            IDynamicExpression dynamic = context.CompileDynamic("1 + 2");
            object? boxed = dynamic.Evaluate();             // 3, an int

            IGenericExpression<int> typed = context.CompileGeneric<int>("1 + 2");
            int value = typed.Evaluate();                   // 3, no boxing

            IGenericExpression<double> widened = context.CompileGeneric<double>("1 + 2");
            double converted = widened.Evaluate();          // 3.0: int converts implicitly to double
            #endregion

            Assert.AreEqual(3, boxed);
            Assert.AreEqual(3, value);
            Assert.AreEqual(3.0, converted);
            Assert.Throws<ExpressionCompileException>(() => context.CompileGeneric<int>("1.5"));
        }

        [Test]
        public void Variables()
        {
            #region doc:Variables
            var context = new ExpressionContext();
            context.Variables["price"] = 10.0;
            context.Variables.DefineVariable("quantity", typeof(int));   // typed, value 0

            IGenericExpression<double> total = context.CompileGeneric<double>("price * quantity");

            context.Variables["quantity"] = 3;
            double first = total.Evaluate();     // 30

            context.Variables["price"] = 12.5;   // no recompiling needed
            double second = total.Evaluate();    // 37.5

            string[] used = total.Info.GetReferencedVariables();   // price, quantity
            #endregion

            Assert.AreEqual(30.0, first);
            Assert.AreEqual(37.5, second);
            CollectionAssert.AreEquivalent(new[] { "price", "quantity" }, used);
        }

        [Test]
        public void Imports()
        {
            #region doc:Imports
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));                     // cos(0), pi
            context.Imports.AddType(typeof(DateTime), "DateTime");     // DateTime.Today
            // A single method; for overloaded methods pass the MethodInfo instead of its name.
            MethodInfo join = typeof(string).GetMethod("Join", new[] { typeof(string), typeof(string[]) })!;
            context.Imports.AddMethod(join, "text");                  // text.join(", ", ...)
            context.Imports.ImportBuiltinTypes();                      // int.MaxValue, cast(x, long)

            object? pi = context.CompileDynamic("pi").Evaluate();
            object? joined = context.CompileDynamic("text.join(\", \", \"a\", \"b\")").Evaluate();
            object? max = context.CompileDynamic("cast(int.MaxValue, long) + 1").Evaluate();
            #endregion

            Assert.AreEqual(Math.PI, pi);
            Assert.AreEqual("a, b", joined);
            Assert.AreEqual(2147483648L, max);
            Assert.AreEqual(DateTime.Today, context.CompileDynamic("DateTime.Today").Evaluate());
        }

        [Test]
        public void Owner()
        {
            #region doc:Owner
            var order = new Order { Price = 12.5, Quantity = 4 };
            var context = new ExpressionContext(order);

            IGenericExpression<double> total = context.CompileGeneric<double>("Price * Quantity");
            double first = total.Evaluate();                // 50

            total.Owner = new Order { Price = 2, Quantity = 3 };
            double second = total.Evaluate();               // 6

            context.Options.OwnerMemberAccess = BindingFlags.Public | BindingFlags.NonPublic;
            string? customer = context.CompileGeneric<string>("_customer").Evaluate();   // ACME
            #endregion

            Assert.AreEqual(50.0, first);
            Assert.AreEqual(6.0, second);
            Assert.AreEqual("ACME", customer);
        }

        [Test]
        public void OnDemand()
        {
            #region doc:OnDemand
            var context = new ExpressionContext();
            var values = new Dictionary<string, double> { ["width"] = 3, ["height"] = 4 };

            context.Variables.ResolveVariableType += (sender, e) =>
                e.VariableType = values.ContainsKey(e.VariableName) ? typeof(double) : null;
            context.Variables.ResolveVariableValue += (sender, e) =>
                e.VariableValue = values[e.VariableName];

            context.Variables.ResolveFunction += (sender, e) =>
            {
                if (e.FunctionName == "area") e.ReturnType = typeof(double);
            };
            context.Variables.InvokeFunction += (sender, e) =>
                e.Result = (double)e.Arguments[0]! * (double)e.Arguments[1]!;

            IGenericExpression<double> area = context.CompileGeneric<double>("area(width, height)");
            double result = area.Evaluate();   // 12

            values["width"] = 5;
            double updated = area.Evaluate();  // 20: values are fetched on every evaluation
            #endregion

            Assert.AreEqual(12.0, result);
            Assert.AreEqual(20.0, updated);
        }

        [Test]
        public void Options()
        {
            #region doc:Options
            var context = new ExpressionContext();

            context.Options.Checked = true;                                  // overflow throws
            context.Options.IntegersAsDoubles = true;                        // 7 / 2 = 3.5
            context.Options.RealLiteralDataType = RealLiteralDataType.Decimal; // 0.1 is a decimal
            context.Options.StringComparison = StringComparison.OrdinalIgnoreCase; // "a" = "A"
            context.Options.ParseCulture = CultureInfo.InvariantCulture;     // '.' and ',' everywhere
            #endregion

            Assert.AreEqual(3.5, context.CompileDynamic("7 / 2").Evaluate());
            Assert.AreEqual(typeof(decimal), context.CompileDynamic("0.1").Evaluate()!.GetType());
            Assert.AreEqual(true, context.CompileDynamic("\"a\" = \"A\"").Evaluate());
            var checkedContext = new ExpressionContext();
            checkedContext.Options.Checked = true;
            Assert.Throws<OverflowException>(() => checkedContext.CompileDynamic("2000000000 * 2").Evaluate());
        }

        [Test]
        public void Errors()
        {
            string? message = null;
            CompileExceptionReason? reason = null;

            #region doc:Errors
            var context = new ExpressionContext();
            try
            {
                context.CompileDynamic("1 + unknown");
            }
            catch (ExpressionCompileException ex)
            {
                reason = ex.Reason;     // CompileExceptionReason.UndefinedName
                message = ex.Message;   // names the problem and the element that found it
            }
            #endregion

            Assert.AreEqual(CompileExceptionReason.UndefinedName, reason);
            StringAssert.Contains("unknown", message);
        }

        [Test]
        public void CalculationEngine()
        {
            #region doc:CalculationEngine
            var engine = new CalculationEngine();
            var context = new ExpressionContext();
            context.Variables["netPrice"] = 100.0;

            engine.Add("tax", "netPrice * 0.19", context);
            engine.Add("gross", "netPrice + tax", context);

            double gross = engine.GetResult<double>("gross");          // 119

            context.Variables["netPrice"] = 200.0;
            engine.Recalculate("tax");                                 // tax and everything after it
            double updated = engine.GetResult<double>("gross");        // 238

            string[] dependents = engine.GetDependents("tax");         // gross
            string[] precedents = engine.GetPrecedents("gross");       // tax
            #endregion

            Assert.AreEqual(119.0, gross, 1e-9);
            Assert.AreEqual(238.0, updated, 1e-9);
            CollectionAssert.AreEquivalent(new[] { "gross" }, dependents);
            CollectionAssert.AreEquivalent(new[] { "tax" }, precedents);
        }

        [Test]
        public void BatchLoad()
        {
            #region doc:BatchLoad
            var engine = new CalculationEngine();
            var context = new ExpressionContext();

            BatchLoader loader = engine.CreateBatchLoader();
            loader.Add("c", "a + b", context);   // order does not matter in a batch
            loader.Add("a", "1", context);
            loader.Add("b", "a * 2", context);
            engine.BatchLoad(loader);

            int c = engine.GetResult<int>("c");  // 3
            #endregion

            Assert.AreEqual(3, c);
        }

        [Test]
        public void SimpleCalcEngine()
        {
            #region doc:SimpleCalcEngine
            var engine = new SimpleCalcEngine();
            engine.Context = new ExpressionContext();
            engine.AddGeneric<int>("a", "10");
            engine.AddDynamic("b", "a * 2");

            object? b = ((IDynamicExpression)engine["b"]!).Evaluate();   // 20
            #endregion

            Assert.AreEqual(20, b);
        }
    }
}
