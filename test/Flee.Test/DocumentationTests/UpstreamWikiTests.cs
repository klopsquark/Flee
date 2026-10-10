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
    /// Every code example of the upstream wiki (github.com/mparlak/Flee/wiki, read 2026-10-10), run
    /// against the fork. Where the wiki's C# does not compile or is wrong, the smallest correction is
    /// made and marked "Wiki:"; doc/upstream-wiki.md lists them.
    /// </summary>
    [TestFixture]
    public class UpstreamWikiTests
    {
        // Getting-Started

        [Test]
        public void GettingStarted()
        {
            ExpressionContext context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Variables["a"] = 100;

            IDynamicExpression eDynamic = context.CompileDynamic("sqrt(a) + pi");
            IGenericExpression<double> eGeneric = context.CompileGeneric<double>("sqrt(a) + pi");

            Assert.AreEqual(10 + Math.PI, (double)eDynamic.Evaluate()!);
            Assert.AreEqual(10 + Math.PI, eGeneric.Evaluate());

            context.Variables["a"] = 144;
            Assert.AreEqual(12 + Math.PI, eGeneric.Evaluate());
        }

        [Test]
        public void GettingStarted_InvalidExpression()
        {
            // Wiki: the text calls the exception "ExpressionCompileExpression" (upstream #96).
            ExpressionContext context = new ExpressionContext();
            var ex = Assert.Throws<ExpressionCompileException>(() => context.CompileDynamic("100 +* 200"));
            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex!.Reason);
        }

        // Boolean-Expression

        [Test]
        public void BooleanExpression()
        {
            ExpressionContext context = new ExpressionContext();
            VariableCollection variables = context.Variables;
            variables.Add("a", 100);
            variables.Add("b", 1);
            variables.Add("c", 24);

            IGenericExpression<bool> e = context.CompileGeneric<bool>("(a = 100 OR b > 0) AND c <> 2");
            Assert.IsTrue(e.Evaluate());
        }

        // Calling-functions-with-a-variable-number-of-arguments

        public static class ParamsFunctions
        {
            public static int Sum(params int[] args)
            {
                int sum = 0;
                foreach (int i in args)
                {
                    sum += i;
                }
                return sum;
            }
        }

        [Test]
        public void VariableNumberOfArguments()
        {
            ExpressionContext context = new ExpressionContext();
            context.Imports.AddType(typeof(ParamsFunctions));

            IDynamicExpression e = context.CompileDynamic("sum(1,2,3,4,5,6)");
            Assert.AreEqual(21, (int)e.Evaluate()!);
        }

        // Culture-Sensitive-Expressions

        [Test]
        public void CultureSensitiveExpressions()
        {
            CultureInfo ci = new CultureInfo("fr-FR");

            ExpressionContext context = new ExpressionContext();
            // Wiki: "context.ParseCulture = ci" (no semicolon); the property is on Options.
            context.Options.ParseCulture = ci;
            context.Imports.AddType(typeof(Math));

            IDynamicExpression e = context.CompileDynamic("round(100,75; 1)");
            Assert.AreEqual(100.8, e.Evaluate());
        }

        // Customizing-Parser

        [Test]
        public void CustomizingParser()
        {
            ExpressionContext context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));

            // Wiki: CompileDynamic("...", context) has no second parameter.
            Assert.AreEqual(4.56, context.CompileDynamic("max(1.23, 4.56)").Evaluate());

            context.ParserOptions.DecimalSeparator = ',';
            context.ParserOptions.FunctionArgumentSeparator = ';';
            // Wiki: context.RecreateParser() is not public; the public method is on ParserOptions.
            context.ParserOptions.RecreateParser();

            Assert.AreEqual(4.56, context.CompileDynamic("max(1,23; 4,56)").Evaluate());
        }

        // Expression-Variables

        [Test]
        public void UsingVariables()
        {
            ExpressionContext context = new ExpressionContext();
            context.Variables["a"] = 100;
            context.Variables["b"] = 43.2;

            IDynamicExpression e = context.CompileDynamic("a + b * (a - b)");
            Assert.AreEqual(100 + 43.2 * (100 - 43.2), e.Evaluate());
        }

        [Test]
        public void VariablesActAsInstancesOfTheirType()
        {
            ExpressionContext context = new ExpressionContext();
            context.Variables["s"] = "this is a string";
            IDynamicExpression e = context.CompileDynamic("s.length + s.Remove(0, 1).length");
            Assert.AreEqual(31, (int)e.Evaluate()!);
        }

        [Test(Description = "Wiki notes: a variable keeps its type; a value of another type fails at evaluation (D-09)")]
        public void VariableKeepsItsType()
        {
            ExpressionContext context = new ExpressionContext();
            context.Variables["a"] = 1;
            IDynamicExpression e = context.CompileDynamic("a + 1");

            context.Variables["a"] = "text";
            Assert.Throws<InvalidCastException>(() => e.Evaluate());
        }

        [Test]
        public void ExpressionsAsVariables()
        {
            ExpressionContext context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Variables.Add("a", 3.14);
            IDynamicExpression e1 = context.CompileDynamic("cos(a) ^ 2");

            context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Variables.Add("a", 3.14);
            IDynamicExpression e2 = context.CompileDynamic("sin(a) ^ 2");

            context = new ExpressionContext();
            context.Variables.Add("a", e1);
            context.Variables.Add("b", e2);
            IDynamicExpression e = context.CompileDynamic("a + b");

            Assert.AreEqual(1.0, (double)e.Evaluate()!, 1e-12);
        }

        [Test]
        public void OnDemandVariables()
        {
            ExpressionContext context = new ExpressionContext();
            VariableCollection variables = context.Variables;
            int typeRequests = 0;
            int valueRequests = 0;

            // Wiki: the example reads the type and values from the console.
            variables.ResolveVariableType += (sender, e) => { typeRequests++; e.VariableType = typeof(double); };
            variables.ResolveVariableValue += (sender, e) => { valueRequests++; e.VariableValue = Convert.ChangeType("2", e.VariableType); };

            IDynamicExpression expression = context.CompileDynamic("a + (a * 0.15)");
            Assert.AreEqual(2.3, (double)expression.Evaluate()!, 1e-12);

            // "Flee raises the resolve events once for every occurrence of a variable."
            Assert.AreEqual(2, typeRequests);
            Assert.AreEqual(2, valueRequests);
        }

        // Extending

        public static class CustomFunctions
        {
            public static int Product(int a, int b)
            {
                return a * b;
            }

            public static int Sum(int a, int b)
            {
                return a + b;
            }
        }

        [Test]
        public void ImportingPublicStaticFunctions()
        {
            ExpressionContext context = new ExpressionContext();
            context.Imports.AddType(typeof(CustomFunctions));
            context.Variables.Add("a", 100);
            context.Variables.Add("b", 200);

            IDynamicExpression e = context.CompileDynamic("product(a,b) + sum(a,b)");
            Assert.AreEqual(20300, (int)e.Evaluate()!);
        }

        [Test]
        public void ImportingIntoANamespace()
        {
            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("a", 100);
            context.Variables.Add("b", 200);
            context.Imports.AddType(typeof(CustomFunctions), "functions");

            IDynamicExpression e = context.CompileDynamic("functions.product(a,b) + a - b");
            Assert.AreEqual(19900, (int)e.Evaluate()!);
        }

        [Test]
        public void ImportingBuiltInTypes()
        {
            ExpressionContext context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Variables.Add("a", 100);

            IDynamicExpression e = context.CompileDynamic("cos(a)");
            // Wiki: "int result = (int) e.Evaluate();" throws InvalidCastException: cos returns a double.
            Assert.Throws<InvalidCastException>(() => { int unused = (int)e.Evaluate()!; });
            Assert.AreEqual(Math.Cos(100), (double)e.Evaluate()!);
        }

        [Test]
        public void InstanceFunctionsOnVariables()
        {
            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("rand", new Random());

            IDynamicExpression e = context.CompileDynamic("rand.nextDouble() + 100");
            double result = (double)e.Evaluate()!;
            Assert.That(result, Is.GreaterThanOrEqualTo(100.0).And.LessThan(101.0));
        }

        [Test]
        public void FunctionsOfAnExpressionOwner()
        {
            Random rand = new Random();
            ExpressionContext context = new ExpressionContext(rand);

            IDynamicExpression e = context.CompileDynamic("nextDouble() + 100");
            double result = (double)e.Evaluate()!;
            Assert.That(result, Is.GreaterThanOrEqualTo(100.0).And.LessThan(101.0));
        }

        // Indexing-arrays-and-collections-in-expressions

        public class IndexingOwner
        {
            public int[] MyArray;
            public List<int> MyList;

            public IndexingOwner()
            {
                MyArray = new int[] { 10, 20, 30 };
                MyList = new List<int>();
                MyList.Add(100);
                MyList.Add(200);
            }
        }

        [Test]
        public void IndexingMembersOfTheOwner()
        {
            IndexingOwner owner = new IndexingOwner();
            ExpressionContext context = new ExpressionContext(owner);
            context.Variables.Add("index", 0);

            IGenericExpression<int> e = context.CompileGeneric<int>("MyArray[1+1]");
            Assert.AreEqual(30, e.Evaluate());

            e = context.CompileGeneric<int>("MyList[index + 1]");
            Assert.AreEqual(200, e.Evaluate());
        }

        [Test]
        public void IndexingArrayVariables()
        {
            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("array", new int[] { 1, 2, 3 });

            IGenericExpression<int> e = context.CompileGeneric<int>("array[1+1]");
            Assert.AreEqual(3, e.Evaluate());
        }

        // Using-an-expression-owner

        public class OwnerWithPrivateField
        {
#pragma warning disable CS0414 // Read by the expression through the owner, which the compiler cannot see
            private int MyField;
#pragma warning restore CS0414

            public int Func(int i)
            {
                return i;
            }

            public void SetFieldValue(int value)
            {
                MyField = value;
            }

            public int Property
            {
                get { return 100; }
            }

            [ExpressionOwnerMemberAccess(false)]
            public int Func1()
            {
                // Wiki: the example's Func1 has an empty body and does not compile.
                return 1;
            }
        }

        [Test]
        public void UsingAnExpressionOwner()
        {
            OwnerWithPrivateField owner = new OwnerWithPrivateField();
            ExpressionContext context = new ExpressionContext(owner);
            context.Options.OwnerMemberAccess = BindingFlags.Public | BindingFlags.NonPublic;

            IDynamicExpression e = context.CompileDynamic("func(myfield + Property) + 100");
            Assert.AreEqual(200, e.Evaluate());

            OwnerWithPrivateField owner2 = new OwnerWithPrivateField();
            owner2.SetFieldValue(16);
            e.Owner = owner2;

            Assert.AreEqual(216, e.Evaluate());
        }

        [Test]
        public void MemberAccessAttributeHidesAPublicMember()
        {
            ExpressionContext context = new ExpressionContext(new OwnerWithPrivateField());

            var ex = Assert.Throws<ExpressionCompileException>(() => context.CompileDynamic("Func1()"));
            Assert.AreEqual(CompileExceptionReason.UndefinedName, ex!.Reason);
        }

        // Using-the-Calculation-Engine

        [Test]
        public void UsingTheCalculationEngine()
        {
            CalculationEngine engine = new CalculationEngine();
            ExpressionContext context = new ExpressionContext();
            VariableCollection variables = context.Variables;

            variables.Add("x", 100);
            variables.Add("y", 200);

            engine.Add("a", "x * 2", context);
            engine.Add("b", "y + 100", context);
            engine.Add("c", "a + b", context);

            Assert.AreEqual(500, engine.GetResult<int>("c"));

            variables["x"] = 200;
            engine.Recalculate("a");

            Assert.AreEqual(700, engine.GetResult<int>("c"));
        }
    }
}
