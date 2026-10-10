#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>Interface implemented by all expressions that evaluate to a specific type</summary>
    /// <typeparam name="T">The type that the expression will evaluate to</typeparam>
    /// <remarks>This is the interface that generic expressions must implement</remarks>
    public interface IGenericExpression<T> : IExpression
    {
        /// <summary>Evaluates the generic expression</summary>
        /// <returns>The result of evaluating the expression</returns>
        /// <remarks>Use this method to evaluate the expression.</remarks>
        T Evaluate();
    }
}
