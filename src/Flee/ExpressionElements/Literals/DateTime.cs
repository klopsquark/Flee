using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Literals
{
    internal class DateTimeLiteralElement : LiteralElement
    {
        private DateTime _value;
        public DateTimeLiteralElement(string image, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;

            if (DateTime.TryParseExact(image, options.DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _value) == false)
            {
                base.ThrowCompileException(CompileErrorResourceKeys.CannotParseType, CompileExceptionReason.InvalidFormat, typeof(DateTime).Name);
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            int index = ilg.GetTempLocalIndex(typeof(DateTime));

            Utility.EmitLoadLocalAddress(ilg, index);

            LiteralElement.EmitLoad(_value.Ticks, ilg);

            ConstructorInfo ci = typeof(DateTime).GetConstructor(new Type[] { typeof(long) });

            ilg.Emit(OpCodes.Call, ci);

            Utility.EmitLoadLocal(ilg, index);
        }

        public override System.Type ResultType => typeof(DateTime);
    }
}
