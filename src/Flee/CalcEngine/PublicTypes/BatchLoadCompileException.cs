#nullable enable
using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
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

        public string AtomName => _atomName;

        public string ExpressionText => _expressionText;
    }
}
