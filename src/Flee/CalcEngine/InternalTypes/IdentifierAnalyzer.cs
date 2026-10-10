using Flee.Parsing;
using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    internal class IdentifierAnalyzer : Analyzer
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
            if (_inFieldPropertyExpression == false)
            {
                return;
            }

            if (_identifiers.ContainsKey(_memberExpressionCount) == false)
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
                if (ei.HasNamespace(identifier) == true)
                {
                    continue;
                }
                else if (context.Variables.ContainsKey(identifier) == true)
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
