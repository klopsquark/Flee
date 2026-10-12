using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Flee.ExpressionElements;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Base.Literals;
using Flee.ExpressionElements.Literals;
using Flee.ExpressionElements.Literals.Integral;
using Flee.ExpressionElements.LogicalBitwise;
using Flee.ExpressionElements.MemberElements;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.Syntax
{
    /// <summary>
    /// Turns the syntax tree into expression elements. Mirrors <c>FleeExpressionAnalyzer</c>, the
    /// Grammatica callbacks, element for element, so that both parsers give the same element tree.
    /// </summary>
    internal sealed class ElementBuilder
    {
        private static readonly Regex s_unicodeEscape = new Regex("\\\\u[0-9a-f]{4}", RegexOptions.IgnoreCase);
        private static readonly Regex s_regularEscape = new Regex("\\\\[\\\\\"'trn]", RegexOptions.IgnoreCase);

        private readonly IServiceProvider _services;

        private ElementBuilder(IServiceProvider services) => _services = services;

        public static ExpressionElement Build(SyntaxNode node, IServiceProvider services) =>
            new ElementBuilder(services).Visit(node);

        private ExpressionElement Visit(SyntaxNode node, bool negated = false)
        {
            switch (node)
            {
                case LiteralNode literal:
                    return Literal(literal, negated);
                case BinaryChainNode chain:
                    return Binary(chain);
                case NotNode not:
                    return Unary(new NotElement(), Visit(not.Operand));
                case NegateNode negate:
                    return Negate(negate);
                case InNode inNode:
                    return In(inNode);
                case MemberNode member:
                    return Member(member, negated);
                case NameNode name:
                    return new IdentifierElement(name.Name);
                case CallNode call:
                    return new FunctionCallElement(call.Name, new ArgumentList(VisitAll(call.Arguments)));
                case IndexNode index:
                    return new IndexerElement(new ArgumentList(VisitAll(index.Arguments)));
                case IfNode ifNode:
                    return new ConditionalElement(Visit(ifNode.Condition), Visit(ifNode.WhenTrue), Visit(ifNode.WhenFalse));
                case CastNode cast:
                    return new CastElement(Visit(cast.Operand), cast.TypeParts, cast.IsArray, _services);
                default:
                    throw new InvalidOperationException("Unknown syntax node " + node.GetType().Name);
            }
        }

        private ArrayList VisitAll(IReadOnlyList<SyntaxNode> nodes)
        {
            var list = new ArrayList(nodes.Count);
            foreach (SyntaxNode node in nodes)
            {
                list.Add(Visit(node));
            }

            return list;
        }

        private ExpressionElement Binary(BinaryChainNode chain)
        {
            var values = new ArrayList(1 + 2 * chain.Rest.Count) { Visit(chain.First) };
            foreach ((string op, SyntaxNode operand) in chain.Rest)
            {
                values.Add(Operation(chain.Level, op));
                values.Add(Visit(operand));
            }

            Type elementType = chain.Level switch
            {
                BinaryLevel.Xor => typeof(XorElement),
                BinaryLevel.Or => typeof(AndOrElement),
                BinaryLevel.And => typeof(AndOrElement),
                BinaryLevel.Compare => typeof(CompareElement),
                BinaryLevel.Shift => typeof(ShiftElement),
                _ => typeof(ArithmeticElement),
            };

            return BinaryExpressionElement.CreateElement(values, elementType);
        }

        // The operation values the old analyzer put on its token nodes.
        private static object Operation(BinaryLevel level, string op)
        {
            switch (level)
            {
                case BinaryLevel.Xor:
                    return "Xor";
                case BinaryLevel.Or:
                    return AndOrOperation.Or;
                case BinaryLevel.And:
                    return AndOrOperation.And;
            }

            switch (op)
            {
                case "+": return BinaryArithmeticOperation.Add;
                case "-": return BinaryArithmeticOperation.Subtract;
                case "*": return BinaryArithmeticOperation.Multiply;
                case "/": return BinaryArithmeticOperation.Divide;
                case "%": return BinaryArithmeticOperation.Mod;
                case "^": return BinaryArithmeticOperation.Power;
                case "=": return LogicalCompareOperation.Equal;
                case "<>": return LogicalCompareOperation.NotEqual;
                case "<": return LogicalCompareOperation.LessThan;
                case ">": return LogicalCompareOperation.GreaterThan;
                case "<=": return LogicalCompareOperation.LessThanOrEqual;
                case ">=": return LogicalCompareOperation.GreaterThanOrEqual;
                case "<<": return ShiftOperation.LeftShift;
                case ">>": return ShiftOperation.RightShift;
                default: throw new InvalidOperationException("Unknown operator " + op);
            }
        }

        private static ExpressionElement Unary(UnaryElement element, ExpressionElement child)
        {
            element.SetChild(child);
            return element;
        }

        // An integer literal straight after "-" is created as negated, so that Int32.MinValue and
        // Int64.MinValue parse; a resulting Int32 or Int64 constant is folded instead of negated.
        private ExpressionElement Negate(NegateNode negate)
        {
            var member = (MemberNode)negate.Operand;
            bool literalFollows = member.First is LiteralNode { Kind: LiteralKind.Integer or LiteralKind.Hex };
            ExpressionElement child = Member(member, literalFollows);

            if (child.GetType() == typeof(Int32LiteralElement))
            {
                ((Int32LiteralElement)child).Negate();
                return child;
            }

            if (child.GetType() == typeof(Int64LiteralElement))
            {
                ((Int64LiteralElement)child).Negate();
                return child;
            }

            return Unary(new NegateElement(), child);
        }

        private ExpressionElement Member(MemberNode member, bool negated)
        {
            ExpressionElement first = Visit(member.First, negated);
            if (member.Postfixes.Count == 0 && !(first is MemberElement))
            {
                return first;
            }

            var values = new ArrayList(1 + member.Postfixes.Count) { first };
            foreach (SyntaxNode postfix in member.Postfixes)
            {
                values.Add(Visit(postfix));
            }

            return new InvocationListElement(values, _services);
        }

        private ExpressionElement In(InNode node)
        {
            ExpressionElement operand = Visit(node.Operand);
            if (node.TargetList != null)
            {
                return new InElement(operand, VisitAll(node.TargetList));
            }

            var target = new ArrayList { new IdentifierElement(node.TargetName!) };
            return new InElement(operand, new InvocationListElement(target, _services));
        }

        private ExpressionElement Literal(LiteralNode literal, bool negated)
        {
            string image = literal.Image;
            switch (literal.Kind)
            {
                case LiteralKind.Integer:
                    return IntegralLiteralElement.Create(image, false, negated, _services);
                case LiteralKind.Hex:
                    return IntegralLiteralElement.Create(image, true, negated, _services);
                case LiteralKind.Real:
                    return RealLiteralElement.Create(image, _services);
                case LiteralKind.String:
                    return new StringLiteralElement(DoEscapes(image));
                case LiteralKind.Char:
                    return new CharLiteralElement(DoEscapes(image)[0]);
                case LiteralKind.True:
                    return new BooleanLiteralElement(true);
                case LiteralKind.False:
                    return new BooleanLiteralElement(false);
                case LiteralKind.Null:
                    return new NullLiteralElement();
                case LiteralKind.DateTime:
                    var context = (ExpressionContext)_services.GetService(typeof(ExpressionContext));
                    return new DateTimeLiteralElement(image.Substring(1, image.Length - 2), context);
                case LiteralKind.TimeSpan:
                    return new TimeSpanLiteralElement(image.Substring(2, image.Length - 3));
                default:
                    throw new InvalidOperationException("Unknown literal kind " + literal.Kind);
            }
        }

        // Copied from FleeExpressionAnalyzer.DoEscapes, bugs included (two passes, so "\\u0041"
        // becomes "\A"; see B2 in the parser analysis). Fixed separately, with a test first.
        private static string DoEscapes(string image)
        {
            image = image.Substring(1, image.Length - 2);
            image = s_unicodeEscape.Replace(image, m => Convert.ToChar(int.Parse(m.Value.Substring(2), NumberStyles.AllowHexSpecifier)).ToString());
            image = s_regularEscape.Replace(image, RegularEscape);
            return image;
        }

        private static string RegularEscape(Match m)
        {
            string s = m.Value.Substring(1);
            switch (s)
            {
                case "\\":
                case "\"":
                case "'":
                    return s;
                case "t":
                case "T":
                    return "\t";
                case "n":
                case "N":
                    return "\n";
                case "r":
                case "R":
                    return "\r";
                default:
                    Debug.Assert(false, "Unrecognized escape sequence");
                    return s;
            }
        }
    }

    /// <summary>
    /// The names an expression refers to, for the calculation engines: the first name of every
    /// member chain (<c>a</c> in <c>a.b.c</c>) and the target of <c>x in list</c>.
    /// </summary>
    internal static class IdentifierCollector
    {
        public static List<string> Collect(SyntaxNode node)
        {
            var names = new List<string>();
            Walk(node, names);
            return names;
        }

        private static void Walk(SyntaxNode node, List<string> names)
        {
            switch (node)
            {
                case BinaryChainNode chain:
                    Walk(chain.First, names);
                    foreach ((_, SyntaxNode operand) in chain.Rest)
                    {
                        Walk(operand, names);
                    }

                    break;
                case NotNode not:
                    Walk(not.Operand, names);
                    break;
                case NegateNode negate:
                    Walk(negate.Operand, names);
                    break;
                case InNode inNode:
                    Walk(inNode.Operand, names);
                    if (inNode.TargetName != null)
                    {
                        names.Add(inNode.TargetName);
                    }
                    else
                    {
                        WalkAll(inNode.TargetList!, names);
                    }

                    break;
                case MemberNode member:
                    if (member.First is NameNode name)
                    {
                        names.Add(name.Name);
                    }
                    else
                    {
                        Walk(member.First, names);
                    }

                    foreach (SyntaxNode postfix in member.Postfixes)
                    {
                        Walk(postfix, names);
                    }

                    break;
                case CallNode call:
                    WalkAll(call.Arguments, names);
                    break;
                case IndexNode index:
                    WalkAll(index.Arguments, names);
                    break;
                case IfNode ifNode:
                    Walk(ifNode.Condition, names);
                    Walk(ifNode.WhenTrue, names);
                    Walk(ifNode.WhenFalse, names);
                    break;
                case CastNode cast:
                    Walk(cast.Operand, names);
                    break;
            }
        }

        private static void WalkAll(IReadOnlyList<SyntaxNode> nodes, List<string> names)
        {
            foreach (SyntaxNode node in nodes)
            {
                Walk(node, names);
            }
        }
    }
}
