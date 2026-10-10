#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>Interface implemented by all expressions</summary>
    /// <remarks>This is the base interface that exposes members common to both dynamic and generic expressions.</remarks>
    public interface IExpression
    {
        /// <summary>
        /// Creates a clone of the current expression
        /// </summary>
        /// <returns>A copy of the current expression with its own set of variables</returns>
        /// <remarks>Use this method when you need to create a copy of an existing expression without the parsing/compilation overhead</remarks>
        IExpression Clone();
        /// <summary>Gets the text the expression was created with</summary>
        /// <value>A string with the expression's text</value>
        /// <remarks>Use this property to get the text that was used to compile the expression.</remarks>
        string Text { get; }
        /// <summary>
        /// Gets the expression's <see cref="ExpressionInfo"/> instance.
        /// </summary>
        /// <value>The ExpressionInfo instance.</value>
        /// <remarks>
        /// Use this property to access the expression's ExpressionInfo instance which holds information about the expression.
        /// </remarks>
        ExpressionInfo Info { get; }
        /// <summary>Gets the context the expression was created with</summary>
        /// <value>
        /// The expression's <see cref="ExpressionContext"/> instance
        /// </value>
        /// <remarks>
        /// Use this property to get the context that was used to compile the expression.  It is the expression's own copy of that
        /// context, which shares the variables with the original (see <see cref="ExpressionContext.CompileDynamic"/>).
        /// </remarks>
        ExpressionContext Context { get; }
        /// <summary>Gets or sets the expression's owner</summary>
        /// <value>
        /// The expression's owner instance.  Must be of the same type as the expression's original owner, or of a type derived from it.
        /// </value>
        /// <remarks>Use this property to get or set the instance of the expression's owner.</remarks>
        /// <exception cref="ArgumentException">The new owner's type is not assignable to the type of the original owner.</exception>
        /// <exception cref="ArgumentNullException">The new owner is <see langword="null"/>.</exception>
        object Owner { get; set; }
    }
}
