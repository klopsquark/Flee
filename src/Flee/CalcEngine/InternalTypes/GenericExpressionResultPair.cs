using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    internal class GenericExpressionResultPair<T> : ExpressionResultPair
    {
        private T _result;
        public GenericExpressionResultPair()
        {
        }

        public override void Recalculate()
        {
            _result = (T)Expression.Evaluate();
        }

        public T Result => _result;

        public override System.Type ResultType => typeof(T);

        public override object ResultAsObject
        {
            get { return _result; }
            set { _result = (T)value; }
        }
    }
}
