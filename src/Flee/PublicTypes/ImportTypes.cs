using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public abstract class ImportBase : IEnumerable<ImportBase>, IEquatable<ImportBase>
    {
        private ExpressionContext _context;

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

        protected abstract void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest);
        protected abstract void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest);

        internal ImportBase Clone()
        {
            return (ImportBase)this.MemberwiseClone();
        }

        protected static void AddImportMembers(ImportBase import, string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            import.AddMembers(memberName, memberType, dest);
        }

        protected static void AddImportMembers(ImportBase import, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            import.AddMembers(memberType, dest);
        }

        protected static void AddMemberRange(ICollection<MemberInfo> members, ICollection<MemberInfo> dest)
        {
            foreach (MemberInfo mi in members)
            {
                dest.Add(mi);
            }
        }

        protected bool AlwaysMemberFilter(MemberInfo member, object criteria)
        {
            return true;
        }

        internal abstract bool IsMatch(string name);
        internal abstract Type FindType(string typename);

        internal virtual ImportBase FindImport(string name)
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
        public MemberInfo[] GetMembers(MemberTypes memberType)
        {
            List<MemberInfo> found = new List<MemberInfo>();
            this.AddMembers(memberType, found);
            return found.ToArray();
        }
        #endregion

        #region "IEnumerable Implementation"
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
        public bool Equals(ImportBase other)
        {
            return this.EqualsInternal(other);
        }

        protected abstract bool EqualsInternal(ImportBase import);
        #endregion

        #region "Properties - Protected"
        protected ExpressionContext Context => _context;

        #endregion

        #region "Properties - Public"
        public abstract string Name { get; }

        public virtual bool IsContainer => false;

        #endregion
    }

    public sealed class TypeImport : ImportBase
    {
        private readonly Type _type;
        private readonly BindingFlags _bindFlags;
        private readonly bool _useTypeNameAsNamespace;
        public TypeImport(Type importType) : this(importType, false)
        {
        }

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
            this.Context.AssertTypeIsAccessible(_type);
        }

        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            MemberInfo[] members = _type.FindMembers(memberType, _bindFlags, this.Context.Options.MemberFilter, memberName);
            ImportBase.AddMemberRange(members, dest);
        }

        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if (_useTypeNameAsNamespace == false)
            {
                MemberInfo[] members = _type.FindMembers(memberType, _bindFlags, this.AlwaysMemberFilter, null);
                ImportBase.AddMemberRange(members, dest);
            }
        }

        internal override bool IsMatch(string name)
        {
            if (_useTypeNameAsNamespace == true)
            {
                return string.Equals(_type.Name, name, this.Context.Options.MemberStringComparison);
            }
            else
            {
                return false;
            }
        }

        internal override Type FindType(string typeName)
        {
            if (string.Equals(typeName, _type.Name, this.Context.Options.MemberStringComparison) == true)
            {
                return _type;
            }
            else
            {
                return null;
            }
        }

        protected override bool EqualsInternal(ImportBase import)
        {
            TypeImport otherSameType = import as TypeImport;
            return (otherSameType != null) && object.ReferenceEquals(_type, otherSameType._type);
        }
        #endregion

        #region "Methods - Public"
        public override IEnumerator<ImportBase> GetEnumerator()
        {
            if (_useTypeNameAsNamespace == true)
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
        public override bool IsContainer => _useTypeNameAsNamespace;

        public override string Name => _type.Name;

        public Type Target => _type;

        #endregion
    }

    public sealed class MethodImport : ImportBase
    {

        private readonly MethodInfo _method;
        public MethodImport(MethodInfo importMethod)
        {
            Utility.AssertNotNull(importMethod, "importMethod");
            _method = importMethod;
        }

        internal override void Validate()
        {
            this.Context.AssertTypeIsAccessible(_method.ReflectedType);
        }

        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if (string.Equals(memberName, _method.Name, this.Context.Options.MemberStringComparison) == true && (memberType & MemberTypes.Method) != 0)
            {
                dest.Add(_method);
            }
        }

        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if ((memberType & MemberTypes.Method) != 0)
            {
                dest.Add(_method);
            }
        }

        internal override bool IsMatch(string name)
        {
            return string.Equals(_method.Name, name, this.Context.Options.MemberStringComparison);
        }

        internal override Type FindType(string typeName)
        {
            return null;
        }

        protected override bool EqualsInternal(ImportBase import)
        {
            MethodImport otherSameType = import as MethodImport;
            return (otherSameType != null) && _method.MethodHandle.Equals(otherSameType._method.MethodHandle);
        }

        public override string Name => _method.Name;

        public MethodInfo Target => _method;
    }

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

        internal override Type FindType(string typeName)
        {
            foreach (ImportBase import in this.NonContainerImports)
            {
                Type t = import.FindType(typeName);

                if ((t != null))
                {
                    return t;
                }
            }

            return null;
        }

        internal override ImportBase FindImport(string name)
        {
            foreach (ImportBase import in _imports)
            {
                if (import.IsMatch(name) == true)
                {
                    return import;
                }
            }
            return null;
        }

        internal override bool IsMatch(string name)
        {
            return string.Equals(_namespace, name, this.Context.Options.MemberStringComparison);
        }

        private ICollection<ImportBase> NonContainerImports
        {
            get
            {
                List<ImportBase> found = new List<ImportBase>();

                foreach (ImportBase import in _imports)
                {
                    if (import.IsContainer == false)
                    {
                        found.Add(import);
                    }
                }

                return found;
            }
        }

        protected override bool EqualsInternal(ImportBase import)
        {
            NamespaceImport otherSameType = import as NamespaceImport;
            return (otherSameType != null) && _namespace.Equals(otherSameType._namespace, this.Context.Options.MemberStringComparison);
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
