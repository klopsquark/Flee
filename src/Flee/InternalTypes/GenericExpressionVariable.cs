using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class GenericExpressionVariable<T> : IVariable, IGenericVariable<T>
    {
        private IGenericExpression<T> _expression;
        public IVariable Clone()
        {
            GenericExpressionVariable<T> copy = new GenericExpressionVariable<T>();
            copy._expression = _expression;
            return copy;
        }

        public object GetValue()
        {
            return _expression.Evaluate();
        }

        public object ValueAsObject
        {
            get { return _expression; }
            set { _expression = (IGenericExpression<T>)value; }
        }

        public System.Type VariableType => _expression.Context.Options.ResultType;
    }
}
