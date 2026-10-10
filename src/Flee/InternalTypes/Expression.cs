using System.ComponentModel.Design;
using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;
using Flee.Resources;
using IDynamicExpression = Flee.PublicTypes.IDynamicExpression;

namespace Flee.InternalTypes
{
    internal class Expression<T> : IExpression, IDynamicExpression, IGenericExpression<T>
    {
        private readonly string _expression;
        private ExpressionContext _context;
        private ExpressionOptions _options;
        private readonly ExpressionInfo _info;
        private ExpressionEvaluator<T> _evaluator;

        private object _owner;

        private const string DynamicMethodName = "Flee Expression";
        public Expression(string expression, ExpressionContext context, bool isGeneric)
        {
            Utility.AssertNotNull(expression, "expression");
            _expression = expression;
            _owner = context.ExpressionOwner;

            _context = context;

            if (!context.NoClone)
            {
                _context = context.CloneInternal(false);
            }

            _info = new ExpressionInfo();

            this.SetupOptions(_context.Options, isGeneric);

            _context.Imports.ImportOwner(_options.OwnerType);

            this.ValidateOwner(_owner);

            this.Compile(expression, _options);

            _context.CalculationEngine?.FixTemporaryHead(this, _context, _options.ResultType);
        }

        private void SetupOptions(ExpressionOptions options, bool isGeneric)
        {
            // Make sure we clone the options
            _options = options;
            _options.IsGeneric = isGeneric;

            if (isGeneric)
            {
                _options.ResultType = typeof(T);
            }

            _options.SetOwnerType(_owner.GetType());
        }

        private void Compile(string expression, ExpressionOptions options)
        {
            // Add the services that will be used by elements during the compile
            IServiceContainer services = new ServiceContainer();
            this.AddServices(services);

            // Parse and get the root element of the parse tree
            ExpressionElement topElement = _context.Parse(expression, services);

            if (options.ResultType == null)
            {
                options.ResultType = topElement.ResultType;
            }

            RootExpressionElement rootElement = new RootExpressionElement(topElement, options.ResultType);

            DynamicMethod dm = this.CreateDynamicMethod();

            FleeILGenerator ilg = new FleeILGenerator(dm.GetILGenerator());

            // Emit the IL
            rootElement.Emit(ilg, services);
            if (ilg.NeedsSecondPass())
            {
                // second pass required due to long branches.
                dm = this.CreateDynamicMethod();
                ilg.PrepareSecondPass(dm.GetILGenerator());
                rootElement.Emit(ilg, services);
            }

            ilg.ValidateLength();

            Type delegateType = typeof(ExpressionEvaluator<>).MakeGenericType(typeof(T));
            _evaluator = (ExpressionEvaluator<T>)dm.CreateDelegate(delegateType);
        }

        private DynamicMethod CreateDynamicMethod()
        {
            // Create the dynamic method
            Type[] parameterTypes = {
            typeof(object),
            typeof(ExpressionContext),
            typeof(VariableCollection)
        };
            DynamicMethod dm = default(DynamicMethod);

            dm = new DynamicMethod(DynamicMethodName, typeof(T), parameterTypes, _options.OwnerType);

            return dm;
        }

        private void AddServices(IServiceContainer dest)
        {
            dest.AddService(typeof(ExpressionOptions), _options);
            dest.AddService(typeof(ExpressionParserOptions), _context.ParserOptions);
            dest.AddService(typeof(ExpressionContext), _context);
            dest.AddService(typeof(IExpression), this);
            dest.AddService(typeof(ExpressionInfo), _info);
        }

        private void ValidateOwner(object owner)
        {
            Utility.AssertNotNull(owner, "owner");
            if (!_options.OwnerType.IsAssignableFrom(owner.GetType()))
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.NewOwnerTypeNotAssignableToCurrentOwner);
                throw new ArgumentException(msg);
            }
        }

        public object Evaluate()
        {
            return _evaluator(_owner, _context, _context.Variables);
        }

        public T EvaluateGeneric()
        {
            return _evaluator(_owner, _context, _context.Variables);
        }
        T IGenericExpression<T>.Evaluate()
        {
            return EvaluateGeneric();
        }

        public IExpression Clone()
        {
            Expression<T> copy = (Expression<T>)this.MemberwiseClone();
            copy._context = _context.CloneInternal(true);
            copy._options = copy._context.Options;
            return copy;
        }

        public override string ToString()
        {
            return _expression;
        }

        internal Type ResultType => _options.ResultType;

        public string Text => _expression;

        public ExpressionInfo Info1 => _info;

        ExpressionInfo IExpression.Info => Info1;

        public object Owner
        {
            get { return _owner; }
            set
            {
                this.ValidateOwner(value);
                _owner = value;
            }
        }

        public ExpressionContext Context => _context;
    }
}
