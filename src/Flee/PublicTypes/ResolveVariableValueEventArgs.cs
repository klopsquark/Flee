namespace Flee.PublicTypes
{
    public class ResolveVariableValueEventArgs : EventArgs
    {
        private readonly string _name;
        private readonly Type _type;

        private object _value;
        internal ResolveVariableValueEventArgs(string name, Type t)
        {
            _name = name;
            _type = t;
        }

        public string VariableName
        {
            get { return _name; }
        }

        public Type VariableType
        {
            get { return _type; }
        }

        public object VariableValue
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
