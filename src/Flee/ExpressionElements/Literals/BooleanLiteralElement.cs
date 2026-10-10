using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals
{
    internal class BooleanLiteralElement : LiteralElement
    {
        private readonly bool _value;
        public BooleanLiteralElement(bool value)
        {
            _value = value;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(_value, ilg);
        }

        public override System.Type ResultType => typeof(bool);
    }
}
