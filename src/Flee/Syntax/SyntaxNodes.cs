#nullable enable

namespace Flee.Syntax
{
    // The syntax tree the Parlot grammar produces. It holds only what the text says; names are
    // resolved and types checked afterwards, when ElementBuilder turns it into expression elements.

    internal abstract class SyntaxNode
    {
    }

    internal enum LiteralKind
    {
        Integer,
        Hex,
        Real,
        String,
        Char,
        True,
        False,
        Null,
        DateTime,
        TimeSpan,
    }

    /// <summary>A literal. <see cref="Image"/> is the source text, quotes and hashes included.</summary>
    internal sealed class LiteralNode : SyntaxNode
    {
        public LiteralNode(LiteralKind kind, string image)
        {
            Kind = kind;
            Image = image;
        }

        public LiteralKind Kind { get; }

        public string Image { get; }
    }

    /// <summary>A left-associative chain of one precedence level: operand (operator operand)*.</summary>
    internal sealed class BinaryChainNode : SyntaxNode
    {
        public BinaryChainNode(BinaryLevel level, SyntaxNode first, IReadOnlyList<(string Operator, SyntaxNode Operand)> rest)
        {
            Level = level;
            First = first;
            Rest = rest;
        }

        public BinaryLevel Level { get; }

        public SyntaxNode First { get; }

        public IReadOnlyList<(string Operator, SyntaxNode Operand)> Rest { get; }
    }

    internal enum BinaryLevel
    {
        Xor,
        Or,
        And,
        Compare,
        Shift,
        Additive,
        Multiplicative,
        Power,
    }

    internal sealed class NotNode : SyntaxNode
    {
        public NotNode(SyntaxNode operand) => Operand = operand;

        public SyntaxNode Operand { get; }
    }

    internal sealed class NegateNode : SyntaxNode
    {
        public NegateNode(SyntaxNode operand) => Operand = operand;

        /// <summary>Always a <see cref="MemberNode"/>, as in the grammar (NegateExpression = "-"? MemberExpression).</summary>
        public SyntaxNode Operand { get; }
    }

    /// <summary><c>operand in target</c>; the target is a bare name or a parenthesised list.</summary>
    internal sealed class InNode : SyntaxNode
    {
        public InNode(SyntaxNode operand, string? targetName, IReadOnlyList<SyntaxNode>? targetList)
        {
            Operand = operand;
            TargetName = targetName;
            TargetList = targetList;
        }

        public SyntaxNode Operand { get; }

        public string? TargetName { get; }

        public IReadOnlyList<SyntaxNode>? TargetList { get; }
    }

    /// <summary>A primary followed by member accesses and indexers: <c>a.b(1)[2].c</c>.</summary>
    internal sealed class MemberNode : SyntaxNode
    {
        public MemberNode(SyntaxNode first, IReadOnlyList<SyntaxNode> postfixes)
        {
            First = first;
            Postfixes = postfixes;
        }

        public SyntaxNode First { get; }

        /// <summary><see cref="NameNode"/>, <see cref="CallNode"/> (after a dot) or <see cref="IndexNode"/>.</summary>
        public IReadOnlyList<SyntaxNode> Postfixes { get; }
    }

    internal sealed class NameNode : SyntaxNode
    {
        public NameNode(string name) => Name = name;

        public string Name { get; }
    }

    internal sealed class CallNode : SyntaxNode
    {
        public CallNode(string name, IReadOnlyList<SyntaxNode> arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }

        public IReadOnlyList<SyntaxNode> Arguments { get; }
    }

    internal sealed class IndexNode : SyntaxNode
    {
        public IndexNode(IReadOnlyList<SyntaxNode> arguments) => Arguments = arguments;

        public IReadOnlyList<SyntaxNode> Arguments { get; }
    }

    internal sealed class IfNode : SyntaxNode
    {
        public IfNode(SyntaxNode condition, SyntaxNode whenTrue, SyntaxNode whenFalse)
        {
            Condition = condition;
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }

        public SyntaxNode Condition { get; }

        public SyntaxNode WhenTrue { get; }

        public SyntaxNode WhenFalse { get; }
    }

    internal sealed class CastNode : SyntaxNode
    {
        public CastNode(SyntaxNode operand, string[] typeParts, bool isArray)
        {
            Operand = operand;
            TypeParts = typeParts;
            IsArray = isArray;
        }

        public SyntaxNode Operand { get; }

        public string[] TypeParts { get; }

        public bool IsArray { get; }
    }
}
