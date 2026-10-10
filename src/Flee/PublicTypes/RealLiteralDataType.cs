#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>
    /// Defines values to indicate the data type to use for storing real literals.
    /// </summary>
    public enum RealLiteralDataType
    {
        /// <summary>Specifies that real literals will be stored using the <see cref="System.Single"/> data type.</summary>
        Single,
        /// <summary>
        /// Specifies that real literals will be stored using the <see cref="System.Double"/> data type.
        /// </summary>
        Double,
        /// <summary>
        /// Specifies that real literals will be stored using the <see cref="System.Decimal"/> data type.
        /// </summary>
        Decimal
    }
}
