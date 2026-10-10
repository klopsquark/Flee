#nullable enable
using Flee.CalcEngine.InternalTypes;
using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    /// <summary>
    /// A simple container of named expressions in which an expression can use the expressions added before it by name.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="CalculationEngine"/>, this class tracks no dependencies and caches no results: an expression that uses another one
    /// evaluates it each time.  Every name an added expression uses that is not a variable of <see cref="Context"/> or an imported namespace
    /// must be the name of an expression already in the engine.
    /// </remarks>
    public class SimpleCalcEngine
    {

        #region "Fields"

        private readonly IDictionary<string, IExpression> _expressions;

        private ExpressionContext _context;
        #endregion

        #region "Constructor"

        /// <summary>Creates an empty engine with a new <see cref="ExpressionContext"/>.</summary>
        public SimpleCalcEngine()
        {
            _expressions = new Dictionary<string, IExpression>(StringComparer.OrdinalIgnoreCase);
            _context = new ExpressionContext();
        }

        #endregion

        #region "Methods - Private"

        private void AddCompiledExpression(string expressionName, IExpression expression)
        {
            if (_expressions.ContainsKey(expressionName))
            {
                throw new InvalidOperationException($"The calc engine already contains an expression named '{expressionName}'");
            }
            else
            {
                _expressions.Add(expressionName, expression);
            }
        }

        private ExpressionContext ParseAndLink(string expressionName, string expression)
        {
            IdentifierAnalyzer analyzer = Context.ParseIdentifiers(expression);

            ExpressionContext context2 = _context.CloneInternal(true);
            this.LinkExpression(expressionName, context2, analyzer);

            // Tell the expression not to clone the context since it's already been cloned
            context2.NoClone = true;

            // Clear our context's variables
            _context.Variables.Clear();

            return context2;
        }

        private void LinkExpression(string expressionName, ExpressionContext context, IdentifierAnalyzer analyzer)
        {
            foreach (string identifier in analyzer.GetIdentifiers(context))
            {
                this.LinkIdentifier(identifier, expressionName, context);
            }
        }

        private void LinkIdentifier(string identifier, string expressionName, ExpressionContext context)
        {
            IExpression? child = null;

            if (!_expressions.TryGetValue(identifier, out child))
            {
                string msg = $"Expression '{expressionName}' references unknown name '{identifier}'";
                throw new InvalidOperationException(msg);
            }

            context.Variables.Add(identifier, child);
        }

        #endregion

        #region "Methods - Public"

        /// <summary>Compiles a dynamic expression and adds it to the engine under a name.</summary>
        /// <param name="expressionName">The name the expression is added under.</param>
        /// <param name="expression">The expression text.</param>
        /// <remarks>
        /// The expression is compiled against a copy of <see cref="Context"/>, in which the expressions it references are variables.
        /// Afterwards all variables of <see cref="Context"/> are removed, so variables meant for the next expression have to be set again.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The expression references a name that is not in the engine -or- the engine already holds an expression with the given name.
        /// </exception>
        /// <exception cref="ExpressionCompileException">The expression could not be compiled.</exception>
        public void AddDynamic(string expressionName, string expression)
        {
            ExpressionContext linkedContext = this.ParseAndLink(expressionName, expression);
            IExpression e = linkedContext.CompileDynamic(expression);
            this.AddCompiledExpression(expressionName, e);
        }

        /// <summary>Compiles a generic expression and adds it to the engine under a name.</summary>
        /// <typeparam name="T">The type that the expression evaluates to.</typeparam>
        /// <param name="expressionName">The name the expression is added under.</param>
        /// <param name="expression">The expression text.</param>
        /// <remarks>
        /// The expression is compiled against a copy of <see cref="Context"/>, in which the expressions it references are variables.
        /// Afterwards all variables of <see cref="Context"/> are removed, so variables meant for the next expression have to be set again.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The expression references a name that is not in the engine -or- the engine already holds an expression with the given name.
        /// </exception>
        /// <exception cref="ExpressionCompileException">The expression could not be compiled.</exception>
        public void AddGeneric<T>(string expressionName, string expression)
        {
            ExpressionContext linkedContext = this.ParseAndLink(expressionName, expression);
            IExpression e = linkedContext.CompileGeneric<T>(expression);
            this.AddCompiledExpression(expressionName, e);
        }

        /// <summary>Removes all expressions from the engine.</summary>
        public void Clear()
        {
            _expressions.Clear();
        }

        #endregion

        #region "Properties - Public"
        /// <summary>Gets the expression added under a name.</summary>
        /// <param name="name">The name of the expression; case is ignored.</param>
        /// <value>The expression, or <see langword="null"/> if the engine has no expression with that name.</value>
        public IExpression? this[string name]
        {
            get
            {
                IExpression? e = null;
                _expressions.TryGetValue(name, out e);
                return e;
            }
        }

        /// <summary>Gets or sets the context that new expressions are compiled with.</summary>
        /// <value>The context.  Its options, imports and variables apply to the next expression added.</value>
        public ExpressionContext Context
        {
            get { return _context; }
            set { _context = value; }
        }
        #endregion
    }

}
