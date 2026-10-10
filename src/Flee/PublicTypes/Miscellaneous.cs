namespace Flee.PublicTypes
{
    public interface IExpression
    {
        IExpression Clone();
        string Text { get; }
        ExpressionInfo Info { get; }
        ExpressionContext Context { get; }
        object Owner { get; set; }
    }

    public interface IDynamicExpression : IExpression
    {
        object Evaluate();
    }

    public interface IGenericExpression<T> : IExpression
    {
        T Evaluate();
    }

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

        public string[] GetReferencedVariables()
        {
            IDictionary<string, string> dict = (IDictionary<string, string>)_data["ReferencedVariables"];
            string[] arr = new string[dict.Count];
            dict.Keys.CopyTo(arr, 0);
            return arr;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ExpressionOwnerMemberAccessAttribute : Attribute
    {


        private readonly bool _allowAccess;
        public ExpressionOwnerMemberAccessAttribute(bool allowAccess)
        {
            _allowAccess = allowAccess;
        }

        internal bool AllowAccess => _allowAccess;
    }

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

    public class ResolveFunctionEventArgs : EventArgs
    {

        private readonly string _name;
        private readonly Type[] _argumentTypes;

        private Type _returnType;
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

        public Type ReturnType
        {
            get { return _returnType; }
            set { _returnType = value; }
        }
    }

    public class InvokeFunctionEventArgs : EventArgs
    {

        private readonly string _name;
        private readonly object[] _arguments;

        private object _functionResult;
        internal InvokeFunctionEventArgs(string name, object[] arguments)
        {
            _name = name;
            _arguments = arguments;
        }

        public string FunctionName
        {
            get { return _name; }
        }

        public object[] Arguments
        {
            get { return _arguments; }
        }

        public object Result
        {
            get { return _functionResult; }
            set { _functionResult = value; }
        }
    }

    public enum RealLiteralDataType
    {
        Single,
        Double,
        Decimal
    }
}
