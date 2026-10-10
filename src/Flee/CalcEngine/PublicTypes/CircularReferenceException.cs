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
}
