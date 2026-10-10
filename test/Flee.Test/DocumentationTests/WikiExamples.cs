#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.DocumentationTests
{
    /// <summary>
    /// The code examples of the fork's wiki (wiki/*.md) that are not already in the API guide. Each
    /// region named "doc:..." is copied verbatim into a wiki page; WikiSyncTests checks that pages
    /// and regions match, and the asserts after each region check that the example does what the
    /// page says.
    /// </summary>
    [TestFixture]
    public class WikiExamples
    {
        #region doc:CustomFunctionsClass
        public static class CustomFunctions
        {
            public static int Product(int a, int b) => a * b;

            public static int Sum(params int[] values)
            {
                int sum = 0;
                foreach (int value in values) sum += value;
                return sum;
            }
        }
        #endregion

        #region doc:IndexingOwnerClass
        public class Inventory
        {
            public int[] Stock = { 10, 20, 30 };
            public List<string> Names = new List<string> { "bolt", "nut", "washer" };
            public Dictionary<string, double> Prices = new Dictionary<string, double> { ["bolt"] = 0.25 };
        }
        #endregion

        [Test]
        public void BooleanFilter()
        {
            #region doc:BooleanFilter
            var context = new ExpressionContext();
            context.Variables["age"] = 0;
            context.Variables["country"] = "";

            IGenericExpression<bool> filter =
                context.CompileGeneric<bool>("age >= 18 and (country = \"DE\" or country = \"AT\")");

            var people = new[] { (17, "DE"), (30, "AT"), (45, "FR") };
            var accepted = new List<int>();
            foreach (var (age, country) in people)
            {
                context.Variables["age"] = age;
                context.Variables["country"] = country;
                if (filter.Evaluate()) accepted.Add(age);   // only 30
            }
            #endregion

            CollectionAssert.AreEqual(new[] { 30 }, accepted);
        }

        [Test]
        public void CustomFunctions_Import()
        {
            #region doc:CustomFunctions
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(CustomFunctions));                 // product(...), sum(...)
            context.Imports.AddType(typeof(CustomFunctions), "fn");           // fn.product(...)
            context.Variables["a"] = 100;
            context.Variables["b"] = 200;

            int plain = context.CompileGeneric<int>("product(a, b) + sum(a, b)").Evaluate();   // 20300
            int prefixed = context.CompileGeneric<int>("fn.product(a, b) - a").Evaluate();    // 19900
            int many = context.CompileGeneric<int>("sum(1, 2, 3, 4, 5, 6)").Evaluate();       // 21
            #endregion

            Assert.AreEqual(20300, plain);
            Assert.AreEqual(19900, prefixed);
            Assert.AreEqual(21, many);
        }

        [Test]
        public void InstanceMembers()
        {
            #region doc:InstanceMembers
            var context = new ExpressionContext();
            context.Variables["s"] = "this is a string";
            context.Variables["start"] = new DateTime(2026, 10, 10);

            int length = context.CompileGeneric<int>("s.Length + s.Remove(0, 1).Length").Evaluate();   // 31
            string upper = context.CompileGeneric<string>("s.ToUpper().Substring(0, 4)").Evaluate();  // THIS
            int month = context.CompileGeneric<int>("start.AddDays(30).Month").Evaluate();            // 11
            #endregion

            Assert.AreEqual(31, length);
            Assert.AreEqual("THIS", upper);
            Assert.AreEqual(11, month);
        }

        [Test]
        public void Indexing()
        {
            #region doc:Indexing
            var context = new ExpressionContext(new Inventory());
            context.Variables["i"] = 1;
            context.Variables["matrix"] = new[] { 1, 2, 3 };

            int stock = context.CompileGeneric<int>("Stock[i + 1]").Evaluate();            // 30
            string name = context.CompileGeneric<string>("Names[i]").Evaluate();           // nut
            double price = context.CompileGeneric<double>("Prices[\"bolt\"]").Evaluate();  // 0.25
            int element = context.CompileGeneric<int>("matrix[2]").Evaluate();            // 3
            #endregion

            Assert.AreEqual(30, stock);
            Assert.AreEqual("nut", name);
            Assert.AreEqual(0.25, price);
            Assert.AreEqual(3, element);
        }

        [Test]
        public void ExpressionsAsVariables()
        {
            #region doc:ExpressionsAsVariables
            var inner = new ExpressionContext();
            inner.Imports.AddType(typeof(Math));
            inner.Variables["x"] = 3.14;
            IDynamicExpression cos2 = inner.CompileDynamic("cos(x) ^ 2");
            IDynamicExpression sin2 = inner.CompileDynamic("sin(x) ^ 2");

            var outer = new ExpressionContext();
            outer.Variables["a"] = cos2;
            outer.Variables["b"] = sin2;
            IGenericExpression<double> sum = outer.CompileGeneric<double>("a + b");

            double one = sum.Evaluate();      // 1, within rounding
            inner.Variables["x"] = 1.0;
            double stillOne = sum.Evaluate(); // 1 again: the inner expressions are evaluated each time
            #endregion

            Assert.AreEqual(1.0, one, 1e-12);
            Assert.AreEqual(1.0, stillOne, 1e-12);
        }

        [Test]
        public void Culture()
        {
            #region doc:Culture
            var german = new ExpressionContext();
            german.Imports.AddType(typeof(Math));
            german.Options.ParseCulture = new CultureInfo("de-DE");          // ',' decimals, ';' between arguments
            double a = german.CompileGeneric<double>("max(1,5; 2,25)").Evaluate();   // 2.25

            var custom = new ExpressionContext();
            custom.Imports.AddType(typeof(Math));
            custom.Options.ParseCulture = CultureInfo.InvariantCulture;
            custom.ParserOptions.FunctionArgumentSeparator = ';';
            custom.ParserOptions.RecreateParser();                            // needed after changing ParserOptions
            double b = custom.CompileGeneric<double>("max(1.5; 2.25)").Evaluate();   // 2.25
            #endregion

            Assert.AreEqual(2.25, a);
            Assert.AreEqual(2.25, b);
        }
    }
}
