#nullable enable
using Flee.CalcEngine.InternalTypes;
using Flee.CalcEngine.PublicTypes;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public sealed class ExpressionContext
    {

        #region "Fields"

        private PropertyDictionary _properties;

        private readonly object _syncRoot = new object();

        private VariableCollection _variables;
        #endregion

        #region "Constructor"

        public ExpressionContext() : this(DefaultExpressionOwner.Instance)
        {
        }

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
            context._properties.SetValue("Options", context.Options.Clone());
            context._properties.SetValue("ParserOptions", context.ParserOptions.Clone());
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

            parser.Parse();

            return (IdentifierAnalyzer)parser.Analyzer;
        }
        #endregion

        #region "Methods - Public"

        public ExpressionContext Clone()
        {
            return this.CloneInternal(true);
        }

        public IDynamicExpression CompileDynamic(string expression)
        {
            return new Flee.InternalTypes.Expression<object>(expression, this, false);
        }

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
        public ExpressionOptions Options => _properties.GetValue<ExpressionOptions>("Options");

        public ExpressionImports Imports => _properties.GetValue<ExpressionImports>("Imports");

        public VariableCollection Variables => _variables;

        // Called from generated IL (calculation-engine atoms). NoInlining keeps the .NET 10 JIT from
        // inlining it into every compiled expression (R-019).
        public CalculationEngine? CalculationEngine
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            get => _properties.GetValue<CalculationEngine>("CalculationEngine");
        }

        public ExpressionParserOptions ParserOptions => _properties.GetValue<ExpressionParserOptions>("ParserOptions");

        #endregion
    }
}
