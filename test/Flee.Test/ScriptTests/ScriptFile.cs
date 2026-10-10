using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Flee.Test.ScriptTests
{
    /// <summary>
    /// One test case from a script file: a non-comment line, split at ';'.
    /// </summary>
    public sealed class ScriptLine
    {
        public ScriptLine(string fileName, int lineNumber, string text)
        {
            FileName = fileName;
            LineNumber = lineNumber;
            Text = text;
            Parts = text.Split(ScriptFile.SeparatorChar);
        }

        public string FileName { get; }
        public int LineNumber { get; }
        public string Text { get; }
        public string[] Parts { get; }

        public override string ToString() => $"{FileName}:{LineNumber}: {Text}";
    }

    /// <summary>
    /// A case listed in KnownFailures.txt: expected to fail today, recorded instead of fixed.
    /// </summary>
    public sealed class KnownFailure
    {
        public KnownFailure(string category, string text)
        {
            Category = category;
            Text = text;
        }

        public string Category { get; }

        /// <summary>The script line as it stands in the script file, to catch stale entries.</summary>
        public string Text { get; }
    }

    /// <summary>
    /// Reads the script files in TestScripts (copied next to the test assembly) and turns each
    /// line into an NUnit test case. Format and comment rules are those of the original
    /// Flee test harness: lines starting with ' are comments, fields are separated by ';'.
    /// </summary>
    public static class ScriptFile
    {
        public const char CommentChar = '\'';
        public const char SeparatorChar = ';';
        public const string KnownFailuresFileName = "KnownFailures.txt";

        /// <summary>Known failures in this category crash the process and are never run.</summary>
        public const string CrashCategory = "crash";

        private static readonly Lazy<Dictionary<(string, int), KnownFailure>> s_knownFailures =
            new Lazy<Dictionary<(string, int), KnownFailure>>(LoadKnownFailures);

        public static string ScriptDirectory =>
            Path.Combine(Path.GetDirectoryName(typeof(ScriptFile).Assembly.Location)!, "TestScripts");

        public static IEnumerable<ScriptLine> ReadLines(string fileName)
        {
            string[] lines = File.ReadAllLines(Path.Combine(ScriptDirectory, fileName));
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith(CommentChar) || line.Trim().Length == 0)
                {
                    continue;
                }
                yield return new ScriptLine(fileName, i + 1, line);
            }
        }

        /// <summary>
        /// One test case per script line, shown as "File:Line" plus the line itself.
        /// </summary>
        public static IEnumerable<TestCaseData> TestCases(string fileName)
        {
            string shortName = Path.GetFileNameWithoutExtension(fileName);
            return ReadLines(fileName).Select(line =>
            {
                TestCaseData data = new TestCaseData(line).SetArgDisplayNames($"{shortName}:{line.LineNumber}", line.Text);
                KnownFailure? known = FindKnownFailure(line);
                if (known != null)
                {
                    data.SetCategory("KnownFailure");
                }
                return data;
            });
        }

        /// <summary>Failures in this category come from a Debug.Assert, so only Debug builds have them.</summary>
        public const string DebugOnlyCategory = "debug-il-length";

        public static KnownFailure? FindKnownFailure(ScriptLine line)
        {
            s_knownFailures.Value.TryGetValue((line.FileName, line.LineNumber), out KnownFailure? known);
#if !DEBUG
            if (known?.Category == DebugOnlyCategory)
            {
                return null;
            }
#endif
            return known;
        }

        // Format: File;Line;Category;Script line (the script line may itself contain ';').
        private static Dictionary<(string, int), KnownFailure> LoadKnownFailures()
        {
            var result = new Dictionary<(string, int), KnownFailure>();
            string path = Path.Combine(ScriptDirectory, KnownFailuresFileName);
            if (!File.Exists(path))
            {
                return result;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                if (line.StartsWith(CommentChar) || line.Trim().Length == 0)
                {
                    continue;
                }
                string[] parts = line.Split(SeparatorChar, 4);
                if (parts.Length != 4)
                {
                    throw new FormatException($"{KnownFailuresFileName}: expected File;Line;Category;Script line, got: {line}");
                }
                result.Add((parts[0], int.Parse(parts[1])), new KnownFailure(parts[2], parts[3]));
            }
            return result;
        }
    }
}
