using System.Globalization;
using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;


namespace Flee.ExpressionElements.Literals.Integral
{
    internal class Int32LiteralElement : IntegralLiteralElement
    {
        private Int32 _value;
        private const string MinValue = "2147483648";
        private readonly bool _isMinValue;
        public Int32LiteralElement(Int32 value)
        {
            _value = value;
        }

        private Int32LiteralElement()
        {
            _isMinValue = true;
        }

        public static Int32LiteralElement TryCreate(string image, bool isHex, bool negated)
        {
            if (negated & image == MinValue)
            {
                return new Int32LiteralElement();
            }
            else if (isHex)
            {
                Int32 value = default(Int32);

                // Since Int32.TryParse will succeed for a string like 0xFFFFFFFF we have to do some special handling
                if (!Int32.TryParse(image, NumberStyles.AllowHexSpecifier, null, out value))
                {
                    return null;
                }
                else if (value >= 0 & value <= Int32.MaxValue)
                {
                    return new Int32LiteralElement(value);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                Int32 value = default(Int32);

                if (Int32.TryParse(image,out value))
                {
                    return new Int32LiteralElement(value);
                }
                else
                {
                    return null;
                }
            }
        }

        public void Negate()
        {
            if (_isMinValue)
            {
                _value = Int32.MinValue;
            }
            else
            {
                _value = -_value;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(_value, ilg);
        }

        public override System.Type ResultType => typeof(Int32);

        public int Value => _value;
    }
}
