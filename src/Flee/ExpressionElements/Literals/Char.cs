using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;


namespace Flee.ExpressionElements.Literals
{
    internal class CharLiteralElement : LiteralElement
    {
        private readonly char _value;
        public CharLiteralElement(char value)
        {
            _value = value;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            int intValue = Convert.ToInt32(_value);
            EmitLoad(intValue, ilg);
        }

        public override System.Type ResultType => typeof(char);
    }
}
