#nullable enable

using System;
using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.CalcEngineTests
{
    /// <summary>
    /// Error handling of the calculation engine and of imports, found while annotating the public
    /// API for nullable reference types (R-038).
    /// </summary>
    [TestFixture]
    public class CalcEngineErrorTests
    {
        [Test(Description = "An unknown name in an engine expression is a compile error")]
        public void UnknownNameIsUndefinedNameCompileError()
        {
            var engine = new CalculationEngine();
            var context = new ExpressionContext();

            var ex = Assert.Throws<ExpressionCompileException>(() => engine.Add("a", "1 + zzz", context));
            Assert.AreEqual(CompileExceptionReason.UndefinedName, ex!.Reason);
        }

        [Test(Description = "A failed Add leaves no trace, so the name can be added again")]
        [Category("KnownFailure")]
        [Ignore("Known failure: the temporary head stays, Contains is true and a second Add fails")]
        public void FailedAddLeavesNoAtomBehind()
        {
            var engine = new CalculationEngine();
            var context = new ExpressionContext();

            Assert.Throws<ExpressionCompileException>(() => engine.Add("a", "1 +", context));

            Assert.IsFalse(engine.Contains("a"));
            Assert.AreEqual(0, engine.Count);
            engine.Add("a", "1 + 2", context);
            Assert.AreEqual(3, engine.GetResult<int>("a"));
        }

        [Test(Description = "Comparing namespace imports that are not attached to a context")]
        [Category("KnownFailure")]
        [Ignore("Known failure: NullReferenceException in NamespaceImport.EqualsInternal")]
        public void DetachedNamespaceImportsCanBeCompared()
        {
            var a = new NamespaceImport("a");

            Assert.IsFalse(a.Equals(new NamespaceImport("b")));
            Assert.IsTrue(a.Equals(new NamespaceImport("a")));
        }
    }
}
