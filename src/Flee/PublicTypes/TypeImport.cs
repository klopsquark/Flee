#nullable enable
using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>Represents an imported type</summary>
    /// <remarks>
    /// Use this class when you want to make the members of a type available to an expression.  Its public and static
    /// members are imported.
    /// </remarks>
    public sealed class TypeImport : ImportBase
    {
        private readonly Type _type;
        private readonly BindingFlags _bindFlags;
        private readonly bool _useTypeNameAsNamespace;
        /// <summary>Creates a new import with a given type</summary>
        /// <param name="importType">The type to import</param>
        /// <exception cref="ArgumentNullException"><paramref name="importType"/> is <see langword="null"/>.</exception>
        public TypeImport(Type importType) : this(importType, false)
        {
        }

        /// <summary>Creates a new import with a given type</summary>
        /// <param name="importType">The type to import</param>
        /// <param name="useTypeNameAsNamespace">True to use the type's name as a namespace; False otherwise</param>
        /// <remarks>
        /// When useTypeNameAsNamespace is set to True, the type will act as a namespace in an expression.  For example: If
        /// you import the DayOfWeek enum and set the flag to true, you can reference it as DayOfWeek.Sunday in an expression.  When the flag is false,
        /// you would reference it as simply Sunday.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="importType"/> is <see langword="null"/>.</exception>
        public TypeImport(Type importType, bool useTypeNameAsNamespace) : this(importType, BindingFlags.Public | BindingFlags.Static, useTypeNameAsNamespace)
        {
        }

        #region "Methods - Non Public"
        internal TypeImport(Type t, BindingFlags flags, bool useTypeNameAsNamespace)
        {
            Utility.AssertNotNull(t, "t");
            _type = t;
            _bindFlags = flags;
            _useTypeNameAsNamespace = useTypeNameAsNamespace;
        }

        internal override void Validate()
        {
            // Validate runs from SetContext, after the context is set.
            this.Context!.AssertTypeIsAccessible(_type);
        }

        /// <inheritdoc/>
        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            // Only called while compiling, when the import belongs to a context.
            MemberInfo[] members = _type.FindMembers(memberType, _bindFlags, this.Context!.Options.MemberFilter, memberName);
            ImportBase.AddMemberRange(members, dest);
        }

        /// <summary>
        /// Adds all members of the imported type that have the given member type to a collection; adds nothing when the
        /// type's name is used as a namespace.
        /// </summary>
        /// <param name="memberType">The kinds of members to find.</param>
        /// <param name="dest">The collection that receives the members found.</param>
        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if (!_useTypeNameAsNamespace)
            {
                MemberInfo[] members = _type.FindMembers(memberType, _bindFlags, this.AlwaysMemberFilter, null);
                ImportBase.AddMemberRange(members, dest);
            }
        }

        internal override bool IsMatch(string name)
        {
            if (_useTypeNameAsNamespace)
            {
                // Only called while compiling, when the import belongs to a context.
                return string.Equals(_type.Name, name, this.Context!.Options.MemberStringComparison);
            }
            else
            {
                return false;
            }
        }

        internal override Type? FindType(string typeName)
        {
            // Only called while compiling, when the import belongs to a context.
            if (string.Equals(typeName, _type.Name, this.Context!.Options.MemberStringComparison))
            {
                return _type;
            }
            else
            {
                return null;
            }
        }

        /// <summary>Determines whether another import is a type import for the same type.</summary>
        /// <param name="import">The import to compare with.</param>
        /// <returns>True if <paramref name="import"/> imports the same type; False otherwise.</returns>
        protected override bool EqualsInternal(ImportBase? import)
        {
            TypeImport? otherSameType = import as TypeImport;
            return (otherSameType != null) && object.ReferenceEquals(_type, otherSameType._type);
        }
        #endregion

        #region "Methods - Public"
        /// <summary>Returns an enumerator over the imports this import contains.</summary>
        /// <returns>
        /// When the type's name is used as a namespace, an enumerator over one import of the type itself; otherwise an empty enumerator.
        /// </returns>
        public override IEnumerator<ImportBase> GetEnumerator()
        {
            if (_useTypeNameAsNamespace)
            {
                List<ImportBase> coll = new List<ImportBase>();
                coll.Add(new TypeImport(_type, false));
                return coll.GetEnumerator();
            }
            else
            {
                return base.GetEnumerator();
            }
        }
        #endregion

        #region "Properties - Public"
        /// <summary>Determines if this import can contain other imports</summary>
        /// <value>True if the type's name is used as a namespace; False otherwise</value>
        public override bool IsContainer => _useTypeNameAsNamespace;

        /// <summary>Gets the name of the import</summary>
        /// <value>The name of the imported type</value>
        public override string Name => _type.Name;

        /// <summary>Gets the type that this import represents</summary>
        /// <value>The type that this import represents</value>
        /// <remarks>Use this property to retrieve the imported type</remarks>
        public Type Target => _type;

        #endregion
    }
}
