#nullable enable

using System;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.ExpressionTests
{
    /// <summary>
    /// A variable created with DefineVariable and not yet given a value reads as its type's
    /// default, as after setting it to null through the indexer (doc/deferred.md, D-27, found while
    /// writing the XML comments, R-048).
    /// </summary>
    [TestFixture]
    public class DefineVariableTests
    {
        [Test]
        public void ValueTypeVariableWithoutValueReadsAsDefault()
        {
            var context = new ExpressionContext();
            context.Variables.DefineVariable("x", typeof(int));

            Assert.AreEqual(1, context.CompileGeneric<int>("x + 1").Evaluate());
            Assert.AreEqual(0, context.Variables["x"]);
        }

        [Test]
        public void DateTimeVariableWithoutValueReadsAsDefault()
        {
            var context = new ExpressionContext();
            context.Variables.DefineVariable("d", typeof(DateTime));

            Assert.AreEqual(1, context.CompileGeneric<int>("d.Year").Evaluate());
        }

        [Test]
        public void ReferenceTypeVariableWithoutValueReadsAsNull()
        {
            var context = new ExpressionContext();
            context.Variables.DefineVariable("s", typeof(string));

            Assert.IsNull(context.CompileDynamic("s").Evaluate());
        }

        [Test(Description = "Today's behaviour of the indexer, which the defined variable now matches")]
        public void IndexerSetToNullStoresDefault()
        {
            var context = new ExpressionContext();
            context.Variables["x"] = 5;
            context.Variables["x"] = null;

            Assert.AreEqual(1, context.CompileGeneric<int>("x + 1").Evaluate());
        }
    }
}
