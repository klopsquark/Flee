#nullable enable
using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
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
            // Validate runs from SetContext, after the context is set.
            this.Context!.AssertTypeIsAccessible(_type);
        }

        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            // Only called while compiling, when the import belongs to a context.
            MemberInfo[] members = _type.FindMembers(memberType, _bindFlags, this.Context!.Options.MemberFilter, memberName);
            ImportBase.AddMemberRange(members, dest);
        }

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

        protected override bool EqualsInternal(ImportBase? import)
        {
            TypeImport? otherSameType = import as TypeImport;
            return (otherSameType != null) && object.ReferenceEquals(_type, otherSameType._type);
        }
        #endregion

        #region "Methods - Public"
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
        public override bool IsContainer => _useTypeNameAsNamespace;

        public override string Name => _type.Name;

        public Type Target => _type;

        #endregion
    }
}
