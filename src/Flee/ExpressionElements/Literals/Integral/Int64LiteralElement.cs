using System.Globalization;
using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    internal class Int64LiteralElement : IntegralLiteralElement
    {

        private Int64 _value;
        private const string MinValue = "9223372036854775808";

        private readonly bool _isMinValue;
        public Int64LiteralElement(Int64 value)
        {
            _value = value;
        }

        private Int64LiteralElement()
        {
            _isMinValue = true;
        }

        public static Int64LiteralElement TryCreate(string image, bool isHex, bool negated)
        {
            if (negated && image == MinValue)
            {
                return new Int64LiteralElement();
            }
            else if (isHex)
            {
                Int64 value = default(Int64);

                if (!Int64.TryParse(image, NumberStyles.AllowHexSpecifier, null, out value))
                {
                    return null;
                }
                else if (value >= 0 && value <= Int64.MaxValue)
                {
                    return new Int64LiteralElement(value);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                Int64 value = default(Int64);

                if (Int64.TryParse(image, out value))
                {
                    return new Int64LiteralElement(value);
                }
                else
                {
                    return null;
                }
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(_value, ilg);
        }

        public void Negate()
        {
            if (_isMinValue)
            {
                _value = Int64.MinValue;
            }
            else
            {
                _value = -_value;
            }
        }

        public override System.Type ResultType => typeof(Int64);
    }
}
