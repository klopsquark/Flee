#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Flee.Test.DocumentationTests
{
    /// <summary>
    /// Checks that every code block in doc/api-guide.md marked "&lt;!-- example: Name --&gt;" is the
    /// same code as the region "doc:Name" in ApiGuideExamples.cs, which runs with assertions.
    /// </summary>
    [TestFixture]
    public class ApiGuideSyncTests
    {
        private static string OutputPath(params string[] parts) =>
            Path.Combine(new[] { Path.GetDirectoryName(typeof(ApiGuideSyncTests).Assembly.Location)! }.Concat(parts).ToArray());

        private static Dictionary<string, string> GuideBlocks()
        {
            string guide = File.ReadAllText(OutputPath("Docs", "api-guide.md")).Replace("\r\n", "\n");
            return Regex.Matches(guide, @"<!-- example: (\w+) -->\n```csharp\n(.*?)\n```", RegexOptions.Singleline)
                .Cast<Match>()
                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        }

        private static Dictionary<string, string> SourceRegions()
        {
            string source = File.ReadAllText(OutputPath("Docs", "ApiGuideExamples.cs")).Replace("\r\n", "\n");
            var result = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(source, @"#region doc:(\w+)\n(.*?)\n[ \t]*#endregion", RegexOptions.Singleline))
            {
                string[] lines = m.Groups[2].Value.Split('\n');
                int indent = lines.Where(l => l.Trim().Length > 0).Min(l => l.Length - l.TrimStart().Length);
                result[m.Groups[1].Value] = string.Join("\n", lines.Select(l => l.Length >= indent ? l.Substring(indent) : l.TrimStart()));
            }
            return result;
        }

        [Test]
        public void EveryGuideExampleMatchesItsTestedRegion()
        {
            Dictionary<string, string> blocks = GuideBlocks();
            Dictionary<string, string> regions = SourceRegions();

            Assert.IsNotEmpty(blocks, "no examples found in the guide");
            CollectionAssert.AreEquivalent(regions.Keys, blocks.Keys, "examples in the guide and regions in ApiGuideExamples.cs differ");
            foreach (var pair in blocks)
            {
                Assert.AreEqual(regions[pair.Key], pair.Value, $"example '{pair.Key}' differs from its region in ApiGuideExamples.cs");
            }
        }
    }
}
