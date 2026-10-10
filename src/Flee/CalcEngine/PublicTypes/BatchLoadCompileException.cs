#nullable enable
using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    /// <summary>
    /// The exception thrown when a batch loaded expression cannot be compiled.
    /// </summary>
    /// <remarks>
    /// This exception is thrown whenever a batch loaded expression cannot be compiled.  You use the AtomName and ExpressionText properties
    /// to get more information about the source of the exception.  The <see cref="ExpressionCompileException"/> that caused it is the
    /// <see cref="Exception.InnerException"/>.
    /// </remarks>
    public class BatchLoadCompileException : Exception
    {

        private readonly string _atomName;

        private readonly string _expressionText;
        internal BatchLoadCompileException(string atomName, string expressionText, ExpressionCompileException innerException) : base(
            $"Batch Load: The expression for atom '${atomName}' could not be compiled", innerException)
        {
            _atomName = atomName;
            _expressionText = expressionText;
        }

        /// <summary>
        /// Gets the name of the expression that could not be compiled.
        /// </summary>
        /// <value>The name of the expression</value>
        /// <remarks>Use this property to determine the name of the expression that caused the exception.</remarks>
        public string AtomName => _atomName;

        /// <summary>
        /// Gets the text of the expression that could not be compiled.
        /// </summary>
        /// <value>The text of the expression</value>
        /// <remarks>Use this property to determine the text of the expression that caused the exception.</remarks>
        public string ExpressionText => _expressionText;
    }
}
