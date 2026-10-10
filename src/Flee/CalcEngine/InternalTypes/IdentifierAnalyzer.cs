using Flee.Parsing;
using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    // Analyzer carries a misplaced [Obsolete] from the VB conversion (doc/deferred.md, D-03).
#pragma warning disable CS0618
    internal class IdentifierAnalyzer : Analyzer
#pragma warning restore CS0618
    {

        private readonly IDictionary<int, string> _identifiers;
        private int _memberExpressionCount;

        private bool _inFieldPropertyExpression;
        public IdentifierAnalyzer()
        {
            _identifiers = new Dictionary<int, string>();
        }

        public override Node Exit(Node node)
        {
            switch (node.Id)
            {
                case (int)ExpressionConstants.IDENTIFIER:
                    this.ExitIdentifier((Token)node);
                    break;
                case (int)ExpressionConstants.FIELD_PROPERTY_EXPRESSION:
                    this.ExitFieldPropertyExpression();
                    break;
            }

            return node;
        }

        public override void Enter(Node node)
        {
            switch (node.Id)
            {
                case (int)ExpressionConstants.MEMBER_EXPRESSION:
                    this.EnterMemberExpression();
                    break;
                case (int)ExpressionConstants.FIELD_PROPERTY_EXPRESSION:
                    this.EnterFieldPropertyExpression();
                    break;
            }
        }

        private void ExitIdentifier(Token node)
        {
            if (!_inFieldPropertyExpression)
            {
                return;
            }

            if (!_identifiers.ContainsKey(_memberExpressionCount))
            {
                _identifiers.Add(_memberExpressionCount, node.Image);
            }
        }

        private void EnterMemberExpression()
        {
            _memberExpressionCount += 1;
        }

        private void EnterFieldPropertyExpression()
        {
            _inFieldPropertyExpression = true;
        }

        private void ExitFieldPropertyExpression()
        {
            _inFieldPropertyExpression = false;
        }

        public override void Reset()
        {
            _identifiers.Clear();
            _memberExpressionCount = -1;
        }

        public ICollection<string> GetIdentifiers(ExpressionContext context)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            ExpressionImports ei = context.Imports;

            foreach (string identifier in _identifiers.Values)
            {
                // Skip names registered as namespaces
                if (ei.HasNamespace(identifier))
                {
                    continue;
                }
                else if (context.Variables.ContainsKey(identifier))
                {
                    // Identifier is a variable
                    continue;
                }

                // Get only the unique values
                dict[identifier] = null;
            }

            return dict.Keys;
        }
    }
}
