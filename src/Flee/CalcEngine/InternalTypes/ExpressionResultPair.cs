using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    internal abstract class ExpressionResultPair
    {

        private string _name;

        private IDynamicExpression _expression;

        protected ExpressionResultPair()
        {
        }

        public abstract void Recalculate();

        public void SetExpression(IDynamicExpression e)
        {
            _expression = e;
        }

        public void SetName(string name)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }

        public string Name => _name;

        public abstract Type ResultType { get; }
        public abstract object ResultAsObject { get; set; }

        public IDynamicExpression Expression => _expression;
    }
}
