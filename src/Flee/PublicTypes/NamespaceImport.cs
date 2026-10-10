#nullable enable
using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public sealed class NamespaceImport : ImportBase, ICollection<ImportBase>
    {
        private readonly string _namespace;
        private readonly List<ImportBase> _imports;
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

        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            foreach (ImportBase import in this.NonContainerImports)
            {
                AddImportMembers(import, memberName, memberType, dest);
            }
        }

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

        protected override bool EqualsInternal(ImportBase? import)
        {
            NamespaceImport? otherSameType = import as NamespaceImport;
            // Assumes the import belongs to a context; on a detached NamespaceImport this throws
            // NullReferenceException, as before.
            return (otherSameType != null) && _namespace.Equals(otherSameType._namespace, this.Context!.Options.MemberStringComparison);
        }

        public override bool IsContainer => true;

        public override string Name => _namespace;

        #region "ICollection implementation"
        public void Add(ImportBase item)
        {
            Utility.AssertNotNull(item, "item");

            if ((this.Context != null))
            {
                item.SetContext(this.Context);
            }

            _imports.Add(item);
        }

        public void Clear()
        {
            _imports.Clear();
        }

        public bool Contains(ImportBase item)
        {
            return _imports.Contains(item);
        }

        public void CopyTo(ImportBase[] array, int arrayIndex)
        {
            _imports.CopyTo(array, arrayIndex);
        }

        public bool Remove(ImportBase item)
        {
            return _imports.Remove(item);
        }

        public override System.Collections.Generic.IEnumerator<ImportBase> GetEnumerator()
        {
            return _imports.GetEnumerator();
        }

        public int Count => _imports.Count;

        public bool IsReadOnly => false;

        #endregion
    }
}
