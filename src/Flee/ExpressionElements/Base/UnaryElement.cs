using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    internal abstract class UnaryElement : ExpressionElement
    {

        protected ExpressionElement Child;

        private Type _resultType;
        public void SetChild(ExpressionElement child)
        {
            Child = child;
            _resultType = this.GetResultType(child.ResultType);

            if (_resultType == null)
            {
                base.ThrowCompileException(CompileErrorResourceKeys.OperationNotDefinedForType, CompileExceptionReason.TypeMismatch, Child.ResultType.Name);
            }
        }

        protected abstract Type GetResultType(Type childType);

        public override System.Type ResultType => _resultType;
    }

}
