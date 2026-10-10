using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal abstract class CustomBinder : Binder
    {

        public override System.Reflection.FieldInfo BindToField(System.Reflection.BindingFlags bindingAttr, System.Reflection.FieldInfo[] match, object value, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        // The VB original returned Nothing here. The C# conversion made this a separate method with
        // a ref parameter, and the derived binders forwarded the real override to a field that was
        // never assigned, so calling it threw NullReferenceException (R-036).
        public override System.Reflection.MethodBase BindToMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, ref object[] args, System.Reflection.ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] names, out object state)
        {
            state = null;
            return null;
        }

        public override object ChangeType(object value, System.Type type, System.Globalization.CultureInfo culture)
        {
            return null;
        }


        public override void ReorderArgumentArray(ref object[] args, object state)
        {
        }

        public override System.Reflection.PropertyInfo SelectProperty(System.Reflection.BindingFlags bindingAttr, System.Reflection.PropertyInfo[] match, System.Type returnType, System.Type[] indexes, System.Reflection.ParameterModifier[] modifiers)
        {
            return null;
        }
    }
}
