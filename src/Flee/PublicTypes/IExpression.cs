#nullable enable
namespace Flee.PublicTypes
{
    public interface IExpression
    {
        IExpression Clone();
        string Text { get; }
        ExpressionInfo Info { get; }
        ExpressionContext Context { get; }
        object Owner { get; set; }
    }
}
