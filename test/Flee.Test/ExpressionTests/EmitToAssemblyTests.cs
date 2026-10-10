using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.ExpressionTests
{
    /// <summary>
    /// Pins what the EmitToAssembly option does for a caller: setting it keeps compiling and
    /// evaluating expressions as usual. Upstream emitted the IL a second time into an in-memory
    /// assembly that was never saved; since Phase 3 the option is a no-op.
    /// </summary>
    [TestFixture]
    public class EmitToAssemblyTests
    {
#pragma warning disable CS0618 // EmitToAssembly is obsolete; this test exercises it on purpose.

        [Test]
        public void EmitToAssembly_IsStoredAndExpressionsStillEvaluate()
        {
            var context = new ExpressionContext();
            context.Options.EmitToAssembly = true;
            context.Variables["a"] = 20;

            Assert.IsTrue(context.Options.EmitToAssembly);
            Assert.AreEqual(3, context.CompileDynamic("1 + 2").Evaluate());
            Assert.AreEqual(42, context.CompileGeneric<int>("a * 2 + 2").Evaluate());
            Assert.AreEqual("ab", context.CompileDynamic("if(a > 10, \"a\" + \"b\", \"c\")").Evaluate());
        }

        [Test]
        public void EmitToAssembly_IsOffByDefault()
        {
            Assert.IsFalse(new ExpressionContext().Options.EmitToAssembly);
        }

#pragma warning restore CS0618
    }
}
