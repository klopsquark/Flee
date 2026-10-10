#nullable enable

using System;
using System.Globalization;
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
        [Category("KnownFailure")]
        [Ignore("Known failure: the clone shares its parse culture and recreates the original's parser (D-28)")]
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
        [Category("KnownFailure")]
        [Ignore("Known failure: the clone's options change the original's parser options (D-28)")]
        public void ParseCultureOfCloneIsIndependent()
        {
            var original = new ExpressionContext();
            ExpressionContext clone = original.Clone();

            clone.Options.ParseCulture = new CultureInfo("de-DE");

            Assert.AreEqual(',', clone.ParserOptions.DecimalSeparator);
            Assert.AreEqual('.', original.ParserOptions.DecimalSeparator);
        }

        [Test(Description = "A type imported into a clone is not visible in the original")]
        [Category("KnownFailure")]
        [Ignore("Known failure: the clone shares the root import's list (D-28)")]
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
        [Category("KnownFailure")]
        [Ignore("Known failure: the clone's options still notify the original's variables (D-28)")]
        public void CaseSensitiveOnCloneKeepsOriginalVariables()
        {
            var original = new ExpressionContext();
            original.Variables["x"] = 1;
            ExpressionContext clone = original.Clone();

            clone.Options.CaseSensitive = true;

            Assert.IsTrue(original.Variables.ContainsKey("x"));
            Assert.AreEqual(2, original.CompileDynamic("x + 1").Evaluate());
        }
    }
}
