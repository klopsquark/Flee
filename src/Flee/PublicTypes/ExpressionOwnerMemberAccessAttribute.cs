#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>
    /// Specifies whether access to a member on the expression owner is allowed.
    /// </summary>
    /// <remarks>
    /// Use this attribute to control the accessibility of individual members on the expression owner.  The access specified in
    /// this attribute overrides the access level specified using the <see cref="ExpressionOptions.OwnerMemberAccess"/> property.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ExpressionOwnerMemberAccessAttribute : Attribute
    {


        private readonly bool _allowAccess;
        /// <summary>
        /// Initializes the attribute with the desired access.
        /// </summary>
        /// <param name="allowAccess">True to allow the member to be used in an expression; False otherwise</param>
        /// <remarks>Initializes the attribute with the desired access.</remarks>
        public ExpressionOwnerMemberAccessAttribute(bool allowAccess)
        {
            _allowAccess = allowAccess;
        }

        internal bool AllowAccess => _allowAccess;
    }
}
