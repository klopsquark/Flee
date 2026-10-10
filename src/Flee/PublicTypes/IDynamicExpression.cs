#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>Interface implemented by all expressions that evaluate to an Object</summary>
    /// <remarks>This is the interface that dynamic expressions must implement</remarks>
    public interface IDynamicExpression : IExpression
    {
        /// <summary>Evaluates the dynamic expression</summary>
        /// <returns>An Object instance that represents the result of evaluating the expression</returns>
        /// <remarks>Use this method to evaluate the expression.</remarks>
        object? Evaluate();
    }
}
