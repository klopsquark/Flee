#nullable enable
namespace Flee.PublicTypes
{
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

        public string FunctionName
        {
            get { return _name; }
        }

        public Type[] ArgumentTypes
        {
            get { return _argumentTypes; }
        }

        public Type? ReturnType
        {
            get { return _returnType; }
            set { _returnType = value; }
        }
    }
}
