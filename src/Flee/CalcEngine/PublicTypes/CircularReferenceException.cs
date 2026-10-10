#nullable enable
using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    /// <summary>
    /// Represents the exception thrown when a circular reference is detected in the calculation engine.
    /// </summary>
    /// <remarks>
    /// This exception will be thrown when <see cref="CalculationEngine.Recalculate"/> is called on the CalculationEngine and there is a circular reference present.
    /// <see cref="CalculationEngine.BatchLoad"/> also throws it when the expressions in the batch reference each other in a circle.
    /// </remarks>
    public class CircularReferenceException : System.Exception
    {
        private readonly string? _circularReferenceSource;

        internal CircularReferenceException()
        {
        }

        internal CircularReferenceException(string circularReferenceSource)
        {
            _circularReferenceSource = circularReferenceSource;
        }

        /// <summary>Gets a message that describes the circular reference.</summary>
        /// <value>A message that names where the circular reference was found, if that is known.</value>
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
