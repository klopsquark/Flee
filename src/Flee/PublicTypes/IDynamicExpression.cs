#nullable enable
namespace Flee.PublicTypes
{
    public interface IDynamicExpression : IExpression
    {
        object? Evaluate();
    }
}
