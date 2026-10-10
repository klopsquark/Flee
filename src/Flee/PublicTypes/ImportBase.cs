#nullable enable
using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>Base class for all expression imports</summary>
    /// <remarks>
    /// Its constructor is internal: the imports are <see cref="TypeImport"/>, <see cref="MethodImport"/> and <see cref="NamespaceImport"/>.
    /// </remarks>
    public abstract class ImportBase : IEnumerable<ImportBase>, IEquatable<ImportBase>
    {
        private ExpressionContext? _context;

        internal ImportBase()
        {
        }

        #region "Methods - Non Public"
        internal virtual void SetContext(ExpressionContext context)
        {
            _context = context;
            this.Validate();
        }

        internal abstract void Validate();

        /// <summary>Adds the members of this import that have the given name and member type to a collection.</summary>
        /// <param name="memberName">The name of the members to find.</param>
        /// <param name="memberType">The kinds of members to find.</param>
        /// <param name="dest">The collection that receives the members found.</param>
        protected abstract void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest);
        /// <summary>Adds all members of this import that have the given member type to a collection.</summary>
        /// <param name="memberType">The kinds of members to find.</param>
        /// <param name="dest">The collection that receives the members found.</param>
        protected abstract void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest);

        internal virtual ImportBase Clone()
        {
            return (ImportBase)this.MemberwiseClone();
        }

        /// <summary>Adds the members of another import that have the given name and member type to a collection.</summary>
        /// <param name="import">The import whose members to add.</param>
        /// <param name="memberName">The name of the members to find.</param>
        /// <param name="memberType">The kinds of members to find.</param>
        /// <param name="dest">The collection that receives the members found.</param>
        protected static void AddImportMembers(ImportBase import, string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            import.AddMembers(memberName, memberType, dest);
        }

        /// <summary>Adds all members of another import that have the given member type to a collection.</summary>
        /// <param name="import">The import whose members to add.</param>
        /// <param name="memberType">The kinds of members to find.</param>
        /// <param name="dest">The collection that receives the members found.</param>
        protected static void AddImportMembers(ImportBase import, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            import.AddMembers(memberType, dest);
        }

        /// <summary>Adds every member of one collection to another.</summary>
        /// <param name="members">The members to add.</param>
        /// <param name="dest">The collection that receives the members.</param>
        protected static void AddMemberRange(ICollection<MemberInfo> members, ICollection<MemberInfo> dest)
        {
            foreach (MemberInfo mi in members)
            {
                dest.Add(mi);
            }
        }

        /// <summary>A <see cref="MemberFilter"/> that accepts every member.</summary>
        /// <param name="member">The member to test.</param>
        /// <param name="criteria">Ignored.</param>
        /// <returns>Always <see langword="true"/>.</returns>
        protected bool AlwaysMemberFilter(MemberInfo member, object? criteria)
        {
            return true;
        }

        internal abstract bool IsMatch(string name);
        internal abstract Type? FindType(string typename);

        internal virtual ImportBase? FindImport(string name)
        {
            return null;
        }

        internal MemberInfo[] FindMembers(string memberName, MemberTypes memberType)
        {
            List<MemberInfo> found = new List<MemberInfo>();
            this.AddMembers(memberName, memberType, found);
            return found.ToArray();
        }
        #endregion

        #region "Methods - Public"
        /// <summary>Gets the members of this import that have the given member type.</summary>
        /// <param name="memberType">The kinds of members to get.</param>
        /// <returns>
        /// The members found.  A <see cref="NamespaceImport"/> returns none, and a <see cref="TypeImport"/> that uses its type name
        /// as a namespace returns none.
        /// </returns>
        public MemberInfo[] GetMembers(MemberTypes memberType)
        {
            List<MemberInfo> found = new List<MemberInfo>();
            this.AddMembers(memberType, found);
            return found.ToArray();
        }
        #endregion

        #region "IEnumerable Implementation"
        /// <summary>Returns an enumerator over the imports this import contains.</summary>
        /// <returns>An enumerator over the contained imports; empty unless this import is a container.</returns>
        public virtual System.Collections.Generic.IEnumerator<ImportBase> GetEnumerator()
        {
            List<ImportBase> coll = new List<ImportBase>();
            return coll.GetEnumerator();
        }

        private System.Collections.IEnumerator GetEnumerator1()
        {
            return this.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator1();
        }
        #endregion

        #region "IEquatable Implementation"
        /// <summary>Determines whether this import and another one import the same thing.</summary>
        /// <param name="other">The import to compare with.</param>
        /// <returns>
        /// True if both are imports of the same kind for the same type, method or namespace name; False otherwise.
        /// </returns>
        public bool Equals(ImportBase? other)
        {
            return this.EqualsInternal(other);
        }

        /// <summary>Implements <see cref="Equals(ImportBase)"/> for a kind of import.</summary>
        /// <param name="import">The import to compare with.</param>
        /// <returns>True if the imports are equal; False otherwise.</returns>
        protected abstract bool EqualsInternal(ImportBase? import);
        #endregion

        #region "Properties - Protected"
        // Null until the import is added to an ExpressionContext's imports.
        /// <summary>Gets the context this import belongs to.</summary>
        /// <value>The context, or <see langword="null"/> until the import is added to a context's imports.</value>
        protected ExpressionContext? Context => _context;

        #endregion

        #region "Properties - Public"
        /// <summary>Gets the name of the import</summary>
        /// <value>The name of the current import instance</value>
        /// <remarks>Use this property to get the name of the import</remarks>
        public abstract string Name { get; }

        /// <summary>Determines if this import can contain other imports</summary>
        /// <value>True if this import can contain other imports; False otherwise</value>
        /// <remarks>Use this property to determine if this import contains other imports</remarks>
        public virtual bool IsContainer => false;

        #endregion
    }
}
