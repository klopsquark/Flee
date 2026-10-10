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
        public void DetachedNamespaceImportsCanBeCompared()
        {
            var a = new NamespaceImport("a");

            Assert.IsFalse(a.Equals(new NamespaceImport("b")));
            Assert.IsTrue(a.Equals(new NamespaceImport("a")));
            Assert.IsTrue(a.Equals(new NamespaceImport("A")), "case-insensitive, as Flee's default options");
        }

        // Found while writing the XML comments (R-048), recorded in doc/deferred.md.

        [Test(Description = "D-25: a syntax error in a batch expression is a compile error and adds nothing")]
        public void BatchSyntaxErrorIsCompileError()
        {
            var engine = new CalculationEngine();
            var context = new ExpressionContext();
            BatchLoader loader = engine.CreateBatchLoader();

            var ex = Assert.Throws<ExpressionCompileException>(() => loader.Add("a", "1 +", context));
            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex!.Reason);
            Assert.IsFalse(loader.Contains("a"));
        }

        [Test(Description = "D-25: a syntax error in a SimpleCalcEngine expression is a compile error")]
        public void SimpleCalcEngineSyntaxErrorIsCompileError()
        {
            var engine = new SimpleCalcEngine();

            var ex = Assert.Throws<ExpressionCompileException>(() => engine.AddDynamic("a", "1 +"));
            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex!.Reason);
            Assert.IsNull(engine["a"]);
        }

        [Test(Description = "D-26: an unknown name in a batch is a compile error naming the atom")]
        [Category("KnownFailure")]
        [Ignore("Known failure: KeyNotFoundException from BatchLoader.GetBachInfos (D-26)")]
        public void BatchUnknownNameIsCompileError()
        {
            var engine = new CalculationEngine();
            var context = new ExpressionContext();
            BatchLoader loader = engine.CreateBatchLoader();
            loader.Add("a", "1 + zzz", context);

            var ex = Assert.Throws<BatchLoadCompileException>(() => engine.BatchLoad(loader));
            Assert.AreEqual("a", ex!.AtomName);
            Assert.AreEqual(CompileExceptionReason.UndefinedName, ((ExpressionCompileException)ex.InnerException!).Reason);
            Assert.AreEqual(0, engine.Count);
        }

        [Test(Description = "D-26: a batch expression may call imported functions")]
        public void BatchCanCallImportedFunctions()
        {
            var engine = new CalculationEngine();
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            BatchLoader loader = engine.CreateBatchLoader();
            loader.Add("b", "a * 2", context);
            loader.Add("a", "sqrt(16)", context);

            engine.BatchLoad(loader);

            Assert.AreEqual(8.0, engine.GetResult<double>("b"));
        }

        [Test(Description = "D-29: a failed SimpleCalcEngine add keeps the context's variables")]
        [Category("KnownFailure")]
        [Ignore("Known failure: variables are cleared before the duplicate check (D-29)")]
        public void SimpleCalcEngineFailedAddKeepsVariables()
        {
            var engine = new SimpleCalcEngine();
            engine.Context.Variables["x"] = 1;

            Assert.Catch(() => engine.AddDynamic("a", "x +"));
            Assert.IsTrue(engine.Context.Variables.ContainsKey("x"), "after a syntax error");

            engine.AddDynamic("a", "x + 1");
            engine.Context.Variables["x"] = 2;
            Assert.Throws<InvalidOperationException>(() => engine.AddDynamic("a", "x"));
            Assert.IsTrue(engine.Context.Variables.ContainsKey("x"), "after a duplicate name");

            engine.AddDynamic("b", "x * 10");
            Assert.AreEqual(20, ((IDynamicExpression)engine["b"]!).Evaluate());
            Assert.AreEqual(0, engine.Context.Variables.Count, "a successful add still clears the variables");
        }
    }
}
