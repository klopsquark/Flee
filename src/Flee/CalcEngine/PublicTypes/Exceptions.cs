using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{

    public class CircularReferenceException : System.Exception
    {
        private readonly string _circularReferenceSource;

        internal CircularReferenceException()
        {
        }

        internal CircularReferenceException(string circularReferenceSource)
        {
            _circularReferenceSource = circularReferenceSource;
        }

        public override string Message
        {
            get
            {
                if (_circularReferenceSource == null)
                {
                    return "Circular reference detected in calculation engine";
                }
                else
                {
                    return $"Circular reference detected in calculation engine at '{_circularReferenceSource}'";
                }
            }
        }
    }

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

