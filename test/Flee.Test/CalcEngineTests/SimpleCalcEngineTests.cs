// Script-driven tests of SimpleCalcEngine over TestScripts/SimpleCalcEngineTests.txt.
//
// Ported to C# from SimpleCalcEngineTests.vb (and the helpers ParseQueryString and Parse
// from Core.vb) of the original VB.NET Flee test project (Copyright (c) 2007 Eugene Ciloci,
// GNU LGPL 2.1 or later), as preserved in
// https://github.com/george-playstudiosasia/PlayStudios.Flee
// (Tests/CalcEngineTests/SimpleCalcEngineTests.vb). Differences: the original ran the whole
// file as one test with one engine; here each script line is its own test case with a fresh
// engine, as in ScriptTests, so one failing line cannot hide or disturb the others.
//
// Line format: ([result name]=[expected value])+;([name]:[expression text]?[variables])+

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;
using Flee.Test.ScriptTests;
using NUnit.Framework;

namespace Flee.Test.CalcEngineTests
{
    [TestFixture]
    public class SimpleCalcEngineTests
    {
        private static readonly CultureInfo TestCulture = CultureInfo.GetCultureInfo(Flee.Test.TestCulture.Name);

        private SimpleCalcEngine _myEngine;

        [SetUp]
        public void CreateEngine()
        {
            var engine = new SimpleCalcEngine();
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Imports.AddType(typeof(Math), "math");
            engine.Context = context;
            _myEngine = engine;
        }

        public static System.Collections.IEnumerable ScriptCases() => ScriptFile.TestCases("SimpleCalcEngineTests.txt");

        [TestCaseSource(nameof(ScriptCases))]
        public void TestScripts(ScriptLine line)
        {
            this.SimpleCalcEngineTestsProcessor(line.Parts);
        }

        private void SimpleCalcEngineTestsProcessor(string[] lineParts)
        {
            string[] expressions = lineParts[1].Split('|');

            _myEngine.Clear();

            this.AddExpressions(expressions);

            this.Evaluate(lineParts[0]);
        }

        private void AddExpressions(string[] expressions)
        {
            foreach (string expression in expressions)
            {
                string[] arr = expression.Split(':');

                string name = arr[0];

                string[] arr2 = arr[1].Split('?');
                string text = arr2[0];

                if (arr2.Length > 1)
                {
                    this.AddVariables(arr2[1]);
                }

                _myEngine.AddDynamic(name, text);
            }
        }

        private void Evaluate(string data)
        {
            IDictionary<string, object> results = ParseQueryString(data);

            foreach (KeyValuePair<string, object> entry in results)
            {
                // The engine's indexer returns IExpression; VB converted it implicitly.
                IDynamicExpression e = (IDynamicExpression)_myEngine[entry.Key];
                object expectedResult = entry.Value;
                object result = e.Evaluate();
                Assert.AreEqual(expectedResult, result);
            }
        }

        private void AddVariables(string variablesText)
        {
            IDictionary<string, object> variables = ParseQueryString(variablesText);

            foreach (KeyValuePair<string, object> entry in variables)
            {
                _myEngine.Context.Variables.Add(entry.Key, entry.Value);
            }
        }

        // ----- Helpers, from Core.vb -----

        // The original parsed with en-CA as .NET Framework defined it; the test culture
        // (TestCulture.cs) stands in for it. The original passed the Integer variable to
        // Double.TryParse and returned a Double that was never assigned, so a real number came
        // back as 0.0; the port returns the parsed number. The script has no real numbers.
        private static object Parse(string s)
        {
            if (bool.TryParse(s, out bool b))
            {
                return b;
            }

            if (int.TryParse(s, NumberStyles.Integer, TestCulture, out int i))
            {
                return i;
            }

            if (double.TryParse(s, NumberStyles.Float, TestCulture, out double d))
            {
                return d;
            }

            if (DateTime.TryParse(s, TestCulture, DateTimeStyles.None, out DateTime dt))
            {
                return dt;
            }

            return s;
        }

        private static IDictionary<string, object> ParseQueryString(string s)
        {
            string[] arr = s.Split('&');
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (string part in arr)
            {
                string[] arr2 = part.Split('=');
                dict.Add(arr2[0], Parse(arr2[1]));
            }

            return dict;
        }
    }
}
