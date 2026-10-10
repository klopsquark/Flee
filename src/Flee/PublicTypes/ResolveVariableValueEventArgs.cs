#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>
    /// Provides the data for the <see cref="VariableCollection.ResolveVariableValue"/> event.
    /// </summary>
    /// <remarks>Use this class to provide the value of an on-demand variable.</remarks>
    public class ResolveVariableValueEventArgs : EventArgs
    {
        private readonly string _name;
        private readonly Type _type;

        private object? _value;
        internal ResolveVariableValueEventArgs(string name, Type t)
        {
            _name = name;
            _type = t;
        }

        /// <summary>
        /// Gets the name of an on-demand variable.
        /// </summary>
        /// <value>The name of the variable</value>
        /// <remarks>
        /// Use this property to get the name of the variable whose value needs to be resolved.
        /// </remarks>
        public string VariableName
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the type of an on-demand variable.
        /// </summary>
        /// <value>The type of the variable</value>
        /// <remarks>
        /// Use this property to get the type of the variable whose value needs to be resolved.
        /// </remarks>
        public Type VariableType
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets or sets the value of an on-demand variable.
        /// </summary>
        /// <value>The value of the variable</value>
        /// <remarks>
        /// Use this property to get or set the value of an on-demand variable.  The value must be assignable to
        /// <see cref="VariableType"/>, or <see langword="null"/>; otherwise the expression throws an <see cref="ArgumentException"/>
        /// when evaluated.
        /// </remarks>
        public object? VariableValue
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
