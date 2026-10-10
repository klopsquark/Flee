using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;


namespace Flee.ExpressionElements.Literals.Real
{
    internal class DoubleLiteralElement : RealLiteralElement
    {
        private readonly double _value;

        private DoubleLiteralElement()
        {
        }

        public DoubleLiteralElement(double value)
        {
            _value = value;
        }

        public static DoubleLiteralElement Parse(string image, IServiceProvider services)
        {
            ExpressionParserOptions options = (ExpressionParserOptions)services.GetService(typeof(ExpressionParserOptions));
            DoubleLiteralElement element = new DoubleLiteralElement();

            try
            {
                double value = options.ParseDouble(image);
                // .NET Core 3.0 and later return infinity for out-of-range input instead of
                // throwing OverflowException; a literal can never mean infinity (R-023).
                if (double.IsInfinity(value))
                {
                    element.OnParseOverflow(image);
                    return null;
                }
                return new DoubleLiteralElement(value);
            }
            catch (OverflowException ex)
            {
                element.OnParseOverflow(image);
                return null;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ilg.Emit(OpCodes.Ldc_R8, _value);
        }

        public override System.Type ResultType => typeof(double);
    }
}
