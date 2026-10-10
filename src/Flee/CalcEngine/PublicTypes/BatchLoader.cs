#nullable enable
using Flee.CalcEngine.InternalTypes;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    /// <summary>
    /// Represents a class that can be used to populate the calculation engine in one batch.
    /// </summary>
    /// <remarks>
    /// Normally, you have to add an expression to the calculation engine after any expressions it depends on.  By using this class, you can load expressions in any order and
    /// then have them be loaded into the calculation engine in one call.
    /// <para>Create an instance with <see cref="CalculationEngine.CreateBatchLoader"/> and load it with <see cref="CalculationEngine.BatchLoad"/>.</para>
    /// </remarks>
    public sealed class BatchLoader
    {

        private readonly IDictionary<string, BatchLoadInfo> _nameInfoMap;

        private readonly DependencyManager<string> _dependencies;
        internal BatchLoader()
        {
            _nameInfoMap = new Dictionary<string, BatchLoadInfo>(StringComparer.OrdinalIgnoreCase);
            _dependencies = new DependencyManager<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds an expression to the batch loader.
        /// </summary>
        /// <param name="atomName">The name that the expression will be associated with</param>
        /// <param name="expression">The expression to add</param>
        /// <param name="context">The context for the expression</param>
        /// <remarks>
        /// Use this method to add an expression to the batch loader and associate it with a name.  The expression is only parsed
        /// here, to find the names it references; it is compiled by <see cref="CalculationEngine.BatchLoad"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="atomName"/>, <paramref name="expression"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The loader already has an expression with the given name.</exception>
        /// <exception cref="ExpressionCompileException">The expression has a syntax error.  The loader is left unchanged.</exception>
        public void Add(string atomName, string expression, ExpressionContext context)
        {
            Utility.AssertNotNull(atomName, "atomName");
            Utility.AssertNotNull(expression, "expression");
            Utility.AssertNotNull(context, "context");

            // Parse first: an expression with a syntax error must leave nothing behind (D-25).
            ICollection<string> references = this.GetReferences(expression, context);

            BatchLoadInfo info = new BatchLoadInfo(atomName, expression, context);
            _nameInfoMap.Add(atomName, info);
            _dependencies.AddTail(atomName);

            foreach (string reference in references)
            {
                _dependencies.AddTail(reference);
                _dependencies.AddDepedency(reference, atomName);
            }
        }

        /// <summary>
        /// Determines if the loader contains an expression with a given name.
        /// </summary>
        /// <param name="atomName">The name of the expression to look up</param>
        /// <returns>True if the loader has an expression with the name; False otherwise</returns>
        /// <remarks>
        /// Use this method to determine if the loader contains an expression with a given name.
        /// </remarks>
        public bool Contains(string atomName)
        {
            return _nameInfoMap.ContainsKey(atomName);
        }

        internal BatchLoadInfo[] GetBachInfos()
        {
            string[] tails = _dependencies.GetTails();
            Queue<string> sources = _dependencies.GetSources(tails);

            IList<string> result = _dependencies.TopologicalSort(sources);

            List<BatchLoadInfo> infos = new List<BatchLoadInfo>(result.Count);

            foreach (string name in result)
            {
                // A referenced name that is not in the batch is left to the compiler, which reports
                // it as an undefined name for the expression that uses it (D-26).
                if (_nameInfoMap.TryGetValue(name, out BatchLoadInfo? info))
                {
                    infos.Add(info);
                }
            }

            return infos.ToArray();
        }

        private ICollection<string> GetReferences(string expression, ExpressionContext context)
        {
            IdentifierAnalyzer analyzer = context.ParseIdentifiers(expression);

            return analyzer.GetIdentifiers(context);
        }
    }

}

