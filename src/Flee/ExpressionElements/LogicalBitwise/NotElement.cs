using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;


namespace Flee.ExpressionElements.LogicalBitwise
{
    internal class NotElement : UnaryElement
    {
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            if (object.ReferenceEquals(Child.ResultType, typeof(bool)))
            {
                this.EmitLogical(ilg, services);
            }
            else
            {
                Child.Emit(ilg, services);
                ilg.Emit(OpCodes.Not);
            }
        }

        private void EmitLogical(FleeILGenerator ilg, IServiceProvider services)
        {
            Child.Emit(ilg, services);
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ceq);
        }

        protected override System.Type GetResultType(System.Type childType)
        {
            if (object.ReferenceEquals(childType, typeof(bool)))
            {
                return typeof(bool);
            }
            else if (Utility.IsIntegralType(childType))
            {
                return childType;
            }
            else
            {
                return null;
            }
        }
    }
}
