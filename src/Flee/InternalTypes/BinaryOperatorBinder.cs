using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class BinaryOperatorBinder : CustomBinder
    {

        private readonly Type _leftType;
        private readonly Type _rightType;

        public BinaryOperatorBinder(Type leftType, Type rightType)
        {
            _leftType = leftType;
            _rightType = rightType;
        }

        public override System.Reflection.MethodBase SelectMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, System.Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            foreach (MethodInfo mi in match)
            {
                ParameterInfo[] parameters = mi.GetParameters();
                bool leftValid = ImplicitConverter.EmitImplicitConvert(_leftType, parameters[0].ParameterType, null);
                bool rightValid = ImplicitConverter.EmitImplicitConvert(_rightType, parameters[1].ParameterType, null);

                if (leftValid && rightValid)
                {
                    return mi;
                }
            }
            return null;
        }
    }
}
