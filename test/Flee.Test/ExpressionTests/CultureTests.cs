using System;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.ExpressionTests
{
    /// <summary>
    /// Pins how the parser defaults follow the current culture. The rest of the suite runs under
    /// en-GB (TestCulture.cs); these tests set their own culture.
    /// </summary>
    [TestFixture]
    public class CultureTests
    {
        private static ExpressionContext CreateContext()
        {
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            return context;
        }

        [Test]
        [SetCulture("de-DE")]
        public void GermanCulture_SetsParserDefaults()
        {
            ExpressionParserOptions options = CreateContext().ParserOptions;

            Assert.AreEqual(',', options.DecimalSeparator);
            Assert.AreEqual(';', options.FunctionArgumentSeparator);
            Assert.AreEqual("dd.MM.yyyy", options.DateTimeFormat);
        }

        [Test]
        [SetCulture("de-DE")]
        public void GermanCulture_UsesCommaDecimalsAndSemicolonArguments()
        {
            ExpressionContext context = CreateContext();

            Assert.AreEqual(2.5, context.CompileDynamic("1,5 + 1").Evaluate());
            Assert.AreEqual(2, context.CompileDynamic("max(1; 2)").Evaluate());
            Assert.AreEqual(new DateTime(2008, 2, 11), context.CompileDynamic("#11.02.2008#").Evaluate());

            var ex = Assert.Throws<ExpressionCompileException>(() => context.CompileDynamic("1.5 + 1"));
            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex!.Reason);
            ex = Assert.Throws<ExpressionCompileException>(() => context.CompileDynamic("max(1, 2)"));
            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex!.Reason);
        }

        [Test]
        [SetCulture("en-GB")]
        public void EnglishCulture_UsesPointDecimalsAndCommaArguments()
        {
            ExpressionContext context = CreateContext();

            Assert.AreEqual(2.5, context.CompileDynamic("1.5 + 1").Evaluate());
            Assert.AreEqual(2, context.CompileDynamic("max(1, 2)").Evaluate());
            Assert.AreEqual(new DateTime(2008, 2, 11), context.CompileDynamic("#11/02/2008#").Evaluate());
        }

        [Test]
        [SetCulture("")]
        public void InvariantCulture_UsesMonthFirstDates()
        {
            Assert.AreEqual("MM/dd/yyyy", CreateContext().ParserOptions.DateTimeFormat);
            Assert.AreEqual(new DateTime(2008, 2, 11), CreateContext().CompileDynamic("#02/11/2008#").Evaluate());
        }

        /// <summary>
        /// Known bug, upstream issue #105: keywords are lowercased with the current culture, so in
        /// Turkish an upper-case "I" becomes a dotless "ı" and "IF" or "IN" no longer parse. Lower-case
        /// keywords work. This test records today's behaviour; Phase 4 fixes it and flips the test.
        /// </summary>
        [Test]
        [SetCulture("tr-TR")]
        public void TurkishCulture_UpperCaseKeywordsWithI_FailToParse()
        {
            ExpressionContext context = CreateContext();

            Assert.AreEqual(1, context.CompileDynamic("if(1 < 2; 1; 2)").Evaluate());

            var ex = Assert.Throws<ExpressionCompileException>(() => context.CompileDynamic("IF(1 < 2; 1; 2)"));
            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex!.Reason);
            ex = Assert.Throws<ExpressionCompileException>(() => context.CompileDynamic("1 IN (1; 2)"));
            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex!.Reason);
        }
    }
}
