using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
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
            if (string.Equals(memberName, _method.Name, this.Context.Options.MemberStringComparison) && (memberType & MemberTypes.Method) != 0)
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
}
