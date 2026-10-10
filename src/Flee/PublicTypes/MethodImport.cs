#nullable enable
using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>Represents an imported method</summary>
    /// <remarks>Use this class when you want to make a single method available to an expression</remarks>
    public sealed class MethodImport : ImportBase
    {

        private readonly MethodInfo _method;
        /// <summary>Creates a new method import with a given method</summary>
        /// <param name="importMethod">The method to import</param>
        /// <exception cref="ArgumentNullException"><paramref name="importMethod"/> is <see langword="null"/>.</exception>
        public MethodImport(MethodInfo importMethod)
        {
            Utility.AssertNotNull(importMethod, "importMethod");
            _method = importMethod;
        }

        internal override void Validate()
        {
            // Validate runs from SetContext, after the context is set. ReflectedType is null only for a
            // module-level (global) method, which C# cannot declare.
            this.Context!.AssertTypeIsAccessible(_method.ReflectedType!);
        }

        /// <inheritdoc/>
        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            // Only called while compiling, when the import belongs to a context.
            if (string.Equals(memberName, _method.Name, this.Context!.Options.MemberStringComparison) && (memberType & MemberTypes.Method) != 0)
            {
                dest.Add(_method);
            }
        }

        /// <inheritdoc/>
        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if ((memberType & MemberTypes.Method) != 0)
            {
                dest.Add(_method);
            }
        }

        internal override bool IsMatch(string name)
        {
            // Only called while compiling, when the import belongs to a context.
            return string.Equals(_method.Name, name, this.Context!.Options.MemberStringComparison);
        }

        internal override Type? FindType(string typeName)
        {
            return null;
        }

        /// <summary>Determines whether another import is a method import for the same method.</summary>
        /// <param name="import">The import to compare with.</param>
        /// <returns>True if <paramref name="import"/> imports the same method; False otherwise.</returns>
        protected override bool EqualsInternal(ImportBase? import)
        {
            MethodImport? otherSameType = import as MethodImport;
            return (otherSameType != null) && _method.MethodHandle.Equals(otherSameType._method.MethodHandle);
        }

        /// <summary>Gets the name of the import</summary>
        /// <value>The name of the imported method</value>
        public override string Name => _method.Name;

        /// <summary>Gets the method that this import represents</summary>
        /// <value>The method that this import represents</value>
        /// <remarks>Use this property to retrieve the imported method</remarks>
        public MethodInfo Target => _method;
    }
}
