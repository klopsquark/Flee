using Flee.CalcEngine.InternalTypes;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    public sealed class BatchLoader
    {

        private readonly IDictionary<string, BatchLoadInfo> _nameInfoMap;

        private readonly DependencyManager<string> _dependencies;
        internal BatchLoader()
        {
            _nameInfoMap = new Dictionary<string, BatchLoadInfo>(StringComparer.OrdinalIgnoreCase);
            _dependencies = new DependencyManager<string>(StringComparer.OrdinalIgnoreCase);
        }

        public void Add(string atomName, string expression, ExpressionContext context)
        {
            Utility.AssertNotNull(atomName, "atomName");
            Utility.AssertNotNull(expression, "expression");
            Utility.AssertNotNull(context, "context");

            BatchLoadInfo info = new BatchLoadInfo(atomName, expression, context);
            _nameInfoMap.Add(atomName, info);
            _dependencies.AddTail(atomName);

            ICollection<string> references = this.GetReferences(expression, context);

            foreach (string reference in references)
            {
                _dependencies.AddTail(reference);
                _dependencies.AddDepedency(reference, atomName);
            }
        }

        public bool Contains(string atomName)
        {
            return _nameInfoMap.ContainsKey(atomName);
        }

        internal BatchLoadInfo[] GetBachInfos()
        {
            string[] tails = _dependencies.GetTails();
            Queue<string> sources = _dependencies.GetSources(tails);

            IList<string> result = _dependencies.TopologicalSort(sources);

            BatchLoadInfo[] infos = new BatchLoadInfo[result.Count];

            for (int i = 0; i <= result.Count - 1; i++)
            {
                infos[i] = _nameInfoMap[result[i]];
            }

            return infos;
        }

        private ICollection<string> GetReferences(string expression, ExpressionContext context)
        {
            IdentifierAnalyzer analyzer = context.ParseIdentifiers(expression);

            return analyzer.GetIdentifiers(context);
        }
    }

}

