using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    internal class RootExpressionElement : ExpressionElement
    {
        private readonly ExpressionElement _child;
        private readonly Type _resultType;
        public RootExpressionElement(ExpressionElement child, Type resultType)
        {
            _child = child;
            _resultType = resultType;
            this.Validate();
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            _child.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(_child.ResultType, _resultType, ilg);

            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));

            if (!options.IsGeneric)
            {
                ImplicitConverter.EmitImplicitConvert(_resultType, typeof(object), ilg);
            }

            ilg.Emit(OpCodes.Ret);
        }

        private void Validate()
        {
            if (!ImplicitConverter.EmitImplicitConvert(_child.ResultType, _resultType, null))
            {
                base.ThrowCompileException(CompileErrorResourceKeys.CannotConvertTypeToExpressionResult, CompileExceptionReason.TypeMismatch, _child.ResultType.Name, _resultType.Name);
            }
        }

        public override System.Type ResultType => typeof(object);
    }
}
