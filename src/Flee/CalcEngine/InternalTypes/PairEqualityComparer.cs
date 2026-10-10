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
}
