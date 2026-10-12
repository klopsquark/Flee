#nullable enable
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Flee.PublicTypes;
using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Flee.Syntax
{
    /// <summary>
    /// The Flee expression language as a Parlot grammar. This class is the single definition of the
    /// syntax: precedence, associativity and every construct are in the constructor, lowest
    /// precedence last. The tokens are in <c>Terminals.cs</c>.
    /// </summary>
    /// <remarks>
    /// One grammar exists per combination of the parser options that change the syntax (decimal
    /// separator, argument separator, digits before the decimal point). Grammars are immutable and
    /// shared between threads; the state of a parse lives in <see cref="FleeParseContext"/>.
    /// </remarks>
    internal sealed class FleeGrammar
    {
        private static readonly ConcurrentDictionary<(char, char, bool), FleeGrammar> s_grammars =
            new ConcurrentDictionary<(char, char, bool), FleeGrammar>();

        private static readonly IReadOnlyList<SyntaxNode> s_noArguments = new SyntaxNode[0];

        private readonly Parser<SyntaxNode> _root;

        public static FleeGrammar For(ExpressionParserOptions options) =>
            s_grammars.GetOrAdd(
                (options.DecimalSeparator, options.FunctionArgumentSeparator, options.RequireDigitsBeforeDecimalPoint),
                key => new FleeGrammar(key.Item1, key.Item2, key.Item3));

        private FleeGrammar(char decimalSeparator, char argumentSeparator, bool requireDigitsBeforeDecimalPoint)
        {
            static Parser<string> Sym(string text, string notFollowedBy = "") => new SymbolTerminal(text, notFollowedBy);
            static Parser<string> Kw(string keyword) => WordTerminal.Keyword(keyword);

            var expression = Deferred<SyntaxNode>();
            var nested = new StackGuard<SyntaxNode>(expression);
            var name = WordTerminal.Identifier();
            // The old tokenizer took the longest token: with "," for both separators and no digit
            // required before it, ",5" is a number, never a separator followed by 5.
            var comma = argumentSeparator == decimalSeparator && !requireDigitsBeforeDecimalPoint
                ? Sym(argumentSeparator.ToString(), "0123456789")
                : Sym(argumentSeparator.ToString());
            var open = Sym("(");
            var close = Sym(")");
            var arguments = Separated(comma, nested).Then(list => (IReadOnlyList<SyntaxNode>)list);

            // Literals
            var literal = OneOf<SyntaxNode>(
                new NumberTerminal(decimalSeparator, requireDigitsBeforeDecimalPoint).Then<SyntaxNode>(x => x),
                new QuotedTerminal('"').Then<SyntaxNode>(x => x),
                new QuotedTerminal('\'').Then<SyntaxNode>(x => x),
                new HashTerminal().Then<SyntaxNode>(x => x),
                Kw("true").Then<SyntaxNode>(x => new LiteralNode(LiteralKind.True, x)),
                Kw("false").Then<SyntaxNode>(x => new LiteralNode(LiteralKind.False, x)),
                Kw("null").Then<SyntaxNode>(x => new LiteralNode(LiteralKind.Null, x)));

            // if(condition, whenTrue, whenFalse) and cast(value, type.name[])
            var ifExpression = Kw("if").SkipAnd(open).SkipAnd(nested).AndSkip(comma).And(nested).AndSkip(comma).And(nested).AndSkip(close)
                .Then<SyntaxNode>(x => new IfNode(x.Item1, x.Item2, x.Item3));
            var castType = name.And(ZeroOrMany(Sym(".").SkipAnd(name))).And(ZeroOrOne(Sym("[]")))
                .Then(x => (Parts: new[] { x.Item1 }.Concat(x.Item2).ToArray(), IsArray: x.Item3 != null));
            var castExpression = Kw("cast").SkipAnd(open).SkipAnd(nested).AndSkip(comma).And(castType).AndSkip(close)
                .Then<SyntaxNode>(x => new CastNode(x.Item1, x.Item2.Parts, x.Item2.IsArray));

            // name or name(arguments)
            var callArguments = open.SkipAnd(ZeroOrOne(arguments)).AndSkip(close).Then(x => x ?? s_noArguments);
            var memberFunction = name.And(ZeroOrOne(callArguments))
                .Then<SyntaxNode>(x => x.Item2 == null ? new NameNode(x.Item1) : new CallNode(x.Item1, x.Item2));
            var group = open.SkipAnd(nested).AndSkip(close);
            var basic = OneOf(literal, group, ifExpression, castExpression, memberFunction);

            // basic followed by .member, .call() and [index]; "[" is not the start of "[]"
            var postfix = OneOf(
                Sym(".").SkipAnd(memberFunction),
                Sym("[", "]").SkipAnd(arguments).AndSkip(Sym("]")).Then<SyntaxNode>(x => new IndexNode(x)));
            var member = new PostfixParser(basic, postfix);

            // Operators, highest precedence first. Unary minus applies once, to a member expression.
            var negate = OneOf(Sym("-").SkipAnd(member).Then<SyntaxNode>(x => new NegateNode(x)), member);
            var power = Chain(negate, BinaryLevel.Power, Sym("^"));
            var multiplicative = Chain(power, BinaryLevel.Multiplicative, Sym("*"), Sym("/"), Sym("%"));
            var additive = Chain(multiplicative, BinaryLevel.Additive, Sym("+"), Sym("-"));
            var shift = Chain(additive, BinaryLevel.Shift, Sym("<<"), Sym(">>"));
            var compare = Chain(shift, BinaryLevel.Compare,
                Sym("="), Sym("<>"), Sym("<="), Sym(">="), Sym("<", "=><"), Sym(">", "=>"));

            // value in name, value in (a, b, ...)
            var inTarget = OneOf(
                name.Then(x => new InTarget(x, null)),
                open.SkipAnd(arguments).AndSkip(close).Then(x => new InTarget(null, x)));
            var inExpression = compare.And(ZeroOrOne(Kw("in").SkipAnd(inTarget)))
                .Then(x => x.Item2 == null ? x.Item1 : new InNode(x.Item1, x.Item2.Name, x.Item2.List));

            // not applies once, to an in-expression
            var not = OneOf(Kw("not").SkipAnd(inExpression).Then<SyntaxNode>(x => new NotNode(x)), inExpression);
            var and = Chain(not, BinaryLevel.And, Kw("and"));
            var or = Chain(and, BinaryLevel.Or, Kw("or"));
            var xor = Chain(or, BinaryLevel.Xor, Kw("xor"));

            expression.Parser = xor;
            _root = nested.AndSkip(new EndTerminal());
        }

        /// <summary>Parses one expression; throws <see cref="SyntaxErrorException"/> if it is not valid.</summary>
        public SyntaxNode Parse(string text)
        {
            if (TryParse(text, false, out SyntaxNode? node, out FleeParseContext _))
            {
                return node!;
            }

            // Invalid: parse again, collecting what was expected where it failed.
            TryParse(text, true, out _, out FleeParseContext context);
            throw SyntaxErrorException.From(context, text);
        }

        private bool TryParse(string text, bool diagnose, out SyntaxNode? node, out FleeParseContext context)
        {
            context = new FleeParseContext(text, diagnose);
            var result = default(ParseResult<SyntaxNode>);
            try
            {
                bool ok = _root.Parse(context, ref result);
                node = ok ? result.Value : null;
                return ok;
            }
            catch (InsufficientExecutionStackException)
            {
                throw new SyntaxErrorException("the expression is nested too deeply", 0, text);
            }
        }

        // operand (operator operand)*, kept flat; the element builder makes it left-associative.
        private static Parser<SyntaxNode> Chain(Parser<SyntaxNode> operand, BinaryLevel level, params Parser<string>[] operators) =>
            new ChainParser(operand, level, operators);

        private sealed class InTarget
        {
            public InTarget(string? name, IReadOnlyList<SyntaxNode>? list)
            {
                Name = name;
                List = list;
            }

            public string? Name { get; }

            public IReadOnlyList<SyntaxNode>? List { get; }
        }
    }

    /// <summary>
    /// <c>operand (operator operand)*</c> as one loop. Same as
    /// <c>operand.And(ZeroOrMany(OneOf(operators).And(operand)))</c>, without the tuples and lists
    /// those allocate for every operand of every precedence level.
    /// </summary>
    internal sealed class ChainParser : Parser<SyntaxNode>
    {
        private readonly Parser<SyntaxNode> _operand;
        private readonly BinaryLevel _level;
        private readonly Parser<string>[] _operators;

        public ChainParser(Parser<SyntaxNode> operand, BinaryLevel level, Parser<string>[] operators)
        {
            _operand = operand;
            _level = level;
            _operators = operators;
        }

        public override bool Parse(ParseContext context, ref ParseResult<SyntaxNode> result)
        {
            var first = default(ParseResult<SyntaxNode>);
            if (!_operand.Parse(context, ref first))
            {
                return false;
            }

            List<(string, SyntaxNode)>? rest = null;
            int end = first.End;
            while (true)
            {
                TextPosition before = context.Scanner.Cursor.Position;
                var op = default(ParseResult<string>);
                bool found = false;
                foreach (Parser<string> candidate in _operators)
                {
                    if (candidate.Parse(context, ref op))
                    {
                        found = true;
                        break;
                    }
                }

                var operand = default(ParseResult<SyntaxNode>);
                if (!found || !_operand.Parse(context, ref operand))
                {
                    context.Scanner.Cursor.ResetPosition(before);
                    break;
                }

                (rest ??= new List<(string, SyntaxNode)>()).Add((op.Value!, operand.Value));
                end = operand.End;
            }

            result.Set(first.Start, end, rest == null ? first.Value : new BinaryChainNode(_level, first.Value, rest));
            return true;
        }
    }

    /// <summary><c>basic postfix*</c> into a <see cref="MemberNode"/>; like <see cref="ChainParser"/>, one loop.</summary>
    internal sealed class PostfixParser : Parser<SyntaxNode>
    {
        private static readonly SyntaxNode[] s_none = new SyntaxNode[0];

        private readonly Parser<SyntaxNode> _basic;
        private readonly Parser<SyntaxNode> _postfix;

        public PostfixParser(Parser<SyntaxNode> basic, Parser<SyntaxNode> postfix)
        {
            _basic = basic;
            _postfix = postfix;
        }

        public override bool Parse(ParseContext context, ref ParseResult<SyntaxNode> result)
        {
            var first = default(ParseResult<SyntaxNode>);
            if (!_basic.Parse(context, ref first))
            {
                return false;
            }

            List<SyntaxNode>? postfixes = null;
            int end = first.End;
            var next = default(ParseResult<SyntaxNode>);
            while (_postfix.Parse(context, ref next))
            {
                (postfixes ??= new List<SyntaxNode>()).Add(next.Value);
                end = next.End;
            }

            result.Set(first.Start, end, new MemberNode(first.Value, (IReadOnlyList<SyntaxNode>?)postfixes ?? s_none));
            return true;
        }
    }

    /// <summary>
    /// Every nested expression goes through here. Deep nesting would overflow the stack (a crash that
    /// cannot be caught); this turns it into a syntax error before that happens.
    /// </summary>
    internal sealed class StackGuard<T> : Parser<T>
    {
        private readonly Parser<T> _inner;

        public StackGuard(Parser<T> inner) => _inner = inner;

        public override bool Parse(ParseContext context, ref ParseResult<T> result)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            return _inner.Parse(context, ref result);
        }
    }

    /// <summary>A syntax error with its position; becomes an <see cref="ExpressionCompileException"/>.</summary>
    internal sealed class SyntaxErrorException : Exception
    {
        public SyntaxErrorException(string message, int offset, string text)
            : base(Describe(message, offset, text))
        {
            Offset = offset;
        }

        public int Offset { get; }

        public static SyntaxErrorException From(FleeParseContext context, string text)
        {
            int offset = Math.Max(context.FurthestOffset, 0);
            string found;
            if (offset >= text.Length)
            {
                found = "end of expression";
            }
            else if (Terminal<object>.IsWordChar(text[offset]))
            {
                int end = offset;
                while (end < text.Length && Terminal<object>.IsWordChar(text[end]))
                {
                    end++;
                }

                found = "\"" + text.Substring(offset, end - offset) + "\"";
            }
            else
            {
                found = "character '" + text[offset] + "'";
            }

            string message = "unexpected " + found;
            if (context.Expected.Count > 0)
            {
                message += ", expected " + (context.Expected.Count == 1 ? context.Expected[0] : "one of " + string.Join(", ", context.Expected));
            }

            return new SyntaxErrorException(message, offset, text);
        }

        private static string Describe(string message, int offset, string text)
        {
            int line = 1;
            int column = 1;
            for (int i = 0; i < offset && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return message + ", on line: " + line + " column: " + column;
        }
    }
}
