namespace Flee.PublicTypes
{
    public class ResolveVariableTypeEventArgs : EventArgs
    {
        private readonly string _name;
        private Type _type;
        internal ResolveVariableTypeEventArgs(string name)
        {
            this._name = name;
        }

        public string VariableName => _name;

        public Type VariableType
        {
            get { return _type; }
            set { _type = value; }
        }
    }
}
