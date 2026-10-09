// Data-driven tests over the expression script files in TestScripts, one test case per line.
//
// Rebuilt from the original VB.NET Flee test harness (BulkTests.vb and Core.vb,
// Copyright (c) 2007 Eugene Ciloci, GNU LGPL 2.1 or later), as preserved in
// https://github.com/george-playstudiosasia/PlayStudios.Flee. Contexts, imports and
// comparisons follow the original. Differences: every case gets fresh contexts instead of
// sharing them across a whole file, and InvalidExpressions also checks the expected
// CompileExceptionReason, which the original parsed but never compared.
//
// Cases listed in TestScripts/KnownFailures.txt are expected to fail. They are reported as
// ignored with the actual failure, and fail if they start passing, so the list stays exact.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.ScriptTests
{
    [TestFixture]
    // The original harness ran under en-CA as .NET Framework defined it in 2007: dates
    // dd/MM/yyyy, '.' as decimal separator. Since .NET 5 (ICU) en-CA dates are yyyy-MM-dd,
    // which breaks both the date literals (their default format follows the current culture)
    // and the expected values. en-GB matches the original assumptions on every platform.
    [SetCulture(CultureName)]
    public class ScriptTests
    {
        private const string CultureName = "en-GB";
        private static readonly CultureInfo TestCulture = CultureInfo.GetCultureInfo(CultureName);

        private TypeDescriptionProvider? _intProvider;
        private TypeDescriptionProvider? _stringProvider;

        [OneTimeSetUp]
        public void AddVirtualProperties()
        {
            // For testing virtual properties: int and string gain a property "Name".
            _intProvider = new UselessTypeDescriptionProvider(TypeDescriptor.GetProvider(typeof(int)));
            _stringProvider = new UselessTypeDescriptionProvider(TypeDescriptor.GetProvider(typeof(string)));
            TypeDescriptor.AddProvider(_intProvider, typeof(int));
            TypeDescriptor.AddProvider(_stringProvider, typeof(string));
        }

        [OneTimeTearDown]
        public void RemoveVirtualProperties()
        {
            if (_intProvider != null) TypeDescriptor.RemoveProvider(_intProvider, typeof(int));
            if (_stringProvider != null) TypeDescriptor.RemoveProvider(_stringProvider, typeof(string));
        }

        public static System.Collections.IEnumerable ValidExpressionCases() => ScriptFile.TestCases("ValidExpressions.txt");
        public static System.Collections.IEnumerable InvalidExpressionCases() => ScriptFile.TestCases("InvalidExpressions.txt");
        public static System.Collections.IEnumerable ValidCastCases() => ScriptFile.TestCases("ValidCasts.txt");
        public static System.Collections.IEnumerable CheckedCases() => ScriptFile.TestCases("CheckedTests.txt");

        [TestCaseSource(nameof(ValidExpressionCases))]
        public void ValidExpressions(ScriptLine line)
        {
            Check(line, () =>
            {
                ExpressionContext context = CreateGenericContext(new ExpressionOwner());
                context.Variables.ResolveFunction += (sender, e) => e.ReturnType = typeof(int);
                context.Variables.InvokeFunction += (sender, e) => e.Result = 100;
                return RunValidExpression(line, context);
            });
        }

        [TestCaseSource(nameof(InvalidExpressionCases))]
        public void InvalidExpressions(ScriptLine line)
        {
            Check(line, () =>
            {
                string[] arr = line.Parts;
                Type expressionType = Type.GetType(arr[0], true, true)!;
                var reason = (CompileExceptionReason)Enum.Parse(typeof(CompileExceptionReason), arr[2], true);

                ExpressionContext context = CreateGenericContext(new ExpressionOwner());
                context.Options.ResultType = expressionType;
                context.Imports.AddType(typeof(Math));
                context.Options.OwnerMemberAccess = BindingFlags.Public | BindingFlags.NonPublic;

                try
                {
                    context.CompileDynamic(arr[1]);
                }
                catch (ExpressionCompileException ex)
                {
                    return ex.Reason == reason
                        ? null
                        : $"Expected compile error {reason} but got {ex.Reason}: {ex.Message}";
                }
                return $"Expected compile error {reason}, but the expression compiled";
            });
        }

        [TestCaseSource(nameof(ValidCastCases))]
        public void ValidCasts(ScriptLine line)
        {
            Check(line, () => RunValidExpression(line, CreateValidCastsContext(new ExpressionOwner())));
        }

        [TestCaseSource(nameof(CheckedCases))]
        public void CheckedExpressions(ScriptLine line)
        {
            Check(line, () =>
            {
                string[] arr = line.Parts;
                string expression = arr[0];
                bool isChecked = bool.Parse(arr[1]);
                bool shouldOverflow = bool.Parse(arr[2]);

                ExpressionContext context = new ExpressionContext(new ExpressionOwner());
                context.Imports.AddType(typeof(Math));
                context.Imports.ImportBuiltinTypes();
                context.Options.Checked = isChecked;

                bool overflowed;
                try
                {
                    context.CompileDynamic(expression).Evaluate();
                    overflowed = false;
                }
                catch (OverflowException)
                {
                    overflowed = true;
                }

                return overflowed == shouldOverflow
                    ? null
                    : shouldOverflow ? "Expected an OverflowException, but none was thrown" : "Unexpected OverflowException";
            });
        }

        // ----- Contexts, as set up by the original harness -----

        private static ExpressionContext CreateGenericContext(object owner)
        {
            ExpressionContext context = new ExpressionContext(owner);

            context.Options.OwnerMemberAccess = BindingFlags.Public | BindingFlags.NonPublic;
            context.Imports.ImportBuiltinTypes();
            context.Imports.AddType(typeof(Math), "Math");
            context.Imports.AddType(typeof(Uri), "Uri");
            context.Imports.AddType(typeof(Mouse), "Mouse");
            context.Imports.AddType(typeof(Monitor), "Monitor");
            context.Imports.AddType(typeof(DateTime), "DateTime");
            context.Imports.AddType(typeof(Convert), "Convert");
            context.Imports.AddType(typeof(Type), "Type");
            context.Imports.AddType(typeof(DayOfWeek), "DayOfWeek");
            context.Imports.AddType(typeof(ConsoleModifiers), "ConsoleModifiers");

            NamespaceImport ns1 = new NamespaceImport("ns1");
            NamespaceImport ns2 = new NamespaceImport("ns2");
            ns2.Add(new TypeImport(typeof(Math)));
            ns1.Add(ns2);
            context.Imports.RootImport.Add(ns1);

            context.Variables.Add("varInt32", 100);
            context.Variables.Add("varDecimal", new decimal(100));
            context.Variables.Add("varString", "string");

            return context;
        }

        private static ExpressionContext CreateValidCastsContext(object owner)
        {
            ExpressionContext context = new ExpressionContext(owner);
            context.Options.OwnerMemberAccess = BindingFlags.Public | BindingFlags.NonPublic;
            context.Imports.ImportBuiltinTypes();
            context.Imports.AddType(typeof(Convert), "Convert");
            context.Imports.AddType(typeof(Guid));
            context.Imports.AddType(typeof(Version));
            context.Imports.AddType(typeof(DayOfWeek));
            context.Imports.AddType(typeof(DayOfWeek), "DayOfWeek");
            context.Imports.AddType(typeof(ValueType));
            context.Imports.AddType(typeof(IComparable));
            context.Imports.AddType(typeof(ICloneable));
            context.Imports.AddType(typeof(Array));
            context.Imports.AddType(typeof(Delegate));
            context.Imports.AddType(typeof(AppDomainInitializer));
            context.Imports.AddType(typeof(System.Text.Encoding));
            context.Imports.AddType(typeof(System.Text.ASCIIEncoding));
            context.Imports.AddType(typeof(ArgumentException));
            return context;
        }

        // ----- Running and comparing -----

        /// <summary>Format: result type (without "System."); expression; expected result.</summary>
        private static string? RunValidExpression(ScriptLine line, ExpressionContext context)
        {
            string[] arr = line.Parts;
            Type expressionType = Type.GetType("System." + arr[0], true, true)!;
            context.Options.ResultType = expressionType;

            IDynamicExpression e = context.CompileDynamic(arr[1]);
            return CompareResult(e, arr[2], expressionType);
        }

        private static string? CompareResult(IDynamicExpression e, string result, Type resultType)
        {
            if (resultType == typeof(object))
            {
                // The expected value names a type: a system type, or one of the test types.
                // System.AppDomainInitializer is missing on .NET Core; the test project's own
                // delegate of that name stands in for it (see ScriptTestTypes.cs).
                if (string.Equals(result, "System.AppDomainInitializer", StringComparison.OrdinalIgnoreCase))
                {
                    result = nameof(AppDomainInitializer);
                }
                Type? expectedType = Type.GetType(result, false, true)
                    ?? typeof(ScriptTests).Assembly.GetType($"{typeof(ScriptTests).Namespace}.{result}", true, true);

                object? actual = e.Evaluate();
                if (expectedType == typeof(void))
                {
                    return actual == null ? null : $"Expected null but got {Describe(actual)}";
                }
                return expectedType!.IsInstanceOfType(actual)
                    ? null
                    : $"Expected an instance of {expectedType} but got {Describe(actual)}";
            }

            TypeConverter tc = TypeDescriptor.GetConverter(resultType);
            object? expectedResult = RoundIfReal(tc.ConvertFromString(null, TestCulture, result));
            object? actualResult = RoundIfReal(e.Evaluate());

            // NUnit's equality, as the original Assert.AreEqual: numerics compare by value across types.
            return Is.EqualTo(expectedResult).ApplyTo(actualResult).IsSuccess
                ? null
                : $"Expected {Describe(expectedResult)} but got {Describe(actualResult)}";
        }

        private static object? RoundIfReal(object? value)
        {
            return value switch
            {
                double d => Math.Round(d, 4),
                float s => (float)Math.Round(s, 4),
                _ => value,
            };
        }

        private static string Describe(object? value) =>
            value == null ? "null" : $"{Convert.ToString(value, TestCulture)} ({value.GetType().Name})";

        /// <summary>
        /// Runs one case and reports it: pass, fail, or for a listed known failure, ignored with
        /// the actual failure. A listed case that passes fails, so the list cannot go stale.
        /// The case itself never asserts, because NUnit records a caught assertion as a failure.
        /// </summary>
        private static void Check(ScriptLine line, Func<string?> run)
        {
            KnownFailure? known = ScriptFile.FindKnownFailure(line);
            if (known != null && known.Text != line.Text)
            {
                Assert.Fail($"Stale entry in {ScriptFile.KnownFailuresFileName}: it lists \"{known.Text}\" but the line is \"{line.Text}\"");
            }
            if (known != null && known.Category == ScriptFile.CrashCategory)
            {
                // Running it would take the whole test process down.
                Assert.Ignore($"Known failure ({known.Category}): not run");
            }

            string? failure;
            try
            {
                failure = run();
            }
            catch (Exception ex)
            {
                // Name where it was thrown, so failures can be grouped by cause.
                string? origin = ex.StackTrace?.Split('\n')[0].Trim();
                failure = $"{ex.GetType().Name}: {ex.Message} ({origin})";
            }

            if (known == null)
            {
                if (failure != null)
                {
                    Assert.Fail($"{failure}\n  at {line}");
                }
                return;
            }

            if (failure == null)
            {
                Assert.Fail($"Listed in {ScriptFile.KnownFailuresFileName} ({known.Category}) but passes now; remove the entry");
            }
            Assert.Ignore($"Known failure ({known.Category}): {failure}");
        }
    }
}
