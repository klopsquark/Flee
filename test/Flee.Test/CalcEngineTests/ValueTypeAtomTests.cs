#nullable enable

using System;
using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.CalcEngineTests
{
    /// <summary>
    /// Member access on a calculation-engine atom whose result is a value type
    /// (upstream issues #64 and #111).
    /// </summary>
    [TestFixture]
    public class ValueTypeAtomTests
    {
        [Test(Description = "Upstream #64: calling a method on a DateTime atom")]
        public void MethodCallOnDateTimeAtom()
        {
            var engine = new CalculationEngine();
            var context = new ExpressionContext();
            context.Variables["now"] = new DateTime(2026, 10, 10, 12, 0, 0);

            engine.Add("d", "now", context);
            engine.Add("res", "d.AddDays(1)", context);

            Assert.AreEqual(new DateTime(2026, 10, 11, 12, 0, 0), engine.GetResult<DateTime>("res"));
        }

        [Test(Description = "Upstream #111: reading a property of a TimeSpan atom")]
        public void PropertyOfTimeSpanAtom()
        {
            var engine = new CalculationEngine();
            var context = new ExpressionContext();
            context.Variables["start"] = new DateTime(2026, 10, 10, 8, 0, 0);
            context.Variables["end"] = new DateTime(2026, 10, 10, 11, 30, 0);

            engine.Add("Duration", "end - start", context);
            engine.Add("Hours", "Duration.TotalHours", context);

            Assert.AreEqual(3.5, engine.GetResult<double>("Hours"));
        }
    }
}
