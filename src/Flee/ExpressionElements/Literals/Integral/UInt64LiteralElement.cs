using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;


namespace Flee.ExpressionElements.Literals.Integral
{
    internal class UInt64LiteralElement : IntegralLiteralElement
    {
        private readonly UInt64 _value;
        public UInt64LiteralElement(string image, System.Globalization.NumberStyles ns)
        {
            try
            {
                _value = UInt64.Parse(image, ns);
            }
            catch (OverflowException)
            {
                base.OnParseOverflow(image);
            }
        }

        public UInt64LiteralElement(UInt64 value)
        {
            _value = value;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            // The IL constant is the same bits, read as signed; unchecked so values above
            // Int64.MaxValue do not throw (R-021).
            EmitLoad(unchecked((long)_value), ilg);
        }

        public override System.Type ResultType => typeof(UInt64);
    }
}
