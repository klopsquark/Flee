using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal interface IVariable
    {
        IVariable Clone();
        Type VariableType { get; }
        object ValueAsObject { get; set; }
    }

    internal interface IGenericVariable<T>
    {
        object GetValue();
    }

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

    internal class GenericVariable<T> : IVariable, IGenericVariable<T>
    {


        public object Value;
        public IVariable Clone()
        {
            GenericVariable<T> copy = new GenericVariable<T> { Value = Value };
            return copy;
        }

        public object GetValue()
        {
            return Value;
        }

        public System.Type VariableType => typeof(T);

        public object ValueAsObject
        {
            get { return Value; }
            set
            {
                if (value == null)
                {
                    Value = default(T);
                }
                else
                {
                    Value = value;
                }
            }
        }
    }

}
