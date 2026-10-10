#nullable enable

using System;
using System.Globalization;
using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.ExpressionTests
{
    /// <summary>
    /// A cloned context has its own options, parser options and imports (doc/deferred.md, D-28,
    /// found while writing the XML comments, R-048). Changing the clone must not change the original.
    /// </summary>
    [TestFixture]
    public class ContextCloneTests
    {
        [Test(Description = "Parser options changed on a clone apply to the clone only")]
        public void ParserOptionsOfCloneAreIndependent()
        {
            var original = new ExpressionContext();
            ExpressionContext clone = original.Clone();

            clone.ParserOptions.DecimalSeparator = ',';
            clone.ParserOptions.FunctionArgumentSeparator = ';';
            clone.ParserOptions.RecreateParser();

            Assert.AreEqual(1.5, clone.CompileDynamic("1,5").Evaluate());
            Assert.AreEqual(1.5, original.CompileDynamic("1.5").Evaluate());
            Assert.AreEqual('.', original.ParserOptions.DecimalSeparator);
        }

        [Test(Description = "ParseCulture set on a clone changes the clone's parser options only")]
        public void ParseCultureOfCloneIsIndependent()
        {
            var original = new ExpressionContext();
            ExpressionContext clone = original.Clone();

            clone.Options.ParseCulture = new CultureInfo("de-DE");

            Assert.AreEqual(',', clone.ParserOptions.DecimalSeparator);
            Assert.AreEqual('.', original.ParserOptions.DecimalSeparator);
        }

        [Test(Description = "A type imported into a clone is not visible in the original")]
        public void ImportsOfCloneAreIndependent()
        {
            var original = new ExpressionContext();
            ExpressionContext clone = original.Clone();

            clone.Imports.AddType(typeof(Math));

            Assert.AreEqual(1.0, clone.CompileDynamic("abs(-1.0)").Evaluate());
            var ex = Assert.Throws<ExpressionCompileException>(() => original.CompileDynamic("abs(-1.0)"));
            Assert.AreEqual(CompileExceptionReason.UndefinedName, ex!.Reason);
        }

        [Test(Description = "Changing CaseSensitive on a clone leaves the original's variables alone")]
        public void CaseSensitiveOnCloneKeepsOriginalVariables()
        {
            var original = new ExpressionContext();
            original.Variables["x"] = 1;
            ExpressionContext clone = original.Clone();

            clone.Options.CaseSensitive = true;

            Assert.IsTrue(original.Variables.ContainsKey("x"));
            Assert.AreEqual(2, original.CompileDynamic("x + 1").Evaluate());
        }

        [Test(Description = "RecreateParser also renews the parser that finds names for the calculation engines")]
        public void RecreateParserRenewsIdentifierParser()
        {
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            var engine = new CalculationEngine();
            engine.CreateBatchLoader().Add("a", "1.5", context);   // creates the identifier parser
            ExpressionContext clone = context.Clone();

            clone.ParserOptions.DecimalSeparator = ',';
            clone.ParserOptions.FunctionArgumentSeparator = ';';
            clone.ParserOptions.RecreateParser();

            BatchLoader loader = engine.CreateBatchLoader();
            loader.Add("a", "max(1,5; 2)", clone);
            engine.BatchLoad(loader);
            Assert.AreEqual(2.0, engine.GetResult<double>("a"));
        }
    }
}
