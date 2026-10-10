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
}
