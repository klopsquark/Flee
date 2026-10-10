using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.ExpressionElements.Literals.Real
{
    internal class SingleLiteralElement : RealLiteralElement
    {
        private readonly float _value;

        private SingleLiteralElement()
        {
        }

        public SingleLiteralElement(float value)
        {
            _value = value;
        }

        public static SingleLiteralElement Parse(string image, IServiceProvider services)
        {
            ExpressionParserOptions options = (ExpressionParserOptions)services.GetService(typeof(ExpressionParserOptions));
            SingleLiteralElement element = new SingleLiteralElement();

            try
            {
                float value = options.ParseSingle(image);
                // .NET Core 3.0 and later return infinity for out-of-range input instead of
                // throwing OverflowException; a literal can never mean infinity (R-023).
                if (float.IsInfinity(value))
                {
                    element.OnParseOverflow(image);
                    return null;
                }
                return new SingleLiteralElement(value);
            }
            catch (OverflowException)
            {
                element.OnParseOverflow(image);
                return null;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ilg.Emit(OpCodes.Ldc_R4, _value);
        }

        public override System.Type ResultType => typeof(float);
    }
}
