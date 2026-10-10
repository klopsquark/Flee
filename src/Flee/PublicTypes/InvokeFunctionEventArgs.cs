#nullable enable
namespace Flee.PublicTypes
{
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

        public string FunctionName
        {
            get { return _name; }
        }

        public object?[] Arguments
        {
            get { return _arguments; }
        }

        public object? Result
        {
            get { return _functionResult; }
            set { _functionResult = value; }
        }
    }
}
