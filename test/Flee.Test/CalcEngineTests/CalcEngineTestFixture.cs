using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;
using NUnit.Framework;
using System.Collections.Generic;

namespace Flee.Test.CalcEngineTests
{
    [TestFixture]
    public class CalcEngineTestFixture
    {
        [Test]
        public void Test_Basic()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 1);
            ce.Add("b", "a + y", context);
            ce.Add("c", "b * 2", context);
            ce.Recalculate("a");

            var result = ce.GetResult<int>("c");
            Assert.AreEqual(result, ((100 * 2) + 1) * 2);
            variables.Remove("x");
            variables.Add("x", 345);
            ce.Recalculate("a");
            result = ce.GetResult<int>("c");

            Assert.AreEqual(((345 * 2) + 1) * 2, result);
        }

        [Test]
        public void Test_MutipleIdentical_References()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            ce.Add("b", "a + a + a", context);
            ce.Recalculate("a");
            var result = ce.GetResult<int>("b");
            Assert.AreEqual((100 * 2) * 3, result);
        }

        [Test]
        public void Test_Complex()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 24);
            ce.Add("b", "y * 2", context);
            ce.Add("c", "a + b", context);
            ce.Add("d", "80", context);
            ce.Add("e", "a + b + c + d", context);
            ce.Recalculate("d");
            ce.Recalculate("a", "b");

            var result = ce.GetResult<int>("e");
            Assert.AreEqual((100 * 2) + (24 * 2) + ((100 * 2) + (24 * 2)) + 80, result);
        }

        [Test]
        public void Test_Arithmetic()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("a", 10);
            variables.Add("b", 20);
            ce.Add("x", "((a * 2) + (b ^ 2)) - (100 % 5)", context);
            ce.Recalculate("x");
            var result = ce.GetResult<int>("x");
            Assert.AreEqual(420, result);
        }

        [Test]
        public void Test_Comparison_Operators()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("a", 10);
            ce.Add("x", "a <> 100", context);
            ce.Recalculate("x");
            var result = ce.GetResult<bool>("x");
            Assert.IsTrue(result);
        }

        [Test]
        public void Test_And_Or_Xor_Not_Operators()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("a", 10);
            ce.Add("x", "a > 100", context);
            ce.Recalculate("x");
            var result = ce.GetResult<bool>("x");
            Assert.IsFalse(result);
            ce.Remove("x");
            variables.Add("b", 100);
            ce.Add("x", "b = 100", context);
            ce.Recalculate("x");
            result = ce.GetResult<bool>("x");
            Assert.IsTrue(result);
        }

        [Test]
        public void Test_Shift_Operators()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            ce.Add("x", "100 >> 2", context);
            ce.Recalculate("x");
            var result = ce.GetResult<int>("x");
            Assert.AreEqual(25, result);
        }

        [Test]
        public void Test_Recalculate_NonSource()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 1);
            ce.Add("b", "a + y", context);
            ce.Add("c", "b * 2", context);
            ce.Recalculate("a", "b");
            var result = ce.GetResult<int>("c");
            Assert.AreEqual(((100) * 2 + 1) * 2, result);
        }

        [Test]
        public void Test_Partial_Recalculate()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 1);
            ce.Add("b", "a + y", context);
            ce.Add("c", "b * 2", context);
            ce.Recalculate("a");
            variables["y"] = 222;
            ce.Recalculate("b");
            var result = ce.GetResult<int>("c");
            Assert.AreEqual(((100 * 2) + 222) * 2, result);
        }

        [Test]
        public void Test_Circular_Reference1()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 1);
            ce.Add("b", "a + y + b", context);
            Assert.Throws<CircularReferenceException>(() => { ce.Recalculate("a"); });
        }

        [Test]
        public void Test_Boolean_Expression()
        {
            string expression = "a AND NOT b AND NOT c AND d";
            Dictionary<string, object> expressionVariables = new Dictionary<string, object>();
            expressionVariables.Add("a", 1);
            expressionVariables.Add("b", 0);
            expressionVariables.Add("c", 0);
            expressionVariables.Add("d", 1);

            var context = new ExpressionContext();
            var vars = context.Variables;
            foreach (var expressionVariable in expressionVariables.Keys)
                vars.Add(expressionVariable, expressionVariables[expressionVariable]);
            IDynamicExpression dynamicExpression = context.CompileDynamic(expression);
            foreach (var expressionVariable in expressionVariables.Keys)
                vars[expressionVariable] = expressionVariables[expressionVariable];
            var a = dynamicExpression.Evaluate();

            //ExpressionContext context = new ExpressionContext();
            //VariableCollection variables = context.Variables;
            ////variables.Add("a", 1);
            ////variables.Add("b", 0);
            ////IGenericExpression<bool> e = context.CompileGeneric<bool>("a=1 OR b=0");

            //IGenericExpression<bool> e = context.CompileGeneric<bool>("false OR false");

            //bool result = e.Evaluate();
            //Assert.AreEqual(false, result);
        }

        // ----- Ported from the original CalcEngineTestFixture.vb -----
        //
        // The tests below were missing from this fixture. They are C# ports of the original
        // VB.NET Flee test project (Copyright (c) 2007 Eugene Ciloci, GNU LGPL 2.1 or later),
        // as preserved in https://github.com/george-playstudiosasia/PlayStudios.Flee
        // (Tests/CalcEngineTests/CalcEngineTestFixture.vb), with the original names and
        // expectations. [ExpectedException], which NUnit 3 no longer has, becomes
        // Assert.Throws around the whole test body.

        [Test]
        public void TestCircularReference2()
        {
            Assert.Throws<CircularReferenceException>(() =>
            {
                var ce = new CalculationEngine();
                var context = new ExpressionContext();
                var variables = context.Variables;

                variables.Add("x", 100);
                ce.Add("a", "x * 2", context);

                variables.Add("y", 1);
                ce.Add("b", "a + y + b", context);

                ce.Recalculate("b");
            });
        }

        [Test]
        public void TestWithReferenceTypes()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", "string");
            ce.Add("a", "x + \" \"", context);

            variables.Add("y", "word");
            ce.Add("b", "y", context);

            ce.Add("c", "a + b", context);

            ce.Recalculate("b", "a");

            string result = ce.GetResult<string>("c");
            Assert.AreEqual("string" + " " + "word", result);
        }

        [Test]
        public void TestRemove1()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            ce.Add("a", "100", context);
            ce.Add("b", "200", context);
            ce.Add("c", "a + b", context);
            ce.Add("d", "300", context);
            ce.Add("e", "c + d", context);

            ce.Remove("a");
            // Only b and d should remain
            Assert.AreEqual(2, ce.Count);

            ce.Remove("b");
            Assert.AreEqual(1, ce.Count);

            ce.Remove("d");
            Assert.AreEqual(0, ce.Count);

            // b and d should have no dependents or precedents
            Assert.IsFalse(ce.HasDependents("b"));
            Assert.IsFalse(ce.HasDependents("d"));
            Assert.IsFalse(ce.HasPrecedents("b"));
            Assert.IsFalse(ce.HasPrecedents("d"));
        }

        [Test]
        public void TestRemove2()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            ce.Add("a", "100", context);
            ce.Add("b", "200", context);
            ce.Add("c", "a + b", context);
            ce.Add("d", "300", context);
            ce.Add("e", "c + d + a", context);

            ce.Remove("a");
            // Only b and d should remain
            Assert.AreEqual(2, ce.Count);
            ce.Remove("b");
            Assert.AreEqual(1, ce.Count);
            ce.Remove("d");
            Assert.AreEqual(0, ce.Count);
        }

        [Test]
        public void TestRemove3()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            ce.Add("a", "100", context);
            ce.Add("b", "200", context);
            ce.Add("c", "a + b", context);
            ce.Add("d", "300 + c", context);
            ce.Add("e", "c + d", context);

            ce.Remove("d");
            Assert.AreEqual(3, ce.Count);

            ce.Recalculate("c");
            ce.Remove("c");
            Assert.AreEqual(2, ce.Count);

            ce.Remove("a");
            Assert.AreEqual(1, ce.Count);

            ce.Remove("b");
            Assert.AreEqual(0, ce.Count);
        }

        [Test]
        public void TestRemove4()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            ce.Add("a", "100", context);
            ce.Add("b", "200", context);
            ce.Add("c", "a + b", context);
            ce.Add("d", "300 + c", context);
            ce.Add("e", "c + d", context);

            ce.Remove("a");
            Assert.AreEqual(1, ce.Count);

            ce.Remove("b");
            Assert.AreEqual(0, ce.Count);
        }

        [Test]
        public void TestBatchLoad()
        {
            // Test that we can add expressions in any order
            var engine = new CalculationEngine();
            var context = new ExpressionContext();

            int interest = 2;
            context.Variables.Add("interest", interest);

            BatchLoader loader = engine.CreateBatchLoader();

            loader.Add("c", "a + b", context);
            loader.Add("a", "100 + interest", context);
            loader.Add("b", "a + 1 + a", context);
            // Test an expression with a reference in a string
            loader.Add("d", "\"str \\\" str\" + a + \"b\"", context);

            engine.BatchLoad(loader);

            int result = engine.GetResult<int>("b");
            Assert.AreEqual((100 + interest) + 1 + (100 + interest), result);

            interest = 300;
            context.Variables["interest"] = interest;
            engine.Recalculate("a");

            result = engine.GetResult<int>("b");
            Assert.AreEqual((100 + interest) + 1 + (100 + interest), result);

            result = engine.GetResult<int>("c");
            Assert.AreEqual((100 + interest) + 1 + (100 + interest) + (100 + interest), result);

            Assert.AreEqual("str \" str400b", engine.GetResult<string>("d"));
        }

        [Test]
        public void TestCalcEngineAtom()
        {
            // Test that calc engine atom reference work properly
            var engine = new CalculationEngine();
            var context = new ExpressionContext();

            engine.Add("a", "\"abc\"", context);
            engine.Add("b", "a.length", context);
            engine.Add("c", "a.startswith(\"a\")", context);

            int result = engine.GetResult<int>("b");
            Assert.AreEqual("abc".Length, result);

            Assert.AreEqual(true, engine.GetResult<bool>("c"));
        }

        [Test]
        public void TestDependencyManagement()
        {
            // Test that we are keeping accurate stats on our dependencies

            var engine = new CalculationEngine();
            var context = new ExpressionContext();

            engine.Add("a", "100", context);
            engine.Add("b", "100", context);

            // Nothing should point to a and b
            Assert.IsFalse(engine.HasPrecedents("a"));
            Assert.IsFalse(engine.HasPrecedents("b"));
            Assert.IsFalse(engine.HasDependents("a"));
            Assert.IsFalse(engine.HasDependents("b"));

            engine.Add("c", "a + b", context);
            engine.Add("d", "a + c", context);

            // a and b still have nothing pointing to them
            Assert.IsFalse(engine.HasPrecedents("a"));
            Assert.IsFalse(engine.HasPrecedents("b"));
            // but they have dependents
            Assert.IsTrue(engine.HasDependents("a"));
            Assert.IsTrue(engine.HasDependents("b"));

            // c and d have precedents
            Assert.IsTrue(engine.HasPrecedents("d"));
            Assert.IsTrue(engine.HasPrecedents("c"));
            // and only c should have dependents
            Assert.IsTrue(engine.HasDependents("c"));
            Assert.IsFalse(engine.HasDependents("d"));

            // test our counts
            Assert.AreEqual(2, engine.GetDependents("a").Length);
            Assert.AreEqual(1, engine.GetDependents("b").Length);
            Assert.AreEqual(1, engine.GetDependents("c").Length);
            Assert.AreEqual(0, engine.GetDependents("d").Length);

            Assert.AreEqual(0, engine.GetPrecedents("a").Length);
            Assert.AreEqual(0, engine.GetPrecedents("b").Length);
            Assert.AreEqual(2, engine.GetPrecedents("c").Length);
            Assert.AreEqual(2, engine.GetPrecedents("d").Length);

            engine.Remove("d");

            Assert.IsFalse(engine.HasDependents("c"));
            Assert.IsFalse(engine.HasDependents("d"));
            Assert.IsFalse(engine.HasPrecedents("d"));
            Assert.IsTrue(engine.HasPrecedents("c"));

            Assert.AreEqual(1, engine.GetDependents("a").Length);
            Assert.AreEqual(1, engine.GetDependents("b").Length);
            Assert.AreEqual(0, engine.GetDependents("c").Length);

            engine.Remove("c");

            Assert.IsFalse(engine.HasPrecedents("c"));
            Assert.IsFalse(engine.HasDependents("c"));
            Assert.IsFalse(engine.HasDependents("a"));
            Assert.IsFalse(engine.HasDependents("b"));

            Assert.AreEqual(0, engine.GetDependents("a").Length);
            Assert.AreEqual(0, engine.GetDependents("b").Length);

            engine.Remove("a");
            engine.Remove("b");

            Assert.IsFalse(engine.HasDependents("a"));
            Assert.IsFalse(engine.HasPrecedents("a"));
            Assert.IsFalse(engine.HasDependents("b"));
            Assert.IsFalse(engine.HasPrecedents("b"));

            Assert.AreEqual(0, engine.GetDependents("a").Length);
            Assert.AreEqual(0, engine.GetDependents("b").Length);
            Assert.AreEqual(0, engine.GetPrecedents("a").Length);
            Assert.AreEqual(0, engine.GetPrecedents("b").Length);
        }

        [Test]
        public void TestInfoMethodsWithMissing()
        {
            // Test that our informational methods can be called with a non-existant expression
            var engine = new CalculationEngine();

            Assert.IsFalse(engine.HasDependents("zz"));
            Assert.IsFalse(engine.HasPrecedents("zz"));
            Assert.AreEqual(0, engine.GetDependents("zz").Length);
            Assert.AreEqual(0, engine.GetPrecedents("zz").Length);
        }

        [Test]
        public void TestClear()
        {
            var engine = new CalculationEngine();
            var context = new ExpressionContext();

            engine.Add("a", "100", context);
            engine.Add("b", "a + 2", context);

            engine.Clear();

            Assert.IsFalse(engine.HasDependents("a"));
            Assert.IsFalse(engine.HasPrecedents("b"));
            Assert.AreEqual(0, engine.Count);
        }
    }
}