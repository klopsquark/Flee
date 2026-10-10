#nullable enable
namespace Flee.PublicTypes
{
    /// <summary>
    /// Provides the data for the <see cref="VariableCollection.InvokeFunction"/> event.
    /// </summary>
    /// <remarks>Use this class to provide the return value of an on-demand function.</remarks>
    public class InvokeFunctionEventArgs : EventArgs
    {

        private readonly string _name;
        private readonly object?[] _arguments;

        private object? _functionResult;
        internal InvokeFunctionEventArgs(string name, object?[] arguments)
        {
            _name = name;
            _arguments = arguments;
        }

        /// <summary>
        /// Gets the name of the on-demand function being invoked.
        /// </summary>
        /// <value>The name of the function</value>
        /// <remarks>
        /// Use this property to get the name of the on-demand function being invoked.
        /// </remarks>
        public string FunctionName
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the values of the arguments to the on-demand function being invoked.
        /// </summary>
        /// <value>An array with the values of each argument</value>
        /// <remarks>
        /// Use this property to get the values of the arguments to the on-demand function being invoked.
        /// </remarks>
        public object?[] Arguments
        {
            get { return _arguments; }
        }

        /// <summary>
        /// Gets or sets the result of the on-demand function being invoked.
        /// </summary>
        /// <value>The return value of the function</value>
        /// <remarks>
        /// Use this property to set the return value of the on-demand function being invoked.  The value must be
        /// assignable to the return type given in the <see cref="VariableCollection.ResolveFunction"/> event, or
        /// <see langword="null"/>; otherwise the expression throws an <see cref="ArgumentException"/> when evaluated.
        /// </remarks>
        public object? Result
        {
            get { return _functionResult; }
            set { _functionResult = value; }
        }
    }
}
