namespace Flee.PublicTypes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ExpressionOwnerMemberAccessAttribute : Attribute
    {


        private readonly bool _allowAccess;
        public ExpressionOwnerMemberAccessAttribute(bool allowAccess)
        {
            _allowAccess = allowAccess;
        }

        internal bool AllowAccess => _allowAccess;
    }
}
