using Flee.ExpressionElements.Base;
using Flee.InternalTypes;


namespace Flee.ExpressionElements.MemberElements
{
    internal class ExpressionMemberElement : MemberElement
    {
        private readonly ExpressionElement _element;
        public ExpressionMemberElement(ExpressionElement element)
        {
            _element = element;
        }

        protected override void ResolveInternal()
        {
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            base.Emit(ilg, services);
            _element.Emit(ilg, services);
            if (_element.ResultType.IsValueType == true)
            {
                EmitValueTypeLoadAddress(ilg, this.ResultType);
            }
        }

        protected override bool SupportsInstance => true;

        protected override bool IsPublic => true;

        public override bool IsStatic => false;
        public override bool IsExtensionMethod => false;

        public override System.Type ResultType => _element.ResultType;
    }
}
