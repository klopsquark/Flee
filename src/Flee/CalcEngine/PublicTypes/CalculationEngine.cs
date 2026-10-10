#nullable enable
using System.Reflection.Emit;
using System.Reflection;
using Flee.CalcEngine.InternalTypes;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    /// <summary>
    /// Creates a calculation network which allows expressions to refer to the result of other expressions, tracks dependencies, and enables natural order recalculation.
    /// </summary>
    /// <remarks>
    /// This class acts as a container for expressions.  As expressions are added to it, their dependencies are tracked and their result is cached.  Expressions defined
    /// in this class can reference other contained expressions.  Once all expressions are added, a natural order recalculate can be performed.
    /// <para>
    /// An expression references another contained expression by its name, as long as no variable or member of the expression owner has
    /// the same name.  Names are matched ignoring case.
    /// </para>
    /// </remarks>
    /// <example>This example shows how to add expressions to the engine and have them reference other contained expressions:
    /// <code lang="C#">
    /// CalculationEngine engine = new CalculationEngine();
    /// ExpressionContext context = new ExpressionContext();
    /// VariableCollection variables = context.Variables;
    ///
    /// // Add some variables
    /// variables.Add("x", 100);
    /// variables.Add("y", 200);
    ///
    /// // Add an expression to the calculation engine as "a"
    /// engine.Add("a", "x * 2", context);
    ///
    /// // Add an expression to the engine as "b"
    /// engine.Add("b", "y + 100", context);
    ///
    /// // Add an expression at "c" that uses the results of "a" and "b"
    /// engine.Add("c", "a + b", context);
    ///
    /// // Get the value of "c"
    /// int result = engine.GetResult&lt;int&gt;("c");
    ///
    /// // Update a variable on the "a" expression
    /// variables["x"] = 200;
    ///
    /// // Recalculate it
    /// engine.Recalculate("a");
    ///
    /// // Get the updated result
    /// result = engine.GetResult&lt;int&gt;("c");
    /// </code>
    /// </example>
    public class CalculationEngine
    {
        #region "Fields"
        private readonly DependencyManager<ExpressionResultPair> _dependencies;
        /// <summary>
        /// Map of name to node
        /// </summary>
        private readonly Dictionary<string, ExpressionResultPair> _nameNodeMap;
        #endregion

        #region "Events"
        /// <summary>
        /// Occurs when the calculation engine recalculates a node.
        /// </summary>
        /// <remarks>You can listen to this event to be notified when a node in the calculation engine is recalculated.</remarks>
        public event EventHandler<NodeEventArgs>? NodeRecalculated;
        #endregion

        #region "Constructor"
        /// <summary>Creates an empty calculation engine.</summary>
        public CalculationEngine()
        {
            _dependencies = new DependencyManager<ExpressionResultPair>(new PairEqualityComparer());
            _nameNodeMap = new Dictionary<string, ExpressionResultPair>(StringComparer.OrdinalIgnoreCase);
        }
        #endregion

        #region "Methods - Private"
        private void AddTemporaryHead(string headName)
        {
            GenericExpressionResultPair<int> pair = new GenericExpressionResultPair<int>();
            pair.SetName(headName);

            if (!_nameNodeMap.ContainsKey(headName))
            {
                _dependencies.AddTail(pair);
                _nameNodeMap.Add(headName, pair);
            }
            else
            {
                throw new ArgumentException($"An expression already exists at '{headName}'");
            }
        }

        private void DoBatchLoadAdd(BatchLoadInfo info)
        {
            try
            {
                this.Add(info.Name, info.ExpressionText, info.Context);
            }
            catch (ExpressionCompileException ex)
            {
                this.Clear();
                throw new BatchLoadCompileException(info.Name, info.ExpressionText, ex);
            }
        }

        private ExpressionResultPair? GetTail(string tailName)
        {
            Utility.AssertNotNull(tailName, "name");
            ExpressionResultPair? pair = null;
            _nameNodeMap.TryGetValue(tailName, out pair);
            return pair;
        }

        private ExpressionResultPair GetTailWithValidate(string tailName)
        {
            Utility.AssertNotNull(tailName, "name");
            ExpressionResultPair? pair = this.GetTail(tailName);

            if (pair == null)
            {
                throw new ArgumentException($"No expression is associated with the name '{tailName}'");
            }
            else
            {
                return pair;
            }
        }

        private string[] GetNames(IList<ExpressionResultPair> pairs)
        {
            string[] names = new string[pairs.Count];

            for (int i = 0; i <= names.Length - 1; i++)
            {
                names[i] = pairs[i].Name;
            }

            return names;
        }

        private ExpressionResultPair[] GetRootTails(string[] roots)
        {
            // No roots supplied so get everything
            if (roots.Length == 0)
            {
                return _dependencies.GetTails();
            }

            // Get the tail for each name
            ExpressionResultPair[] arr = new ExpressionResultPair[roots.Length];

            for (int i = 0; i <= arr.Length - 1; i++)
            {
                arr[i] = this.GetTailWithValidate(roots[i]);
            }

            return arr;
        }

        #endregion

        #region "Methods - Internal"

        internal void FixTemporaryHead(IDynamicExpression expression, ExpressionContext context, Type resultType)
        {
            Type pairType = typeof(GenericExpressionResultPair<>);
            pairType = pairType.MakeGenericType(resultType);

            // CreateInstance returns null only for Nullable<T>; pairType is a class.
            ExpressionResultPair pair = (ExpressionResultPair)Activator.CreateInstance(pairType)!;
            // Only called while compiling an engine expression: Add has set the name with SetCalcEngine.
            string headName = context.CalcEngineExpressionName!;
            pair.SetName(headName);
            pair.SetExpression(expression);

            ExpressionResultPair oldPair = _nameNodeMap[headName];
            _dependencies.ReplaceDependency(oldPair, pair);
            _nameNodeMap[headName] = pair;

            // Let the pair store the result of its expression
            pair.Recalculate();
        }

        /// <summary>
        /// Called by an expression when it references another expression in the engine
        /// </summary>
        /// <param name="tailName"></param>
        /// <param name="context"></param>
        internal void AddDependency(string tailName, ExpressionContext context)
        {
            // Null when the expression names something the engine does not hold; DependencyManager then
            // throws ArgumentNullException, as before.
            ExpressionResultPair actualTail = this.GetTail(tailName)!;
            // Only called while compiling an engine expression: Add has set the name with SetCalcEngine
            // and added the temporary head under it.
            string headName = context.CalcEngineExpressionName!;
            ExpressionResultPair actualHead = this.GetTail(headName)!;

            // An expression could depend on the same reference more than once (ie: "a + a * a")
            _dependencies.AddDepedency(actualTail, actualHead);
        }

        internal Type ResolveTailType(string tailName)
        {
            // Called after AddDependency succeeded for this name, so the tail exists.
            ExpressionResultPair actualTail = this.GetTail(tailName)!;
            return actualTail.ResultType;
        }

        internal bool HasTail(string tailName)
        {
            return _nameNodeMap.ContainsKey(tailName);
        }

        internal void EmitLoad(string tailName, FleeILGenerator ilg)
        {
            // Both members exist: ExpressionContext.CalculationEngine and this class's GetResult<T>.
            PropertyInfo pi = typeof(ExpressionContext).GetProperty("CalculationEngine")!;
            ilg.Emit(OpCodes.Callvirt, pi.GetGetMethod());

            // Load the tail
            MemberInfo[] methods = typeof(CalculationEngine).FindMembers(MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public, Type.FilterNameIgnoreCase, "GetResult");
            MethodInfo? mi = null;

            foreach (MethodInfo method in methods)
            {
                if (method.IsGenericMethod)
                {
                    mi = method;
                    break;
                }
            }

            Type resultType = this.ResolveTailType(tailName);

            mi = mi!.MakeGenericMethod(resultType);

            ilg.Emit(OpCodes.Ldstr, tailName);
            ilg.Emit(OpCodes.Call, mi);
        }

        #endregion

        #region "Methods - Public"
        /// <summary>
        /// Adds an expression to the calculation engine.
        /// </summary>
        /// <param name="atomName">The name that the expression will be associated with</param>
        /// <param name="expression">The expression to add</param>
        /// <param name="context">The context for the expression</param>
        /// <remarks>
        /// Use this method to add an expression to the engine and associate it with a name.  The expression is compiled and
        /// evaluated at once, and its result is cached.  The expressions it references must already be in the engine.
        /// The context is attached to this engine (see <see cref="ExpressionContext.CalculationEngine"/>).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="atomName"/>, <paramref name="expression"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The engine already has an expression with the given name.</exception>
        /// <exception cref="ExpressionCompileException">The expression could not be compiled; the name is not added.</exception>
        public void Add(string atomName, string expression, ExpressionContext context)
        {
            Utility.AssertNotNull(atomName, "atomName");
            Utility.AssertNotNull(expression, "expression");
            Utility.AssertNotNull(context, "context");

            this.AddTemporaryHead(atomName);

            context.SetCalcEngine(this, atomName);

            try
            {
                context.CompileDynamic(expression);
            }
            catch
            {
                // Do not leave the temporary head behind: the name must stay free for another
                // attempt and the engine must not hold an atom without an expression (R-040).
                this.Remove(atomName);
                throw;
            }
        }

        /// <summary>
        /// Removes an expression and all its dependents from the calculation engine.
        /// </summary>
        /// <param name="name">The name whose expression to remove</param>
        /// <returns>True if the name was removed from the engine; False otherwise</returns>
        /// <remarks>
        /// Use this method to remove an expression and all its dependents from the calculation engine.  No exception is thrown if the
        /// name does not exist in the engine.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public bool Remove(string name)
        {
            ExpressionResultPair? tail = this.GetTail(name);

            if (tail == null)
            {
                return false;
            }

            ExpressionResultPair[] dependents = _dependencies.GetDependents(tail);
            _dependencies.Remove(dependents);

            foreach (ExpressionResultPair pair in dependents)
            {
                _nameNodeMap.Remove(pair.Name);
            }

            return true;
        }

        /// <summary>
        /// Creates a BatchLoader that can be used to populate the calculation engine in one batch.
        /// </summary>
        /// <returns>A new instance of the BatchLoader class</returns>
        /// <remarks>
        /// Use this method to create a BatchLoader instance.
        /// </remarks>
        public BatchLoader CreateBatchLoader()
        {
            BatchLoader loader = new BatchLoader();
            return loader;
        }

        /// <summary>
        /// Populates the calculation engine from the given BatchLoader.
        /// </summary>
        /// <param name="loader">The batch loader instance to use</param>
        /// <remarks>
        /// Call this method to load the calculation engine with all the expressions in the given batch loader.  The engine is
        /// cleared first, and the expressions are added in an order that puts every expression after the ones it references.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="loader"/> is <see langword="null"/>.</exception>
        /// <exception cref="BatchLoadCompileException">An expression could not be compiled; the engine is left empty.</exception>
        /// <exception cref="CircularReferenceException">The expressions in the loader reference each other in a circle.</exception>
        public void BatchLoad(BatchLoader loader)
        {
            Utility.AssertNotNull(loader, "loader");
            this.Clear();

            BatchLoadInfo[] infos = loader.GetBachInfos();

            foreach (BatchLoadInfo info in infos)
            {
                this.DoBatchLoadAdd(info);
            }
        }

        // Called from generated IL. NoInlining keeps the .NET 10 JIT from inlining this method into
        // every compiled expression, which made each expression's first call cost about 1.4 ms (R-019).
        /// <summary>
        /// Gets the cached result of a contained expression.
        /// </summary>
        /// <typeparam name="T">The type of the expression's result.</typeparam>
        /// <param name="name">The name that the expression is associated with</param>
        /// <returns>The cached result of evaluating the expression</returns>
        /// <remarks>Use this method after a recalculate to get the updated value of an expression.</remarks>
        /// <exception cref="ArgumentException">
        /// No expression is associated with the name -or- <typeparamref name="T"/> is not exactly the expression's result type.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public T GetResult<T>(string name)
        {
            ExpressionResultPair tail = this.GetTailWithValidate(name);

            if ((!object.ReferenceEquals(typeof(T), tail.ResultType)))
            {
                string msg = $"The result type of '{name}' ('{tail.ResultType.Name}') does not match the supplied type argument ('{typeof(T).Name}')";
                throw new ArgumentException(msg);
            }

            GenericExpressionResultPair<T> actualTail = (GenericExpressionResultPair<T>)tail;
            return actualTail.Result;
        }

        /// <summary>
        /// Gets the cached result of a contained expression.
        /// </summary>
        /// <param name="name">The name that the expression is associated with</param>
        /// <returns>The cached result of evaluating the expression</returns>
        /// <remarks>Use this method after a recalculate to get the updated value of an expression.</remarks>
        /// <exception cref="ArgumentException">No expression is associated with the name.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public object? GetResult(string name)
        {
            ExpressionResultPair tail = this.GetTailWithValidate(name);
            return tail.ResultAsObject;
        }

        /// <summary>
        /// Gets the expression associated with a name.
        /// </summary>
        /// <param name="name">The name that the expression is associated with</param>
        /// <returns>The expression associated with the given name</returns>
        /// <remarks>Use this method to obtain the expression associated with a name.</remarks>
        /// <exception cref="ArgumentException">No expression is associated with the name.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public IExpression? GetExpression(string name)
        {
            ExpressionResultPair tail = this.GetTailWithValidate(name);
            return tail.Expression;
        }

        /// <summary>
        /// Gets the names of the expressions that depend on a given name.
        /// </summary>
        /// <param name="name">The name of the expression to look up</param>
        /// <returns>The names of all expressions that depend on the expression with the given name</returns>
        /// <remarks>
        /// Use this method to obtain all the expressions that depend on a given name.  For example: if a=100, b=200, and c=a+b, then calling
        /// GetDependents("a") will return "c" since when the value of "a" changes, the value of "c" will also change.  This method is not recursive, so it
        /// will only return the names that directly depend on the given name.  This method is the inverse of <see cref="GetPrecedents(string)"/>
        /// </remarks>
        public string[] GetDependents(string name)
        {
            ExpressionResultPair? pair = this.GetTail(name);
            List<ExpressionResultPair> dependents = new List<ExpressionResultPair>();

            if ((pair != null))
            {
                _dependencies.GetDirectDependents(pair, dependents);
            }

            return this.GetNames(dependents);
        }

        /// <summary>
        /// Gets the names of the expressions that a given name depends on.
        /// </summary>
        /// <param name="name">The name of the expression to look up</param>
        /// <returns>The names of all expressions that the given name depends on</returns>
        /// <remarks>
        /// Use this method to obtain all the expressions that a given name depends on.  For example: if a=100, b=200, and c=a+b, then calling
        /// GetPrecedents("c") will return "a, b" since when either "a" or "b" change, the value of "c" will also change.  This method is not recursive, so it
        /// will only return the names that the given name directly depends on.  This method is the inverse of <see cref="GetDependents(string)"/>
        /// </remarks>
        public string[] GetPrecedents(string name)
        {
            ExpressionResultPair? pair = this.GetTail(name);
            List<ExpressionResultPair> dependents = new List<ExpressionResultPair>();

            if ((pair != null))
            {
                _dependencies.GetDirectPrecedents(pair, dependents);
            }

            return this.GetNames(dependents);
        }

        /// <summary>
        /// Determines if an expression with a given name is referenced by any other expressions in the engine.
        /// </summary>
        /// <param name="name">The name of the expression to look up</param>
        /// <returns>True if the engine has expressions that depend on it; False otherwise</returns>
        /// <remarks>
        /// Use this method to determine if the expression associated with a given name has any expressions that depend on it.  For example: you can use this method to allow
        /// a user to remove an expression only when no other expressions depend on it.
        /// </remarks>
        public bool HasDependents(string name)
        {
            ExpressionResultPair? pair = this.GetTail(name);
            return (pair != null) && _dependencies.HasDependents(pair);
        }

        /// <summary>
        /// Determines if an expression with a given name depends on any other expression.
        /// </summary>
        /// <param name="name">The name of the expression to look up</param>
        /// <returns>True if the name has expressions that it depends on; False otherwise</returns>
        /// <remarks>
        /// Use this method to determine if an expression depends on any other expressions.
        /// </remarks>
        public bool HasPrecedents(string name)
        {
            ExpressionResultPair? pair = this.GetTail(name);
            return (pair != null) && _dependencies.HasPrecedents(pair);
        }

        /// <summary>
        /// Determines if the engine contains an expression with a given name.
        /// </summary>
        /// <param name="name">The name of the expression to look up</param>
        /// <returns>True if the engine has an expression with the name; False otherwise</returns>
        /// <remarks>
        /// Use this method to determine if the calculation engine contains an expression with a given name.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public bool Contains(string name)
        {
            Utility.AssertNotNull(name, "name");
            return _nameNodeMap.ContainsKey(name);
        }

        /// <summary>
        /// Performs a natural order recalculation of the engine.
        /// </summary>
        /// <param name="roots">The names representing the starting points of the recalculation</param>
        /// <remarks>
        /// This method will perform a natural order recalculate on the expressions in the engine.  The recalculation will start at the given roots
        /// and continue with all their dependents.  If no roots are given, then a recalculation of all expressions is performed.
        /// The <see cref="NodeRecalculated"/> event is raised for each recalculated expression.
        /// </remarks>
        /// <exception cref="CircularReferenceException">A recalculate is requested on an engine containing a circular reference</exception>
        /// <exception cref="ArgumentException">A root name is not associated with an expression.</exception>
        public void Recalculate(params string[] roots)
        {
            // Get the tails corresponding to the names
            ExpressionResultPair[] rootTails = this.GetRootTails(roots);
            // Create a dependency list based on the tails
            DependencyManager<ExpressionResultPair> tempDependents = _dependencies.CloneDependents(rootTails);
            // Get the sources (ie: nodes with no incoming edges) since that's what the sort requires
            Queue<ExpressionResultPair> sources = tempDependents.GetSources(rootTails);
            // Do the topological sort
            IList<ExpressionResultPair> calcList = tempDependents.TopologicalSort(sources);

            NodeEventArgs args = new NodeEventArgs();

            // Recalculate the sorted expressions
            foreach (ExpressionResultPair pair in calcList)
            {
                pair.Recalculate();
                args.SetData(pair.Name, pair.ResultAsObject);
                if (NodeRecalculated != null)
                {
                    NodeRecalculated(this, args);
                }
            }
        }

        /// <summary>
        /// Clears all expressions from the CalculationEngine
        /// </summary>
        /// <remarks>Use this method to reset the CalculationEngine to the empty state.</remarks>
        public void Clear()
        {
            _dependencies.Clear();
            _nameNodeMap.Clear();
        }

        #endregion

        #region "Properties - Public"
        /// <summary>
        /// Gets the number of expressions contained in the calculation engine.
        /// </summary>
        /// <value>The number of expressions in the calculation engine</value>
        /// <remarks>
        /// Use this property to see how many expressions are contained in the calculation engine.
        /// </remarks>
        public int Count
        {
            get { return _dependencies.Count; }
        }

        /// <summary>
        /// Gets a string representation of the engine's dependency graph.
        /// </summary>
        /// <value>A string representing the graph.</value>
        /// <remarks>
        /// Use this property to get a string version of the engine's dependency graph.  There will be one line for each expression. Each line will
        /// be of the format "[reference] -> [dependant1, dependant2]" and is read as "A change in [reference] will cause a change in [dependant]".
        /// An expression that nothing depends on is listed as "[reference] -> &lt;empty&gt;".
        /// </remarks>
        public string DependencyGraph
        {
            get { return _dependencies.DependencyGraph; }
        }
        #endregion
    }

}
