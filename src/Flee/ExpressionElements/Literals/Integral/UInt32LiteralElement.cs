using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    internal class UInt32LiteralElement : IntegralLiteralElement
    {
        private readonly UInt32 _value;
        public UInt32LiteralElement(UInt32 value)
        {
            _value = value;
        }

        public static UInt32LiteralElement TryCreate(string image, System.Globalization.NumberStyles ns)
        {
            UInt32 value = default(UInt32);
            if (UInt32.TryParse(image, ns, null, out value))
            {
                return new UInt32LiteralElement(value);
            }
            else
            {
                return null;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            // The IL constant is the same bits, read as signed; unchecked so values above
            // Int32.MaxValue do not throw (R-021).
            EmitLoad(unchecked((int)_value), ilg);
        }

        public override System.Type ResultType => typeof(UInt32);
    }
}
