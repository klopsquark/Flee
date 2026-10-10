using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    internal class PairEqualityComparer : EqualityComparer<ExpressionResultPair>
    {
        public override bool Equals(ExpressionResultPair x, ExpressionResultPair y)
        {
            return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(ExpressionResultPair obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
        }
    }

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

    internal class BatchLoadInfo
    {
        public string Name;
        public string ExpressionText;

        public ExpressionContext Context;
        public BatchLoadInfo(string name, string text, ExpressionContext context)
        {
            this.Name = name;
            this.ExpressionText = text;
            this.Context = context;
        }
    }

    public sealed class NodeEventArgs : EventArgs
    {

        private string _name;

        private object _result;

        internal NodeEventArgs()
        {
        }

        internal void SetData(string name, object result)
        {
            _name = name;
            _result = result;
        }

        public string Name => _name;

        public object Result => _result;
    }

}

