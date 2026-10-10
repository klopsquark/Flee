using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    internal class ConditionalElement : ExpressionElement
    {
        private readonly ExpressionElement _condition;
        private readonly ExpressionElement _whenTrue;
        private readonly ExpressionElement _whenFalse;
        private readonly Type _resultType;
        public ConditionalElement(ExpressionElement condition, ExpressionElement whenTrue, ExpressionElement whenFalse)
        {
            _condition = condition;
            _whenTrue = whenTrue;
            _whenFalse = whenFalse;

            if ((!object.ReferenceEquals(_condition.ResultType, typeof(bool))))
            {
                base.ThrowCompileException(CompileErrorResourceKeys.FirstArgNotBoolean, CompileExceptionReason.TypeMismatch);
            }

            // The result type is the type that is common to the true/false operands
            if (ImplicitConverter.EmitImplicitConvert(_whenFalse.ResultType, _whenTrue.ResultType, null) == true)
            {
                _resultType = _whenTrue.ResultType;
            }
            else if (ImplicitConverter.EmitImplicitConvert(_whenTrue.ResultType, _whenFalse.ResultType, null) == true)
            {
                _resultType = _whenFalse.ResultType;
            }
            else
            {
                base.ThrowCompileException(CompileErrorResourceKeys.NeitherArgIsConvertibleToTheOther, CompileExceptionReason.TypeMismatch, _whenTrue.ResultType.Name, _whenFalse.ResultType.Name);
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            this.EmitConditional(ilg, services);
        }

        private void EmitConditional(FleeILGenerator ilg, IServiceProvider services)
        {
            Label falseLabel = ilg.DefineLabel();
            Label endLabel = ilg.DefineLabel();

            // Emit the condition
            _condition.Emit(ilg, services);

            // On false go to the false operand
            ilg.EmitBranchFalse(falseLabel);

            // Emit the true operand
            _whenTrue.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(_whenTrue.ResultType, _resultType, ilg);

            // Jump to end
            ilg.EmitBranch(endLabel);

            ilg.MarkLabel(falseLabel);

            // Emit the false operand
            _whenFalse.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(_whenFalse.ResultType, _resultType, ilg);
            // Fall through to end
            ilg.MarkLabel(endLabel);
        }

        public override System.Type ResultType => _resultType;
    }
}
