using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel;
using Flee.CalcEngine.PublicTypes;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Base.Literals;
using Flee.ExpressionElements.Literals;
using Flee.ExpressionElements.Literals.Integral;
using Flee.ExpressionElements.Literals.Real;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;


namespace Flee.ExpressionElements.MemberElements
{
    /// <summary>
    /// Represents an identifier
    /// </summary>
    internal class IdentifierElement : MemberElement
    {
        private FieldInfo _field;
        private PropertyInfo _property;
        private PropertyDescriptor _propertyDescriptor;
        private Type _variableType;
        private Type _calcEngineReferenceType;
        public IdentifierElement(string name)
        {
            this.MemberName = name;
        }

        protected override void ResolveInternal()
        {
            // Try to bind to a field or property
            if (this.ResolveFieldProperty(Previous))
            {
                this.AddReferencedVariable(Previous);
                return;
            }

            // Try to find a variable with our name
            _variableType = Context.Variables.GetVariableTypeInternal(MemberName);

            // Variables are only usable as the first element
            if (Previous == null && (_variableType != null))
            {
                this.AddReferencedVariable(Previous);
                return;
            }

            CalculationEngine ce = Context.CalculationEngine;

            // Only names the engine knows are atoms; anything else falls through to the
            // UndefinedName compile error below (R-039).
            if (ce != null && ce.HasTail(MemberName))
            {
                ce.AddDependency(MemberName, Context);
                _calcEngineReferenceType = ce.ResolveTailType(MemberName);
                return;
            }

            if (Previous == null)
            {
                base.ThrowCompileException(CompileErrorResourceKeys.NoIdentifierWithName, CompileExceptionReason.UndefinedName, MemberName);
            }
            else
            {
                base.ThrowCompileException(CompileErrorResourceKeys.NoIdentifierWithNameOnType, CompileExceptionReason.UndefinedName, MemberName, Previous.TargetType.Name);
            }
        }

        private bool ResolveFieldProperty(MemberElement previous)
        {
            MemberInfo[] members = this.GetMembers(MemberTypes.Field | MemberTypes.Property);

            // Keep only the ones which are accessible
            members = this.GetAccessibleMembers(members);

            if (members.Length == 0)
            {
                // No accessible members; try to resolve a virtual property
                return this.ResolveVirtualProperty(previous);
            }
            else if (members.Length > 1)
            {
                // More than one accessible member
                if (previous == null)
                {
                    base.ThrowCompileException(CompileErrorResourceKeys.IdentifierIsAmbiguous, CompileExceptionReason.AmbiguousMatch, MemberName);
                }
                else
                {
                    base.ThrowCompileException(CompileErrorResourceKeys.IdentifierIsAmbiguousOnType, CompileExceptionReason.AmbiguousMatch, MemberName, previous.TargetType.Name);
                }
            }
            else
            {
                // Only one member; bind to it
                _field = members[0] as FieldInfo;
                if ((_field != null))
                {
                    return true;
                }

                // Assume it must be a property
                _property = (PropertyInfo)members[0];
                return true;
            }

            return false;
        }

        private bool ResolveVirtualProperty(MemberElement previous)
        {
            if (previous == null)
            {
                // We can't use virtual properties if we are the first element
                return false;
            }

            PropertyDescriptorCollection coll = TypeDescriptor.GetProperties(previous.ResultType);
            _propertyDescriptor = coll.Find(MemberName, true);
            return (_propertyDescriptor != null);
        }

        private void AddReferencedVariable(MemberElement previous)
        {
            if ((previous != null))
            {
                return;
            }

            if ((_variableType != null) || Options.IsOwnerType(this.MemberOwnerType))
            {
                ExpressionInfo info = (ExpressionInfo)Services.GetService(typeof(ExpressionInfo));
                info.AddReferencedVariable(MemberName);
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            base.Emit(ilg, services);

            this.EmitFirst(ilg);

            if ((_calcEngineReferenceType != null))
            {
                this.EmitReferenceLoad(ilg);
            }
            else if ((_variableType != null))
            {
                this.EmitVariableLoad(ilg);
            }
            else if ((_field != null))
            {
                this.EmitFieldLoad(_field, ilg, services);
            }
            else if ((_propertyDescriptor != null))
            {
                this.EmitVirtualPropertyLoad(ilg);
            }
            else
            {
                this.EmitPropertyLoad(_property, ilg);
            }
        }

        private void EmitReferenceLoad(FleeILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldarg_1);
            Context.CalculationEngine.EmitLoad(MemberName, ilg);

            // A member access on a value-type result needs its address, as for variables
            // (EmitMethodCall does this there). Missing here, it crashed (upstream #64, #111; R-026).
            if (this.ResultType.IsValueType && this.NextRequiresAddress)
            {
                EmitValueTypeLoadAddress(ilg, this.ResultType);
            }
        }

        private void EmitFirst(FleeILGenerator ilg)
        {
            if ((Previous != null))
            {
                return;
            }

            bool isVariable = (_variableType != null);

            if (isVariable)
            {
                // Load variables
                EmitLoadVariables(ilg);
            }
            else if (Options.IsOwnerType(this.MemberOwnerType) && !this.IsStatic)
            {
                this.EmitLoadOwner(ilg);
            }
        }

        private void EmitVariableLoad(FleeILGenerator ilg)
        {
            MethodInfo mi = VariableCollection.GetVariableLoadMethod(_variableType);
            ilg.Emit(OpCodes.Ldstr, MemberName);
            this.EmitMethodCall(mi, ilg);
        }

        private void EmitFieldLoad(System.Reflection.FieldInfo fi, FleeILGenerator ilg, IServiceProvider services)
        {
            if (fi.IsLiteral)
            {
                EmitLiteral(fi, ilg, services);
            }
            else if (this.ResultType.IsValueType && this.NextRequiresAddress)
            {
                EmitLdfld(fi, true, ilg);
            }
            else
            {
                EmitLdfld(fi, false, ilg);
            }
        }

        private static void EmitLdfld(System.Reflection.FieldInfo fi, bool indirect, FleeILGenerator ilg)
        {
            if (fi.IsStatic)
            {
                if (indirect)
                {
                    ilg.Emit(OpCodes.Ldsflda, fi);
                }
                else
                {
                    ilg.Emit(OpCodes.Ldsfld, fi);
                }
            }
            else
            {
                if (indirect)
                {
                    ilg.Emit(OpCodes.Ldflda, fi);
                }
                else
                {
                    ilg.Emit(OpCodes.Ldfld, fi);
                }
            }
        }

        /// <summary>
        /// Emit the load of a constant field.  We can't emit a ldsfld/ldfld of a constant so we have to get its value
        /// and then emit a ldc.
        /// </summary>
        /// <param name="fi"></param>
        /// <param name="ilg"></param>
        /// <param name="services"></param>
        private static void EmitLiteral(System.Reflection.FieldInfo fi, FleeILGenerator ilg, IServiceProvider services)
        {
            object value = fi.GetValue(null);
            Type t = value.GetType();
            TypeCode code = Type.GetTypeCode(t);
            LiteralElement elem = default(LiteralElement);

            switch (code)
            {
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    elem = new Int32LiteralElement(System.Convert.ToInt32(value));
                    break;
                case TypeCode.UInt32:
                    elem = new UInt32LiteralElement((UInt32)value);
                    break;
                case TypeCode.Int64:
                    elem = new Int64LiteralElement((Int64)value);
                    break;
                case TypeCode.UInt64:
                    elem = new UInt64LiteralElement((UInt64)value);
                    break;
                case TypeCode.Double:
                    elem = new DoubleLiteralElement((double)value);
                    break;
                case TypeCode.Single:
                    elem = new SingleLiteralElement((float)value);
                    break;
                case TypeCode.Boolean:
                    elem = new BooleanLiteralElement((bool)value);
                    break;
                case TypeCode.String:
                    elem = new StringLiteralElement((string)value);
                    break;
                default:
                    throw new NotSupportedException($"Constant of type {fi.FieldType} is not supported");
            }

            elem.Emit(ilg, services);
        }

        private void EmitPropertyLoad(System.Reflection.PropertyInfo pi, FleeILGenerator ilg)
        {
            System.Reflection.MethodInfo getter = pi.GetGetMethod(true);
            base.EmitMethodCall(getter, ilg);
        }

        /// <summary>
        /// Load a PropertyDescriptor based property
        /// </summary>
        /// <param name="ilg"></param>
        private void EmitVirtualPropertyLoad(FleeILGenerator ilg)
        {
            // The previous value is already on the top of the stack but we need it at the bottom

            // Get a temporary local index
            int index = ilg.GetTempLocalIndex(Previous.ResultType);

            // Store the previous value there
            Utility.EmitStoreLocal(ilg, index);

            // Load the variable collection
            EmitLoadVariables(ilg);
            // Load the property name
            ilg.Emit(OpCodes.Ldstr, MemberName);

            // Load the previous value and convert it to object
            Utility.EmitLoadLocal(ilg, index);
            ImplicitConverter.EmitImplicitConvert(Previous.ResultType, typeof(object), ilg);

            // Call the method to get the actual value
            MethodInfo mi = VariableCollection.GetVirtualPropertyLoadMethod(this.ResultType);
            this.EmitMethodCall(mi, ilg);
        }

        private Type MemberOwnerType
        {
            get
            {
                if ((_field != null))
                {
                    return _field.ReflectedType;
                }
                else if ((_propertyDescriptor != null))
                {
                    return _propertyDescriptor.ComponentType;
                }
                else if ((_property != null))
                {
                    return _property.ReflectedType;
                }
                else
                {
                    return null;
                }
            }
        }

        public override System.Type ResultType
        {
            get
            {
                if ((_calcEngineReferenceType != null))
                {
                    return _calcEngineReferenceType;
                }
                else if ((_variableType != null))
                {
                    return _variableType;
                }
                else if ((_propertyDescriptor != null))
                {
                    return _propertyDescriptor.PropertyType;
                }
                else if ((_field != null))
                {
                    return _field.FieldType;
                }
                else
                {
                    MethodInfo mi = _property.GetGetMethod(true);
                    return mi.ReturnType;
                }
            }
        }

        protected override bool RequiresAddress => _propertyDescriptor == null;

        protected override bool IsPublic
        {
            get
            {
                if ((_variableType != null) || (_calcEngineReferenceType != null))
                {
                    return true;
                }
                else if ((_variableType != null))
                {
                    return true;
                }
                else if ((_propertyDescriptor != null))
                {
                    return true;
                }
                else if ((_field != null))
                {
                    return _field.IsPublic;
                }
                else
                {
                    MethodInfo mi = _property.GetGetMethod(true);
                    return mi.IsPublic;
                }
            }
        }

        protected override bool SupportsStatic
        {
            get
            {
                if ((_variableType != null))
                {
                    // Variables never support static
                    return false;
                }
                else if ((_propertyDescriptor != null))
                {
                    // Neither do virtual properties
                    return false;
                }
                else if (Options.IsOwnerType(this.MemberOwnerType) && Previous == null)
                {
                    // Owner members support static if we are the first element
                    return true;
                }
                else
                {
                    // Support static if we are the first (ie: we are a static import)
                    return Previous == null;
                }
            }
        }

        protected override bool SupportsInstance
        {
            get
            {
                if ((_variableType != null))
                {
                    // Variables always support instance
                    return true;
                }
                else if ((_propertyDescriptor != null))
                {
                    // So do virtual properties
                    return true;
                }
                else if (Options.IsOwnerType(this.MemberOwnerType) && Previous == null)
                {
                    // Owner members support instance if we are the first element
                    return true;
                }
                else
                {
                    // We always support instance if we are not the first element
                    return (Previous != null);
                }
            }
        }

        public override bool IsStatic
        {
            get
            {
                if ((_variableType != null) || (_calcEngineReferenceType != null))
                {
                    return false;
                }
                else if ((_variableType != null))
                {
                    return false;
                }
                else if ((_field != null))
                {
                    return _field.IsStatic;
                }
                else if ((_propertyDescriptor != null))
                {
                    return false;
                }
                else
                {
                    MethodInfo mi = _property.GetGetMethod(true);
                    return mi.IsStatic;
                }
            }
        }
        public override bool IsExtensionMethod => false;
    }
}
