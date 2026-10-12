#nullable enable
using Parlot;
using Parlot.Fluent;

namespace Flee.Syntax
{
    /// <summary>
    /// Parse state for one expression: Parlot's context plus the furthest position a terminal failed
    /// at and what it expected there, for the syntax error message.
    /// </summary>
    internal sealed class FleeParseContext : ParseContext
    {
        private readonly List<string> _expected = new List<string>();

        /// <param name="text">The expression.</param>
        /// <param name="diagnose">Collect what was expected at the error position. Only the second
        /// parse of an invalid expression does this, so a valid one pays nothing for messages.</param>
        public FleeParseContext(string text, bool diagnose) : base(new Scanner(text))
        {
            Diagnose = diagnose;
        }

        public bool Diagnose { get; }

        public int FurthestOffset { get; private set; } = -1;

        public IReadOnlyList<string> Expected => _expected;

        public void Fail(int offset, string expected)
        {
            if (offset > FurthestOffset)
            {
                FurthestOffset = offset;
                _expected.Clear();
            }

            if (Diagnose && offset == FurthestOffset && !_expected.Contains(expected))
            {
                _expected.Add(expected);
            }
        }
    }

    /// <summary>
    /// Base of Flee's tokens. Each terminal skips the whitespace Flee allows (ASCII only, as the
    /// old tokenizer's <c>\s</c>), reads its token, and on failure restores the position and records
    /// what it expected.
    /// </summary>
    internal abstract class Terminal<T> : Parser<T>
    {
        protected Terminal(string expected)
        {
            ExpectedName = expected;
        }

        public string ExpectedName { get; }

        public sealed override bool Parse(ParseContext context, ref ParseResult<T> result)
        {
            Cursor cursor = context.Scanner.Cursor;
            TextPosition before = cursor.Position;
            string buffer = cursor.Buffer;
            int start = cursor.Offset;

            while (start < buffer.Length && IsWhiteSpace(buffer[start]))
            {
                start++;
            }

            int length = Read(buffer, start, out T value);

            if (length > 0 || (length == 0 && AllowsEmpty))
            {
                cursor.Advance(start - cursor.Offset + length);
                result.Set(start, start + length, value);
                return true;
            }

            ((FleeParseContext)context).Fail(start, ExpectedName);
            cursor.ResetPosition(before);
            return false;
        }

        /// <summary>True for a terminal that matches without consuming input (the end of input).</summary>
        protected virtual bool AllowsEmpty => false;

        /// <summary>Reads the token at <paramref name="start"/>; returns its length, 0 or less if there is none.</summary>
        protected abstract int Read(string text, int start, out T value);

        public static bool IsWhiteSpace(char c) => c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\f' || c == '\v';

        public static bool IsDigit(char c) => c >= '0' && c <= '9';

        public static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        public static bool IsWordChar(char c) => IsLetter(c) || IsDigit(c) || c == '_';

        public static bool IsHexDigit(char c) => IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        protected static bool At(string text, int i, char c) => i < text.Length && text[i] == c;

        protected static bool AtIgnoreCase(string text, int i, char lower) => i < text.Length && char.ToLowerInvariant(text[i]) == lower;
    }

    /// <summary>
    /// An operator or punctuation symbol. The <c>notFollowedBy</c> characters keep the longest-match rule
    /// of the old tokenizer: <c>&lt;</c> does not match the start of <c>&lt;=</c>.
    /// </summary>
    internal sealed class SymbolTerminal : Terminal<string>
    {
        private readonly string _text;
        private readonly string _notFollowedBy;

        public SymbolTerminal(string text, string notFollowedBy = "") : base("\"" + text + "\"")
        {
            _text = text;
            _notFollowedBy = notFollowedBy;
        }

        protected override int Read(string text, int start, out string value)
        {
            value = _text;
            if (string.CompareOrdinal(text, start, _text, 0, _text.Length) != 0)
            {
                return 0;
            }

            int next = start + _text.Length;
            if (next < text.Length && _notFollowedBy.IndexOf(text[next]) >= 0)
            {
                return 0;
            }

            return _text.Length;
        }
    }

    /// <summary>
    /// A word: <c>[a-z_][a-z0-9_]*</c>, ASCII, case-insensitive. Matches either any word that is not a
    /// keyword (a name) or exactly one keyword. A keyword never matches the start of a longer name.
    /// </summary>
    internal sealed class WordTerminal : Terminal<string>
    {
        private static readonly HashSet<string> s_keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "and", "or", "xor", "not", "in", "true", "false", "null", "if", "cast",
        };

        private readonly string? _keyword;

        private WordTerminal(string? keyword, string expected) : base(expected)
        {
            _keyword = keyword;
        }

        public static WordTerminal Identifier() => new WordTerminal(null, "<name>");

        public static WordTerminal Keyword(string keyword) => new WordTerminal(keyword, "\"" + keyword + "\"");

        protected override int Read(string text, int start, out string value)
        {
            value = string.Empty;
            if (start >= text.Length || !(IsLetter(text[start]) || text[start] == '_'))
            {
                return 0;
            }

            int end = start + 1;
            while (end < text.Length && IsWordChar(text[end]))
            {
                end++;
            }

            int length = end - start;
            if (_keyword == null)
            {
                value = text.Substring(start, length);
                return s_keywords.Contains(value) ? 0 : length;
            }

            if (length == _keyword.Length && string.Compare(text, start, _keyword, 0, length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                value = _keyword;
                return length;
            }

            return 0;
        }
    }

    /// <summary>
    /// Integer, hexadecimal and real literals, with the longest-match rule between them.
    /// Integer: <c>\d+(u|l|ul|lu|f|m)?</c>; hex: <c>0x[0-9a-f]+(u|l|ul|lu)?</c>;
    /// real: <c>\d*SEP\d+(e[+-]\d{1,3})?(d|f|m)?</c>, with <c>\d+</c> before the separator when
    /// <c>RequireDigitsBeforeDecimalPoint</c> is set.
    /// </summary>
    internal sealed class NumberTerminal : Terminal<LiteralNode>
    {
        private readonly char _decimalSeparator;
        private readonly bool _requireDigitsBefore;

        public NumberTerminal(char decimalSeparator, bool requireDigitsBefore) : base("<number>")
        {
            _decimalSeparator = decimalSeparator;
            _requireDigitsBefore = requireDigitsBefore;
        }

        protected override int Read(string text, int start, out LiteralNode value)
        {
            value = null!;
            int integer = ReadInteger(text, start);
            int hex = ReadHex(text, start);
            int real = ReadReal(text, start);

            // Longest match wins; on a tie the order of the old tokenizer's matchers decides (integer,
            // hex before real), which cannot tie in practice.
            int best = Math.Max(integer, Math.Max(hex, real));
            if (best == 0)
            {
                return 0;
            }

            string image = text.Substring(start, best);
            if (best == integer)
            {
                value = new LiteralNode(LiteralKind.Integer, image);
            }
            else if (best == hex)
            {
                value = new LiteralNode(LiteralKind.Hex, image);
            }
            else
            {
                value = new LiteralNode(LiteralKind.Real, image);
            }

            return best;
        }

        private static int ReadInteger(string text, int start)
        {
            int i = start;
            while (i < text.Length && IsDigit(text[i]))
            {
                i++;
            }

            if (i == start)
            {
                return 0;
            }

            if ((AtIgnoreCase(text, i, 'u') && AtIgnoreCase(text, i + 1, 'l')) || (AtIgnoreCase(text, i, 'l') && AtIgnoreCase(text, i + 1, 'u')))
            {
                return i + 2 - start;
            }

            if (AtIgnoreCase(text, i, 'u') || AtIgnoreCase(text, i, 'l') || AtIgnoreCase(text, i, 'f') || AtIgnoreCase(text, i, 'm'))
            {
                return i + 1 - start;
            }

            return i - start;
        }

        private static int ReadHex(string text, int start)
        {
            if (!(At(text, start, '0') && AtIgnoreCase(text, start + 1, 'x')))
            {
                return 0;
            }

            int i = start + 2;
            while (i < text.Length && IsHexDigit(text[i]))
            {
                i++;
            }

            if (i == start + 2)
            {
                return 0;
            }

            if ((AtIgnoreCase(text, i, 'u') && AtIgnoreCase(text, i + 1, 'l')) || (AtIgnoreCase(text, i, 'l') && AtIgnoreCase(text, i + 1, 'u')))
            {
                return i + 2 - start;
            }

            if (AtIgnoreCase(text, i, 'u') || AtIgnoreCase(text, i, 'l'))
            {
                return i + 1 - start;
            }

            return i - start;
        }

        private int ReadReal(string text, int start)
        {
            int i = start;
            while (i < text.Length && IsDigit(text[i]))
            {
                i++;
            }

            if ((_requireDigitsBefore && i == start) || !At(text, i, _decimalSeparator))
            {
                return 0;
            }

            i++;
            int fractionStart = i;
            while (i < text.Length && IsDigit(text[i]))
            {
                i++;
            }

            if (i == fractionStart)
            {
                return 0;
            }

            // Optional exponent: e, a sign, one to three digits; incomplete means no exponent.
            if (AtIgnoreCase(text, i, 'e') && (At(text, i + 1, '+') || At(text, i + 1, '-')))
            {
                int j = i + 2;
                while (j < text.Length && j < i + 5 && IsDigit(text[j]))
                {
                    j++;
                }

                if (j > i + 2)
                {
                    i = j;
                }
            }

            if (AtIgnoreCase(text, i, 'd') || AtIgnoreCase(text, i, 'f') || AtIgnoreCase(text, i, 'm'))
            {
                i++;
            }

            return i - start;
        }
    }

    /// <summary>
    /// A string (<c>"..."</c>) or char (<c>'...'</c>) literal: any character but the quote, CR, LF
    /// and backslash, or an escape <c>\uXXXX</c> or <c>\\ \" \' \t \r \n</c> (case-insensitive, as before).
    /// </summary>
    internal sealed class QuotedTerminal : Terminal<LiteralNode>
    {
        private readonly char _quote;

        public QuotedTerminal(char quote) : base(quote == '"' ? "<string>" : "<char>")
        {
            _quote = quote;
        }

        protected override int Read(string text, int start, out LiteralNode value)
        {
            value = null!;
            if (!At(text, start, _quote))
            {
                return 0;
            }

            int i = start + 1;
            int characters = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == _quote)
                {
                    // A char literal holds exactly one character or escape.
                    if (_quote == '\'' && characters != 1)
                    {
                        return 0;
                    }

                    int length = i + 1 - start;
                    value = new LiteralNode(_quote == '"' ? LiteralKind.String : LiteralKind.Char, text.Substring(start, length));
                    return length;
                }

                if (c == '\r' || c == '\n')
                {
                    return 0;
                }

                if (c == '\\')
                {
                    if (AtIgnoreCase(text, i + 1, 'u') && i + 5 < text.Length
                        && IsHexDigit(text[i + 2]) && IsHexDigit(text[i + 3]) && IsHexDigit(text[i + 4]) && IsHexDigit(text[i + 5]))
                    {
                        i += 6;
                    }
                    else if (i + 1 < text.Length && "\\\"'tTrRnN".IndexOf(text[i + 1]) >= 0)
                    {
                        i += 2;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    i++;
                }

                characters++;
            }

            return 0;
        }
    }

    /// <summary>
    /// Date and time-span literals. Time span: <c>##(\d+\.)?\d{2}:\d{2}(:\d{2}(\.\d{1,7})?)?#</c>;
    /// date: <c>#[^#]+#</c>, its content parsed later with the date format option.
    /// </summary>
    internal sealed class HashTerminal : Terminal<LiteralNode>
    {
        public HashTerminal() : base("<date or time span>")
        {
        }

        protected override int Read(string text, int start, out LiteralNode value)
        {
            value = null!;
            if (!At(text, start, '#'))
            {
                return 0;
            }

            if (At(text, start + 1, '#'))
            {
                int length = ReadTimeSpan(text, start);
                if (length > 0)
                {
                    value = new LiteralNode(LiteralKind.TimeSpan, text.Substring(start, length));
                }

                return length;
            }

            int end = text.IndexOf('#', start + 1);
            if (end < 0 || end == start + 1)
            {
                return 0;
            }

            value = new LiteralNode(LiteralKind.DateTime, text.Substring(start, end + 1 - start));
            return end + 1 - start;
        }

        private static int ReadTimeSpan(string text, int start)
        {
            int i = start + 2;

            // Optional days: \d+ followed by a dot. Only taken if the rest matches as well.
            int days = i;
            while (days < text.Length && IsDigit(text[days]))
            {
                days++;
            }

            if (days > i && At(text, days, '.'))
            {
                int afterDays = MatchClock(text, days + 1);
                if (afterDays > 0)
                {
                    return afterDays - start;
                }
            }

            int end = MatchClock(text, i);
            return end > 0 ? end - start : 0;
        }

        // \d{2}:\d{2}(:\d{2}(\.\d{1,7})?)?# ; returns the index after the closing hash, or 0.
        private static int MatchClock(string text, int i)
        {
            if (!(TwoDigits(text, i) && At(text, i + 2, ':') && TwoDigits(text, i + 3)))
            {
                return 0;
            }

            i += 5;
            if (At(text, i, ':') && TwoDigits(text, i + 1))
            {
                int afterSeconds = i + 3;
                int j = afterSeconds;
                if (At(text, j, '.'))
                {
                    int k = j + 1;
                    while (k < text.Length && k < j + 8 && IsDigit(text[k]))
                    {
                        k++;
                    }

                    if (k > j + 1 && At(text, k, '#'))
                    {
                        return k + 1;
                    }
                }

                if (At(text, afterSeconds, '#'))
                {
                    return afterSeconds + 1;
                }
            }

            return At(text, i, '#') ? i + 1 : 0;
        }

        private static bool TwoDigits(string text, int i) => i + 1 < text.Length && IsDigit(text[i]) && IsDigit(text[i + 1]);
    }

    /// <summary>Succeeds only at the end of the input (after whitespace).</summary>
    internal sealed class EndTerminal : Terminal<bool>
    {
        public EndTerminal() : base("<end of expression>")
        {
        }

        protected override bool AllowsEmpty => true;

        protected override int Read(string text, int start, out bool value)
        {
            value = true;
            return start >= text.Length ? 0 : -1;
        }
    }
}
