#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Flee.Test.DocumentationTests
{
    /// <summary>
    /// Keeps the hand-written wiki pages (wiki/*.md) correct: every C# block is a tested example,
    /// identical to its "doc:Name" region in WikiExamples.cs or ApiGuideExamples.cs, every region
    /// of WikiExamples.cs is used, and every link to another page names a page that exists.
    /// </summary>
    [TestFixture]
    public class WikiSyncTests
    {
        private static string OutputPath(params string[] parts) =>
            Path.Combine(new[] { Path.GetDirectoryName(typeof(WikiSyncTests).Assembly.Location)! }.Concat(parts).ToArray());

        private static Dictionary<string, string> Pages() =>
            Directory.GetFiles(OutputPath("Docs", "wiki"), "*.md")
                .ToDictionary(p => Path.GetFileNameWithoutExtension(p), p => File.ReadAllText(p).Replace("\r\n", "\n"));

        private static Dictionary<string, string> Regions(string file)
        {
            string source = File.ReadAllText(OutputPath("Docs", file)).Replace("\r\n", "\n");
            var result = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(source, @"#region doc:(\w+)\n(.*?)\n[ \t]*#endregion", RegexOptions.Singleline))
            {
                string[] lines = m.Groups[2].Value.Split('\n');
                int indent = lines.Where(l => l.Trim().Length > 0).Min(l => l.Length - l.TrimStart().Length);
                result[m.Groups[1].Value] = string.Join("\n", lines.Select(l => l.Length >= indent ? l.Substring(indent) : l.TrimStart()));
            }
            return result;
        }

        private static IEnumerable<(string Page, string Name, string Code)> MarkedBlocks() =>
            from page in Pages()
            from Match m in Regex.Matches(page.Value, @"<!-- example: (\w+) -->\n```csharp\n(.*?)\n```", RegexOptions.Singleline)
            select (page.Key, m.Groups[1].Value, m.Groups[2].Value);

        [Test]
        public void EveryCSharpBlockIsATestedExample()
        {
            foreach (var page in Pages())
            {
                int all = Regex.Matches(page.Value, @"^```csharp$", RegexOptions.Multiline).Count;
                int marked = Regex.Matches(page.Value, @"<!-- example: \w+ -->\n```csharp\n").Count;
                Assert.AreEqual(all, marked, $"{page.Key}.md has C# blocks without an <!-- example: Name --> marker");
            }
        }

        [Test]
        public void EveryExampleMatchesItsTestedRegion()
        {
            Dictionary<string, string> regions = Regions("WikiExamples.cs");
            foreach (var pair in Regions("ApiGuideExamples.cs"))
            {
                Assert.IsFalse(regions.ContainsKey(pair.Key), $"region '{pair.Key}' exists in both example files");
                regions[pair.Key] = pair.Value;
            }

            var blocks = MarkedBlocks().ToList();
            Assert.IsNotEmpty(blocks, "no examples found in the wiki");
            foreach (var block in blocks)
            {
                Assert.IsTrue(regions.ContainsKey(block.Name), $"{block.Page}.md: no region 'doc:{block.Name}'");
                Assert.AreEqual(regions[block.Name], block.Code, $"{block.Page}.md: example '{block.Name}' differs from its region");
            }
        }

        [Test]
        public void EveryWikiRegionIsUsed()
        {
            var used = new HashSet<string>(MarkedBlocks().Select(b => b.Name));
            foreach (string name in Regions("WikiExamples.cs").Keys)
            {
                Assert.IsTrue(used.Contains(name), $"region 'doc:{name}' in WikiExamples.cs is not used on any wiki page");
            }
        }

        [Test]
        public void EveryPageLinkResolves()
        {
            var pages = new HashSet<string>(Pages().Keys);
            foreach (string line in File.ReadAllLines(OutputPath("Docs", "wiki", "generated-pages.txt")))
            {
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#")) continue;
                pages.Add(line.Split('=')[1].Trim());
            }

            foreach (var page in Pages())
            {
                foreach (Match m in Regex.Matches(page.Value, @"\]\(([^)\s]+)\)"))
                {
                    string target = m.Groups[1].Value;
                    if (target.StartsWith("http") || target.StartsWith("#")) continue;
                    string name = target.Split('#')[0];
                    Assert.IsTrue(pages.Contains(name), $"{page.Key}.md links to '{target}', which is not a wiki page");
                }
            }
        }
    }
}
