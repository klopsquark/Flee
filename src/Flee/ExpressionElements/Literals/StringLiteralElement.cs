using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;


namespace Flee.ExpressionElements.Literals
{
    internal class StringLiteralElement : LiteralElement
    {
        private readonly string _value;
        public StringLiteralElement(string value)
        {
            _value = value;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ilg.Emit(OpCodes.Ldstr, _value);
        }

        public override System.Type ResultType => typeof(string);
    }
}
