#nullable enable
namespace Flee.PublicTypes
{
    public interface IGenericExpression<T> : IExpression
    {
        T Evaluate();
    }
}
