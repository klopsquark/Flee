#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>
    /// Provides the data for the <see cref="VariableCollection.ResolveFunction"/> event.
    /// </summary>
    /// <remarks>Use this class to provide the return type of an on-demand function.</remarks>
    public class ResolveFunctionEventArgs : EventArgs
    {

        private readonly string _name;
        private readonly Type[] _argumentTypes;

        private Type? _returnType;
        internal ResolveFunctionEventArgs(string name, Type[] argumentTypes)
        {
            _name = name;
            _argumentTypes = argumentTypes;
        }

        /// <summary>
        /// Gets the name of the on-demand function being resolved.
        /// </summary>
        /// <value>The name of the function</value>
        /// <remarks>
        /// Use this property to get the name of the on-demand function being resolved.
        /// </remarks>
        public string FunctionName
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the types of the arguments to the on-demand function being resolved.
        /// </summary>
        /// <value>An array with the type of each argument</value>
        /// <remarks>
        /// Use this property to get the types of the arguments to the on-demand function being resolved.
        /// </remarks>
        public Type[] ArgumentTypes
        {
            get { return _argumentTypes; }
        }

        /// <summary>
        /// Gets or sets the return type of the on-demand function being resolved.
        /// </summary>
        /// <value>The return type of the function</value>
        /// <remarks>
        /// Use this property to set the return type of the on-demand function being resolved.  If it stays
        /// <see langword="null"/>, the function is not resolved.
        /// </remarks>
        public Type? ReturnType
        {
            get { return _returnType; }
            set { _returnType = value; }
        }
    }
}
