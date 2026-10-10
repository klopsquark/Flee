using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class DynamicExpressionVariable<T> : IVariable, IGenericVariable<T>
    {
        private IDynamicExpression _expression;
        public IVariable Clone()
        {
            DynamicExpressionVariable<T> copy = new DynamicExpressionVariable<T>();
            copy._expression = _expression;
            return copy;
        }

        public object GetValue()
        {
            return (T)_expression.Evaluate();
        }

        public object ValueAsObject
        {
            get { return _expression; }
            set { _expression = value as IDynamicExpression; }
        }

        public System.Type VariableType => _expression.Context.Options.ResultType;
    }
}
