#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.DocumentationTests
{
    /// <summary>
    /// Runs every example of doc/language-reference.md: each row of a table headed
    /// "| Expression | Result | Type |" is compiled in the context the document describes and
    /// its result compared with the row. Keeps the reference from going stale.
    /// </summary>
    [TestFixture]
    public class LanguageReferenceTests
    {
        private const string DocumentName = "language-reference.md";

        public sealed class Example
        {
            public Example(int line, string expression, string result, string type)
            {
                Line = line;
                Expression = expression;
                Result = result;
                Type = type;
            }

            public int Line { get; }
            public string Expression { get; }
            public string Result { get; }
            public string Type { get; }
            public override string ToString() => $"line {Line}: {Expression}";
        }

        public static IEnumerable<TestCaseData> Examples()
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(LanguageReferenceTests).Assembly.Location)!, "Docs", DocumentName);
            string[] lines = File.ReadAllLines(path);
            bool inTable = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("| Expression | Result | Type |"))
                {
                    inTable = true;
                    i++; // separator row
                    continue;
                }
                if (!inTable)
                {
                    continue;
                }
                if (!line.StartsWith("|"))
                {
                    inTable = false;
                    continue;
                }
                string[] cells = SplitRow(line);
                string expression = Unquote(cells[0]);
                yield return new TestCaseData(new Example(i + 1, expression, cells[1], cells[2]))
                    .SetArgDisplayNames($"L{i + 1}", expression);
            }
        }

        [TestCaseSource(nameof(Examples))]
        public void Example_GivesDocumentedResult(Example example)
        {
            ExpressionContext context = CreateContext();

            if (example.Result.StartsWith("error: "))
            {
                var reason = (CompileExceptionReason)Enum.Parse(typeof(CompileExceptionReason), example.Result.Substring(7));
                var ex = Assert.Throws<ExpressionCompileException>(() => context.CompileDynamic(example.Expression));
                Assert.AreEqual(reason, ex!.Reason, ex.Message);
                return;
            }

            IDynamicExpression e = context.CompileDynamic(example.Expression);

            if (example.Result.StartsWith("throws: "))
            {
                Exception? thrown = null;
                try
                {
                    e.Evaluate();
                }
                catch (Exception ex)
                {
                    thrown = ex;
                }
                Assert.IsNotNull(thrown, "expected an exception");
                Assert.AreEqual(example.Result.Substring(8), thrown!.GetType().Name);
                return;
            }

            object? result = e.Evaluate();
            Assert.AreEqual(example.Result, Format(result), "result");
            Assert.AreEqual(example.Type, result?.GetType().Name ?? "", "type");
        }

        /// <summary>The context described at the top of doc/language-reference.md.</summary>
        private static ExpressionContext CreateContext()
        {
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Imports.AddType(typeof(Math), "math");
            context.Imports.ImportBuiltinTypes();
            context.Variables["a"] = 3;
            context.Variables["b"] = 4.5;
            context.Variables["s"] = "hello";
            context.Variables["d"] = new DateTime(2026, 10, 10);
            context.Variables["list"] = new List<int> { 1, 2, 3 };
            return context;
        }

        private static string Format(object? value) => value switch
        {
            null => "null",
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "",
        };

        // Markdown table cells, honouring \| inside a cell.
        private static string[] SplitRow(string line)
        {
            var cells = new List<string>();
            var current = new System.Text.StringBuilder();
            for (int i = 1; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\\' && i + 1 < line.Length && line[i + 1] == '|')
                {
                    current.Append('|');
                    i++;
                }
                else if (c == '|')
                {
                    cells.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            return cells.ToArray();
        }

        private static string Unquote(string cell) =>
            cell.Length >= 2 && cell[0] == '`' && cell[cell.Length - 1] == '`' ? cell.Substring(1, cell.Length - 2) : cell;
    }
}
