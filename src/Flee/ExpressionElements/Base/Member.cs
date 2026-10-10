using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    internal abstract class MemberElement : ExpressionElement
    {
        public string MemberName { get; protected set; }
        protected MemberElement Previous;
        protected MemberElement Next;
        protected IServiceProvider Services;
        protected ExpressionOptions Options;
        protected ExpressionContext Context;
        protected ImportBase Import;

        public const BindingFlags BindFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        protected MemberElement()
        {
        }

        public void Link(MemberElement nextElement)
        {
            Next = nextElement;
            if ((nextElement != null))
            {
                nextElement.Previous = this;
            }
        }

        public void Resolve(IServiceProvider services)
        {
            Services = services;
            Options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
            Context = (ExpressionContext)services.GetService(typeof(ExpressionContext));
            this.ResolveInternal();
            this.Validate();
        }

        public void SetImport(ImportBase import)
        {
            Import = import;
        }

        protected abstract void ResolveInternal();
        public abstract bool IsStatic { get; }
        public abstract bool IsExtensionMethod { get; }
        protected abstract bool IsPublic { get; }

        protected virtual void Validate()
        {
            if (Previous == null)
            {
                return;
            }

            if (this.IsStatic && !this.SupportsStatic && !IsExtensionMethod)
            {
                base.ThrowCompileException(CompileErrorResourceKeys.StaticMemberCannotBeAccessedWithInstanceReference, CompileExceptionReason.TypeMismatch, MemberName);
            }
            else if (!this.IsStatic && !this.SupportsInstance)
            {
                base.ThrowCompileException(CompileErrorResourceKeys.ReferenceToNonSharedMemberRequiresObjectReference, CompileExceptionReason.TypeMismatch, MemberName);
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            if ((Previous != null))
            {
                Previous.Emit(ilg, services);
            }
        }

        protected static void EmitLoadVariables(FleeILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldarg_2);
        }

        /// <summary>
        /// Handles a call emit for static, instance methods of reference/value types
        /// </summary>
        /// <param name="mi"></param>
        /// <param name="ilg"></param>
        protected void EmitMethodCall(MethodInfo mi, FleeILGenerator ilg)
        {
            EmitMethodCall(this.ResultType, this.NextRequiresAddress, mi, ilg);
        }

        protected static void EmitMethodCall(Type resultType, bool nextRequiresAddress, MethodInfo mi, FleeILGenerator ilg)
        {
            // The type the method is called on, as in the original VB code; mi.GetType() was the
            // MethodInfo's own type and never a value type (R-024).
            if (!mi.ReflectedType.IsValueType)
            {
                EmitReferenceTypeMethodCall(mi, ilg);
            }
            else
            {
                EmitValueTypeMethodCall(mi, ilg);
            }

            if (resultType.IsValueType && nextRequiresAddress)
            {
                EmitValueTypeLoadAddress(ilg, resultType);
            }
        }

        protected static bool IsGetTypeMethod(MethodInfo mi)
        {
            MethodInfo miGetType = typeof(object).GetMethod("gettype", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            return mi.MethodHandle.Equals(miGetType.MethodHandle);
        }

        /// <summary>
        /// Emit a function call for a value type
        /// </summary>
        /// <param name="mi"></param>
        /// <param name="ilg"></param>
        private static void EmitValueTypeMethodCall(MethodInfo mi, FleeILGenerator ilg)
        {
            if (mi.IsStatic)
            {
                ilg.Emit(OpCodes.Call, mi);
            }
            else if ((!object.ReferenceEquals(mi.DeclaringType, mi.ReflectedType)))
            {
                // Method is not defined on the value type

                if (IsGetTypeMethod(mi))
                {
                    // Special GetType method which requires a box
                    ilg.Emit(OpCodes.Box, mi.ReflectedType);
                    ilg.Emit(OpCodes.Call, mi);
                }
                else
                {
                    // Equals, GetHashCode, and ToString methods on the base
                    ilg.Emit(OpCodes.Constrained, mi.ReflectedType);
                    ilg.Emit(OpCodes.Callvirt, mi);
                }
            }
            else
            {
                // Call value type's implementation
                ilg.Emit(OpCodes.Call, mi);
            }
        }

        private static void EmitReferenceTypeMethodCall(MethodInfo mi, FleeILGenerator ilg)
        {
            if (mi.IsStatic)
            {
                ilg.Emit(OpCodes.Call, mi);
            }
            else
            {
                ilg.Emit(OpCodes.Callvirt, mi);
            }
        }

        protected static void EmitValueTypeLoadAddress(FleeILGenerator ilg, Type targetType)
        {
            int index = ilg.GetTempLocalIndex(targetType);
            Utility.EmitStoreLocal(ilg, index);
            ilg.Emit(OpCodes.Ldloca_S, Convert.ToByte(index));
        }

        protected void EmitLoadOwner(FleeILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldarg_0);

            Type ownerType = Options.OwnerType;

            if (!ownerType.IsValueType)
            {
                return;
            }

            ilg.Emit(OpCodes.Unbox, ownerType);
            ilg.Emit(OpCodes.Ldobj, ownerType);

            // Emit usual stuff for value types but use the owner type as the target
            if (this.RequiresAddress)
            {
                EmitValueTypeLoadAddress(ilg, ownerType);
            }
        }

        /// <summary>
        /// Determine if a field, property, or method is public
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private static bool IsMemberPublic(MemberInfo member)
        {
            FieldInfo fi = member as FieldInfo;

            if ((fi != null))
            {
                return fi.IsPublic;
            }

            PropertyInfo pi = member as PropertyInfo;

            if ((pi != null))
            {
                MethodInfo pmi = pi.GetGetMethod(true);
                return pmi.IsPublic;
            }

            MethodInfo mi = member as MethodInfo;

            if ((mi != null))
            {
                return mi.IsPublic;
            }

            Debug.Assert(false, "unknown member type");
            return false;
        }

        protected MemberInfo[] GetAccessibleMembers(MemberInfo[] members)
        {
            List<MemberInfo> accessible = new List<MemberInfo>();

            // Keep all members that are accessible
            foreach (MemberInfo mi in members)
            {
                if (this.IsMemberAccessible(mi))
                {
                    accessible.Add(mi);
                }
            }

            return accessible.ToArray();
        }

        protected static bool IsOwnerMemberAccessible(MemberInfo member, ExpressionOptions options)
        {
            bool accessAllowed = false;

            // Get the allowed access defined in the options
            if (IsMemberPublic(member))
            {
                accessAllowed = (options.OwnerMemberAccess & BindingFlags.Public) != 0;
            }
            else
            {
                accessAllowed = (options.OwnerMemberAccess & BindingFlags.NonPublic) != 0;
            }

            // See if the member has our access attribute defined
            ExpressionOwnerMemberAccessAttribute attr = (ExpressionOwnerMemberAccessAttribute)Attribute.GetCustomAttribute(member, typeof(ExpressionOwnerMemberAccessAttribute));

            if (attr == null)
            {
                // No, so return the access level
                return accessAllowed;
            }
            else
            {
                // Member has our access attribute defined; use its access value instead
                return attr.AllowAccess;
            }
        }

        public bool IsMemberAccessible(MemberInfo member)
        {
            if (Options.IsOwnerType(member.ReflectedType))
            {
                return IsOwnerMemberAccessible(member, Options);
            }
            else
            {
                return IsMemberPublic(member);
            }
        }

        protected MemberInfo[] GetMembers(MemberTypes targets)
        {
            if (Previous == null)
            {
                // Do we have a namespace?
                if (Import == null)
                {
                    // Get all members in the default namespace
                    return this.GetDefaultNamespaceMembers(MemberName, targets);
                }
                else
                {
                    return Import.FindMembers(MemberName, targets);
                }
            }
            else
            {
                // We are not the first element; find all members with our name on the type of the previous member
                // We are not the first element; find all members with our name on the type of the previous member
                var foundMembers = Previous.TargetType.FindMembers(targets, BindFlags, Options.MemberFilter, MemberName);
                var importedMembers = Context.Imports.RootImport.FindMembers(MemberName, targets);
                if (foundMembers.Length == 0) //If no members found search in root import
                    return importedMembers;

                MemberInfo[] allMembers = new MemberInfo[foundMembers.Length + importedMembers.Length];
                foundMembers.CopyTo(allMembers, 0);
                importedMembers.CopyTo(allMembers, foundMembers.Length);
                return allMembers;
            }
        }

        /// <summary>
        /// Find members in the default namespace
        /// </summary>
        /// <param name="name"></param>
        /// <param name="memberType"></param>
        /// <returns></returns>
        protected MemberInfo[] GetDefaultNamespaceMembers(string name, MemberTypes memberType)
        {
            // Search the owner first
            MemberInfo[] members = Context.Imports.FindOwnerMembers(name, memberType);

            // Keep only the accessible members
            members = this.GetAccessibleMembers(members);

            //Also search imports
            var importedMembers = Context.Imports.RootImport.FindMembers(name, memberType);

            //if no members, just return imports
            if (members.Length == 0)
                return importedMembers;

            //combine members and imports
            MemberInfo[] allMembers = new MemberInfo[members.Length + importedMembers.Length];
            members.CopyTo(allMembers, 0);
            importedMembers.CopyTo(allMembers, members.Length);
            return allMembers;
        }

        protected static bool IsElementPublic(MemberElement e)
        {
            return e.IsPublic;
        }

        protected bool NextRequiresAddress => Next != null && Next.RequiresAddress;

        protected virtual bool RequiresAddress => false;

        protected virtual bool SupportsInstance => true;

        protected virtual bool SupportsStatic => false;

        public System.Type TargetType => this.ResultType;
    }
}
