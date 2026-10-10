#nullable enable
using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>Represents an imported namespace</summary>
    /// <remarks>This class acts as a container for other imports.  Use it when you want to logically group expression imports.</remarks>
    public sealed class NamespaceImport : ImportBase, ICollection<ImportBase>
    {
        private readonly string _namespace;
        private List<ImportBase> _imports;
        /// <summary>Creates a new namespace import with a given namespace name</summary>
        /// <param name="importNamespace">The name of the namespace to import</param>
        /// <exception cref="ArgumentNullException"><paramref name="importNamespace"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="importNamespace"/> is the empty string.</exception>
        public NamespaceImport(string importNamespace)
        {
            Utility.AssertNotNull(importNamespace, "importNamespace");
            if (importNamespace.Length == 0)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.InvalidNamespaceName);
                throw new ArgumentException(msg);
            }

            _namespace = importNamespace;
            _imports = new List<ImportBase>();
        }

        internal override void SetContext(ExpressionContext context)
        {
            base.SetContext(context);

            foreach (ImportBase import in _imports)
            {
                import.SetContext(context);
            }
        }

        internal override void Validate()
        {
        }

        internal override ImportBase Clone()
        {
            // Copy the child imports too: a cloned context must not add to, or set the context
            // of, the imports of the context it was cloned from (D-28).
            NamespaceImport copy = (NamespaceImport)base.Clone();
            copy._imports = new List<ImportBase>(_imports.Count);

            foreach (ImportBase import in _imports)
            {
                copy._imports.Add(import.Clone());
            }

            return copy;
        }

        /// <summary>Adds the matching members of the imports in this namespace that are not containers themselves.</summary>
        /// <param name="memberName">The name of the members to find.</param>
        /// <param name="memberType">The kinds of members to find.</param>
        /// <param name="dest">The collection that receives the members found.</param>
        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            foreach (ImportBase import in this.NonContainerImports)
            {
                AddImportMembers(import, memberName, memberType, dest);
            }
        }

        /// <summary>Adds nothing: a namespace does not list its members.</summary>
        /// <param name="memberType">Ignored.</param>
        /// <param name="dest">Left unchanged.</param>
        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
        }

        internal override Type? FindType(string typeName)
        {
            foreach (ImportBase import in this.NonContainerImports)
            {
                Type? t = import.FindType(typeName);

                if ((t != null))
                {
                    return t;
                }
            }

            return null;
        }

        internal override ImportBase? FindImport(string name)
        {
            foreach (ImportBase import in _imports)
            {
                if (import.IsMatch(name))
                {
                    return import;
                }
            }
            return null;
        }

        internal override bool IsMatch(string name)
        {
            // Only called while compiling, when the import belongs to a context.
            return string.Equals(_namespace, name, this.Context!.Options.MemberStringComparison);
        }

        private ICollection<ImportBase> NonContainerImports
        {
            get
            {
                List<ImportBase> found = new List<ImportBase>();

                foreach (ImportBase import in _imports)
                {
                    if (!import.IsContainer)
                    {
                        found.Add(import);
                    }
                }

                return found;
            }
        }

        /// <summary>Determines whether another import is a namespace import with the same name.</summary>
        /// <param name="import">The import to compare with.</param>
        /// <returns>
        /// True if <paramref name="import"/> is a namespace import with the same name; False otherwise.  Names are compared as the
        /// context's <see cref="ExpressionOptions.CaseSensitive"/> option says, or ignoring case while the import belongs to no context.
        /// </returns>
        protected override bool EqualsInternal(ImportBase? import)
        {
            NamespaceImport? otherSameType = import as NamespaceImport;
            // An import not yet attached to a context compares like Flee's default options,
            // case-insensitively; it used to throw NullReferenceException (R-041).
            StringComparison comparison = this.Context?.Options.MemberStringComparison ?? StringComparison.OrdinalIgnoreCase;
            return (otherSameType != null) && _namespace.Equals(otherSameType._namespace, comparison);
        }

        /// <summary>Determines if this import can contain other imports</summary>
        /// <value>Always <see langword="true"/>.</value>
        public override bool IsContainer => true;

        /// <summary>Gets the name of the import</summary>
        /// <value>The name of the namespace</value>
        public override string Name => _namespace;

        #region "ICollection implementation"
        /// <summary>Adds an import to this namespace.</summary>
        /// <param name="item">The import to add.</param>
        /// <remarks>
        /// If this namespace already belongs to a context, the added import is checked against that context at once.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// The imported type, or the type that declares the imported method, is not accessible to the context's expressions.
        /// </exception>
        public void Add(ImportBase item)
        {
            Utility.AssertNotNull(item, "item");

            if ((this.Context != null))
            {
                item.SetContext(this.Context);
            }

            _imports.Add(item);
        }

        /// <summary>Removes all imports from this namespace.</summary>
        public void Clear()
        {
            _imports.Clear();
        }

        /// <summary>Determines whether this namespace contains an import.</summary>
        /// <param name="item">The import to look for.</param>
        /// <returns>True if an equal import (see <see cref="ImportBase.Equals(ImportBase)"/>) is in this namespace; False otherwise.</returns>
        public bool Contains(ImportBase item)
        {
            return _imports.Contains(item);
        }

        /// <summary>Copies the imports of this namespace to an array.</summary>
        /// <param name="array">The array that receives the imports.</param>
        /// <param name="arrayIndex">The index in <paramref name="array"/> at which copying starts.</param>
        public void CopyTo(ImportBase[] array, int arrayIndex)
        {
            _imports.CopyTo(array, arrayIndex);
        }

        /// <summary>Removes an import from this namespace.</summary>
        /// <param name="item">The import to remove.</param>
        /// <returns>True if an equal import was found and removed; False otherwise.</returns>
        public bool Remove(ImportBase item)
        {
            return _imports.Remove(item);
        }

        /// <summary>Returns an enumerator over the imports in this namespace.</summary>
        /// <returns>An enumerator over the imports in this namespace.</returns>
        public override System.Collections.Generic.IEnumerator<ImportBase> GetEnumerator()
        {
            return _imports.GetEnumerator();
        }

        /// <summary>Gets the number of imports in this namespace.</summary>
        /// <value>The number of imports.</value>
        public int Count => _imports.Count;

        /// <summary>Gets a value indicating whether the namespace is read-only.</summary>
        /// <value>Always <see langword="false"/>.</value>
        public bool IsReadOnly => false;

        #endregion
    }
}
