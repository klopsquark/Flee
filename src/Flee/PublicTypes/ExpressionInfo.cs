#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>
    /// Holds information about a compiled expression.
    /// </summary>
    /// <remarks>
    /// This class holds information about an expression after it has been compiled.  For example: you can use this class to find out what variables
    /// an expression uses.
    /// </remarks>
    public sealed class ExpressionInfo
    {


        private readonly IDictionary<string, object> _data;
        internal ExpressionInfo()
        {
            _data = new Dictionary<string, object>
            {
                {"ReferencedVariables", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)}
            };
        }

        internal void AddReferencedVariable(string name)
        {
            IDictionary<string, string> dict = (IDictionary<string, string>)_data["ReferencedVariables"];
            dict[name] = name;
        }

        /// <summary>
        /// Gets the variables that are used in an expression.
        /// </summary>
        /// <returns>A string array containing all the variables used in the expression.</returns>
        /// <remarks>
        /// Use this method when you need to get a list of all variables used in an expression.
        /// Fields and properties of the expression owner that the expression uses by name are included.
        /// </remarks>
        public string[] GetReferencedVariables()
        {
            IDictionary<string, string> dict = (IDictionary<string, string>)_data["ReferencedVariables"];
            string[] arr = new string[dict.Count];
            dict.Keys.CopyTo(arr, 0);
            return arr;
        }
    }
}
