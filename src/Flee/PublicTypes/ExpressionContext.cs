#nullable enable
using Flee.CalcEngine.InternalTypes;
using Flee.CalcEngine.PublicTypes;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>Class that holds all information required to create an expression</summary>
    /// <remarks>
    /// This class holds all information required to create an expression.
    /// <para>Thread safety: the <see cref="CompileDynamic"/> and <see cref="CompileGeneric{TResultType}"/> methods are thread-safe.</para>
    /// </remarks>
    /// <example>
    /// This example shows how to create and evaluate an expression:
    /// <code lang="C#">
    /// // Define the context of our expression
    /// ExpressionContext context = new ExpressionContext();
    /// // Allow the expression to use all static public methods of System.Math
    /// context.Imports.AddType(typeof(Math));
    ///
    /// // Define an int variable
    /// context.Variables["a"] = 100;
    ///
    /// // Create a dynamic expression that evaluates to an Object
    /// IDynamicExpression eDynamic = context.CompileDynamic("sqrt(a) + pi");
    /// // Create a generic expression that evaluates to a double
    /// IGenericExpression&lt;double&gt; eGeneric = context.CompileGeneric&lt;double&gt;("sqrt(a) + pi");
    ///
    /// // Evaluate the expressions
    /// double result = (double)eDynamic.Evaluate();
    /// result = eGeneric.Evaluate();
    ///
    /// // Update the value of our variable
    /// context.Variables["a"] = 144;
    /// // Evaluate again to get the updated result
    /// result = eGeneric.Evaluate();
    /// </code>
    /// </example>
    public sealed class ExpressionContext
    {

        #region "Fields"

        private PropertyDictionary _properties;

        private readonly object _syncRoot = new object();

        private VariableCollection _variables;
        #endregion

        #region "Constructor"

        /// <summary>Creates a new expression context with the default expression owner.</summary>
        /// <remarks>Use this constructor to create an expression context when you don't plan to use an expression owner.</remarks>
        public ExpressionContext() : this(DefaultExpressionOwner.Instance)
        {
        }

        /// <summary>Creates a new expression context with a specified expression owner.</summary>
        /// <param name="expressionOwner">The expression owner instance to use</param>
        /// <remarks>
        /// Use this constructor to create an expression context when you want to supply an expression owner.
        /// The members of the owner can be used in an expression without qualification; <see cref="ExpressionOptions.OwnerMemberAccess"/>
        /// controls which of them are accessible.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="expressionOwner"/> is <see langword="null"/>.</exception>
        public ExpressionContext(object expressionOwner)
        {
            Utility.AssertNotNull(expressionOwner, "expressionOwner");
            _properties = new PropertyDictionary();

            _properties.SetValue("CalculationEngine", null);
            _properties.SetValue("CalcEngineExpressionName", null);
            _properties.SetValue("IdentifierParser", null);

            _properties.SetValue("ExpressionOwner", expressionOwner);

            _properties.SetValue("ParserOptions", new ExpressionParserOptions(this));

            _properties.SetValue("Options", new ExpressionOptions(this));
            _properties.SetValue("Imports", new ExpressionImports());
            this.Imports.SetContext(this);
            _variables = new VariableCollection(this);

            _properties.SetToDefault<bool>("NoClone");

            this.RecreateParser();
        }

        #endregion

        #region "Methods - Private"

        private void AssertTypeIsAccessibleInternal(Type t)
        {
            bool isPublic = t.IsPublic;

            if (t.IsNested)
            {
                isPublic = t.IsNestedPublic;
            }

            bool isSameModuleAsOwner = object.ReferenceEquals(t.Module, this.ExpressionOwner.GetType().Module);

            // Public types are always accessible.  Otherwise they have to be in the same module as the owner
            bool isAccessible = isPublic || isSameModuleAsOwner;

            if (!isAccessible)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.TypeNotAccessibleToExpression, t.Name);
                throw new ArgumentException(msg);
            }
        }

        private void AssertNestedTypeIsAccessible(Type? t)
        {
            while ((t != null))
            {
                AssertTypeIsAccessibleInternal(t);
                t = t.DeclaringType;
            }
        }
        #endregion

        #region "Methods - Internal"
        internal ExpressionContext CloneInternal(bool cloneVariables)
        {
            ExpressionContext context = (ExpressionContext)this.MemberwiseClone();
            context._properties = _properties.Clone();
            context._properties.SetValue("Options", context.Options.Clone(context));
            context._properties.SetValue("ParserOptions", context.ParserOptions.Clone(context));
            context._properties.SetValue("Imports", context.Imports.Clone());
            context.Imports.SetContext(context);

            if (cloneVariables)
            {
                context._variables = new VariableCollection(context);
                this.Variables.Copy(context._variables);
            }

            return context;
        }

        internal void AssertTypeIsAccessible(Type t)
        {
            if (t.IsNested)
            {
                AssertNestedTypeIsAccessible(t);
            }
            else
            {
                AssertTypeIsAccessibleInternal(t);
            }
        }

        internal ExpressionElement Parse(string expression, IServiceProvider services)
        {
            lock (_syncRoot)
            {
                System.IO.StringReader sr = new System.IO.StringReader(expression);
                ExpressionParser parser = this.Parser;
                parser.Reset(sr);
                parser.Tokenizer.Reset(sr);
                FleeExpressionAnalyzer analyzer = (FleeExpressionAnalyzer)parser.Analyzer;

                analyzer.SetServices(services);

                Node rootNode = DoParse();
                analyzer.Reset();
                // The analyzer always leaves the top element as the root node's first value.
                ExpressionElement topElement = (ExpressionElement)rootNode.Values[0]!;
                return topElement;
            }
        }

        internal void RecreateParser()
        {
            lock (_syncRoot)
            {
                FleeExpressionAnalyzer analyzer = new FleeExpressionAnalyzer();
                ExpressionParser parser = new ExpressionParser(TextReader.Null, analyzer, this);
                _properties.SetValue("ExpressionParser", parser);
                // The calculation engines' identifier parser is created again on next use, with
                // the current options (D-28).
                _properties.SetValue("IdentifierParser", null);
            }
        }

        internal Node DoParse()
        {
            try
            {
                return this.Parser.Parse();
            }
            catch (ParserLogException ex)
            {
                // Syntax error; wrap it in our exception and rethrow
                throw new ExpressionCompileException(ex);
            }
        }

        internal void SetCalcEngine(CalculationEngine engine, string calcEngineExpressionName)
        {
            _properties.SetValue("CalculationEngine", engine);
            _properties.SetValue("CalcEngineExpressionName", calcEngineExpressionName);
        }

        internal IdentifierAnalyzer ParseIdentifiers(string expression)
        {
            ExpressionParser parser = this.IdentifierParser;
            StringReader sr = new StringReader(expression);
            parser.Reset(sr);
            parser.Tokenizer.Reset(sr);

            IdentifierAnalyzer analyzer = (IdentifierAnalyzer)parser.Analyzer;
            analyzer.Reset();

            try
            {
                parser.Parse();
            }
            catch (ParserLogException ex)
            {
                // Syntax error; report it as DoParse does, not as the parser's own exception (D-25)
                throw new ExpressionCompileException(ex);
            }

            return (IdentifierAnalyzer)parser.Analyzer;
        }
        #endregion

        #region "Methods - Public"

        /// <summary>Creates a copy of the current context.</summary>
        /// <returns>An identical copy of the current context</returns>
        /// <remarks>
        /// Use this method when you need an identical copy of an existing context.  The copy has its own options, parser options,
        /// imports and variables; the variables are copied with their current values.
        /// </remarks>
        public ExpressionContext Clone()
        {
            return this.CloneInternal(true);
        }

        /// <summary>Creates a dynamic expression (one that evaluates to Object) from an expression text string and the current context</summary>
        /// <param name="expression">The expression text to parse</param>
        /// <returns>A new dynamic expression</returns>
        /// <remarks>
        /// Use this method when you want to create an expression that evaluates to an Object.  "Dynamic" means that the result type
        /// of the expression can be anything and is not fixed as with a generic expression.
        /// <para>
        /// Note: the context, imports, and options of the compiled expression will be a clone of the originals.  The variables however
        /// are not cloned and will point to the same instance.
        /// </para>
        /// </remarks>
        /// <exception cref="ExpressionCompileException">The expression could not be compiled</exception>
        public IDynamicExpression CompileDynamic(string expression)
        {
            return new Flee.InternalTypes.Expression<object>(expression, this, false);
        }

        /// <summary>Creates a generic expression from an expression text string and the current context</summary>
        /// <typeparam name="TResultType">The type that the expression evaluates to</typeparam>
        /// <param name="expression">The expression text to parse</param>
        /// <returns>A new generic expression</returns>
        /// <remarks>
        /// Use this method when you want to create an expression that evaluates to a strongly-typed value.
        /// <para>
        /// Note: the context, imports, and options of the compiled expression will be a clone of the originals.  The variables however
        /// are not cloned and will point to the same instance.
        /// </para>
        /// </remarks>
        /// <exception cref="ExpressionCompileException">The expression could not be compiled</exception>
        public IGenericExpression<TResultType> CompileGeneric<TResultType>(string expression)
        {
            return new Flee.InternalTypes.Expression<TResultType>(expression, this, true);
        }

        #endregion

        #region "Properties - Private"

        private ExpressionParser IdentifierParser
        {
            get
            {
                ExpressionParser? parser = _properties.GetValue<ExpressionParser>("IdentifierParser");

                if (parser == null)
                {
                    IdentifierAnalyzer analyzer = new IdentifierAnalyzer();
                    parser = new ExpressionParser(System.IO.TextReader.Null, analyzer, this);
                    //parser = new ExpressionParser(System.IO.StringReader.Null, analyzer, this);
                    _properties.SetValue("IdentifierParser", parser);
                }

                return parser;
            }
        }

        #endregion

        #region "Properties - Internal"

        internal bool NoClone
        {
            get { return _properties.GetValue<bool>("NoClone"); }
            set { _properties.SetValue("NoClone", value); }
        }

        internal object ExpressionOwner => _properties.GetValue<object>("ExpressionOwner");

        internal string? CalcEngineExpressionName => _properties.GetValue<string>("CalcEngineExpressionName");

        internal ExpressionParser Parser => _properties.GetValue<ExpressionParser>("ExpressionParser");

        #endregion

        #region "Properties - Public"
        /// <summary>Gets the ExpressionOptions to be used in an expression</summary>
        /// <value>The ExpressionOptions instance</value>
        /// <remarks>Use this property to access the options to be used in an expression.</remarks>
        public ExpressionOptions Options => _properties.GetValue<ExpressionOptions>("Options");

        /// <summary>
        /// Gets the types imported by an expression.
        /// </summary>
        /// <value>The collection of imported types.</value>
        /// <remarks>
        /// Use this property to get the imports that will be used by an expression.
        /// </remarks>
        /// <seealso cref="ExpressionImports"/>
        public ExpressionImports Imports => _properties.GetValue<ExpressionImports>("Imports");

        /// <summary>Gets the variables available to an expression</summary>
        /// <value>The VariableCollection instance</value>
        /// <remarks>Use this property to get collection of variables available to an expression.</remarks>
        public VariableCollection Variables => _variables;

        // Called from generated IL (calculation-engine atoms). NoInlining keeps the .NET 10 JIT from
        // inlining it into every compiled expression (R-019).
        /// <summary>Gets the CalculationEngine instance used by the expression.</summary>
        /// <value>
        /// The CalculationEngine instance, or <see langword="null"/> if this context has not been passed to
        /// <see cref="CalcEngine.PublicTypes.CalculationEngine.Add"/>.
        /// </value>
        /// <remarks>Use this property to get CalculationEngine instance used by an expression</remarks>
        public CalculationEngine? CalculationEngine
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            get => _properties.GetValue<CalculationEngine>("CalculationEngine");
        }

        /// <summary>Gets the ExpressionParserOptions for this context.</summary>
        /// <value>The ExpressionParserOptions instance</value>
        /// <remarks>Use this property to customize the expression parser.</remarks>
        public ExpressionParserOptions ParserOptions => _properties.GetValue<ExpressionParserOptions>("ParserOptions");

        #endregion
    }
}
