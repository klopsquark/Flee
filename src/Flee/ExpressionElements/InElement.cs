using System.Collections;
using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements.Base;

using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;


namespace Flee.ExpressionElements
{
    internal class InElement : ExpressionElement
    {
        // Element we will search for
        private ExpressionElement _operand;
        // Elements we will compare against
        private List<ExpressionElement> _arguments;
        // Collection to look in
        private ExpressionElement _targetCollectionElement;
        // Type of the collection

        private Type _targetCollectionType;
        // Initialize for searching a list of values
        public InElement(ExpressionElement operand, IList listElements)
        {
            _operand = operand;

            ExpressionElement[] arr = new ExpressionElement[listElements.Count];
            listElements.CopyTo(arr, 0);

            _arguments = new List<ExpressionElement>(arr);
            this.ResolveForListSearch();
        }

        // Initialize for searching a collection
        public InElement(ExpressionElement operand, ExpressionElement targetCollection)
        {
            _operand = operand;
            _targetCollectionElement = targetCollection;
            this.ResolveForCollectionSearch();
        }

        private void ResolveForListSearch()
        {
            CompareElement ce = new CompareElement();

            // Validate that our operand is comparable to all elements in the list
            foreach (ExpressionElement argumentElement in _arguments)
            {
                ce.Initialize(_operand, argumentElement, LogicalCompareOperation.Equal);
                ce.Validate();
            }
        }

        private void ResolveForCollectionSearch()
        {
            // Try to find a collection type
            _targetCollectionType = this.GetTargetCollectionType();

            if (_targetCollectionType == null)
            {
                base.ThrowCompileException(CompileErrorResourceKeys.SearchArgIsNotKnownCollectionType, CompileExceptionReason.TypeMismatch, _targetCollectionElement.ResultType.Name);
            }

            // Validate that the operand type is compatible with the collection
            MethodInfo mi = this.GetCollectionContainsMethod();
            ParameterInfo p1 = mi.GetParameters()[0];

            if (!ImplicitConverter.EmitImplicitConvert(_operand.ResultType, p1.ParameterType, null))
            {
                base.ThrowCompileException(CompileErrorResourceKeys.OperandNotConvertibleToCollectionType, CompileExceptionReason.TypeMismatch, _operand.ResultType.Name, p1.ParameterType.Name);
            }
        }

        private Type GetTargetCollectionType()
        {
            Type collType = _targetCollectionElement.ResultType;

            // Try to see if the collection is a generic ICollection or IDictionary
            Type[] interfaces = collType.GetInterfaces();

            foreach (Type interfaceType in interfaces)
            {
                if (!interfaceType.IsGenericType)
                {
                    continue;
                }

                Type genericTypeDef = interfaceType.GetGenericTypeDefinition();

                if (object.ReferenceEquals(genericTypeDef, typeof(ICollection<>)) || object.ReferenceEquals(genericTypeDef, typeof(IDictionary<,>)))
                {
                    return interfaceType;
                }
            }

            // Try to see if it is a regular IList or IDictionary. These are the non-generic
            // interfaces: the open generic types IList<> and IDictionary<,> never match here (R-022).
            if (typeof(System.Collections.IList).IsAssignableFrom(collType))
            {
                return typeof(System.Collections.IList);
            }
            else if (typeof(System.Collections.IDictionary).IsAssignableFrom(collType))
            {
                return typeof(System.Collections.IDictionary);
            }

            // Not a known collection type
            return null;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            if ((_targetCollectionType != null))
            {
                this.EmitCollectionIn(ilg, services);
            }
            else
            {
                // Do the real emit
                this.EmitListIn(ilg, services);
            }
        }

        private void EmitCollectionIn(FleeILGenerator ilg, IServiceProvider services)
        {
            // Get the contains method
            MethodInfo mi = this.GetCollectionContainsMethod();
            ParameterInfo p1 = mi.GetParameters()[0];

            // Load the collection
            _targetCollectionElement.Emit(ilg, services);
            // Load the argument
            _operand.Emit(ilg, services);
            // Do an implicit convert if necessary
            ImplicitConverter.EmitImplicitConvert(_operand.ResultType, p1.ParameterType, ilg);
            // Call the contains method
            ilg.Emit(OpCodes.Callvirt, mi);
        }

        private MethodInfo GetCollectionContainsMethod()
        {
            string methodName = "Contains";

            if (_targetCollectionType.IsGenericType && object.ReferenceEquals(_targetCollectionType.GetGenericTypeDefinition(), typeof(IDictionary<,>)))
            {
                methodName = "ContainsKey";
            }

            return _targetCollectionType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }

        private void EmitListIn(FleeILGenerator ilg, IServiceProvider services)
        {
            CompareElement ce = new CompareElement();
            Label endLabel = ilg.DefineLabel();
            Label trueTerminal = ilg.DefineLabel();

            // Cache the operand since we will be comparing against it a lot
            LocalBuilder lb = ilg.DeclareLocal(_operand.ResultType);
            int targetIndex = lb.LocalIndex;

            _operand.Emit(ilg, services);
            Utility.EmitStoreLocal(ilg, targetIndex);

            // Wrap our operand in a local shim
            LocalBasedElement targetShim = new LocalBasedElement(_operand, targetIndex);

            // Emit the compares
            foreach (ExpressionElement argumentElement in _arguments)
            {
                ce.Initialize(targetShim, argumentElement, LogicalCompareOperation.Equal);
                ce.Emit(ilg, services);

                EmitBranchToTrueTerminal(ilg, trueTerminal);
            }

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Br_S, endLabel);

            ilg.MarkLabel(trueTerminal);

            ilg.Emit(OpCodes.Ldc_I4_1);

            ilg.MarkLabel(endLabel);
        }

        private static void EmitBranchToTrueTerminal(FleeILGenerator ilg, Label trueTerminal)
        {
            ilg.EmitBranchTrue(trueTerminal);
        }

        public override System.Type ResultType => typeof(bool);
    }
}
