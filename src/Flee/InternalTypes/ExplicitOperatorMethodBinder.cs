using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class ExplicitOperatorMethodBinder : CustomBinder
    {
        private readonly Type _returnType;
        private readonly Type _argType;

        public ExplicitOperatorMethodBinder(Type returnType, Type argType)
        {
            _returnType = returnType;
            _argType = argType;
        }

        public override System.Reflection.MethodBase SelectMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, System.Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            foreach (MethodInfo mi in match)
            {
                ParameterInfo[] parameters = mi.GetParameters();
                ParameterInfo firstParameter = parameters[0];
                if (object.ReferenceEquals(firstParameter.ParameterType, _argType) && object.ReferenceEquals(mi.ReturnType, _returnType))
                {
                    return mi;
                }
            }
            return null;
        }
    }
}
