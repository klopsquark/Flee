#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>
    /// Provides the data for the <see cref="VariableCollection.ResolveVariableType"/> event.
    /// </summary>
    /// <remarks>Use this class to provide the type of an on-demand variable.</remarks>
    public class ResolveVariableTypeEventArgs : EventArgs
    {
        private readonly string _name;
        private Type? _type;
        internal ResolveVariableTypeEventArgs(string name)
        {
            this._name = name;
        }

        /// <summary>
        /// Gets the name of an on-demand variable.
        /// </summary>
        /// <value>The name of the variable</value>
        /// <remarks>
        /// Use this property to get the name of the variable whose type needs to be resolved.
        /// </remarks>
        public string VariableName => _name;

        /// <summary>
        /// Gets or sets the type of an on-demand variable.
        /// </summary>
        /// <value>The type of the variable</value>
        /// <remarks>
        /// Use this property to get or set the type of the on-demand variable.  If it stays <see langword="null"/>,
        /// the name is not treated as a variable.
        /// </remarks>
        public Type? VariableType
        {
            get { return _type; }
            set { _type = value; }
        }
    }
}
