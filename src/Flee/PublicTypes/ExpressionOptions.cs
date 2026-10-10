#nullable enable
using System.Reflection;
using System.Globalization;
using Flee.InternalTypes;


namespace Flee.PublicTypes
{
    public sealed class ExpressionOptions
    {

        private PropertyDictionary _properties;
        private Type? _ownerType;
        private readonly ExpressionContext _owner;
        internal event EventHandler? CaseSensitiveChanged;

        internal ExpressionOptions(ExpressionContext owner)
        {
            _owner = owner;
            _properties = new PropertyDictionary();

            this.InitializeProperties();
        }

        #region "Methods - Private"

        private void InitializeProperties()
        {
            this.StringComparison = System.StringComparison.Ordinal;
            this.OwnerMemberAccess = BindingFlags.Public;

            _properties.SetToDefault<bool>("CaseSensitive");
            _properties.SetToDefault<bool>("Checked");
            _properties.SetToDefault<bool>("EmitToAssembly");
            _properties.SetToDefault<Type>("ResultType");
            _properties.SetToDefault<bool>("IsGeneric");
            _properties.SetToDefault<bool>("IntegersAsDoubles");
            _properties.SetValue("ParseCulture", CultureInfo.CurrentCulture);
            this.SetParseCulture(this.ParseCulture);
            _properties.SetValue("RealLiteralDataType", RealLiteralDataType.Double);
        }

        private void SetParseCulture(CultureInfo ci)
        {
            ExpressionParserOptions po = _owner.ParserOptions;
            po.DecimalSeparator = Convert.ToChar(ci.NumberFormat.NumberDecimalSeparator);
            po.FunctionArgumentSeparator = Convert.ToChar(ci.TextInfo.ListSeparator);
            po.DateTimeFormat = ci.DateTimeFormat.ShortDatePattern;
        }

        #endregion

        #region "Methods - Internal"

        internal ExpressionOptions Clone()
        {
            ExpressionOptions clonedOptions = (ExpressionOptions)this.MemberwiseClone();
            clonedOptions._properties = _properties.Clone();
            return clonedOptions;
        }

        internal bool IsOwnerType(Type? t)
        {
            // The expression sets the owner type before it compiles, and only the compiler calls this.
            return this._ownerType!.IsAssignableFrom(t);
        }

        internal void SetOwnerType(Type ownerType)
        {
            _ownerType = ownerType;
        }

        #endregion

        #region "Properties - Public"
        public Type? ResultType
        {
            get { return _properties.GetValue<Type>("ResultType"); }
            set
            {
                Utility.AssertNotNull(value, "value");
                _properties.SetValue("ResultType", value);
            }
        }

        public bool Checked
        {
            get { return _properties.GetValue<bool>("Checked"); }
            set { _properties.SetValue("Checked", value); }
        }

        public StringComparison StringComparison
        {
            get { return _properties.GetValue<StringComparison>("StringComparison"); }
            set { _properties.SetValue("StringComparison", value); }
        }

        /// <summary>
        /// Has no effect. Upstream Flee emitted the IL a second time into an in-memory assembly
        /// that was never saved, because .NET Core cannot save dynamic assemblies. Kept so that
        /// existing code still compiles.
        /// </summary>
        [Obsolete("EmitToAssembly has no effect: generated IL cannot be saved to an assembly on this platform.")]
        public bool EmitToAssembly
        {
            get { return _properties.GetValue<bool>("EmitToAssembly"); }
            set { _properties.SetValue("EmitToAssembly", value); }
        }

        public BindingFlags OwnerMemberAccess
        {
            get { return _properties.GetValue<BindingFlags>("OwnerMemberAccess"); }
            set { _properties.SetValue("OwnerMemberAccess", value); }
        }

        public bool CaseSensitive
        {
            get { return _properties.GetValue<bool>("CaseSensitive"); }
            set
            {
                if (this.CaseSensitive != value)
                {
                    _properties.SetValue("CaseSensitive", value);
                    if (CaseSensitiveChanged != null)
                    {
                        CaseSensitiveChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool IntegersAsDoubles
        {
            get { return _properties.GetValue<bool>("IntegersAsDoubles"); }
            set { _properties.SetValue("IntegersAsDoubles", value); }
        }

        public CultureInfo ParseCulture
        {
            get { return _properties.GetValue<CultureInfo>("ParseCulture"); }
            set
            {
                Utility.AssertNotNull(value, "ParseCulture");
                if ((value.LCID != this.ParseCulture.LCID))
                {
                    _properties.SetValue("ParseCulture", value);
                    this.SetParseCulture(value);
                    _owner.ParserOptions.RecreateParser();
                }
            }
        }

        public RealLiteralDataType RealLiteralDataType
        {
            get { return _properties.GetValue<RealLiteralDataType>("RealLiteralDataType"); }
            set { _properties.SetValue("RealLiteralDataType", value); }
        }
        #endregion

        #region "Properties - Non Public"
        internal IEqualityComparer<string> StringComparer
        {
            get
            {
                if (this.CaseSensitive)
                {
                    return System.StringComparer.Ordinal;
                }
                else
                {
                    return System.StringComparer.OrdinalIgnoreCase;
                }
            }
        }

        internal MemberFilter MemberFilter
        {
            get
            {
                if (this.CaseSensitive)
                {
                    return Type.FilterName;
                }
                else
                {
                    return Type.FilterNameIgnoreCase;
                }
            }
        }

        internal StringComparison MemberStringComparison
        {
            get
            {
                if (this.CaseSensitive)
                {
                    return System.StringComparison.Ordinal;
                }
                else
                {
                    return System.StringComparison.OrdinalIgnoreCase;
                }
            }
        }

        internal Type? OwnerType => _ownerType;

        internal bool IsGeneric
        {
            get { return _properties.GetValue<bool>("IsGeneric"); }
            set { _properties.SetValue("IsGeneric", value); }
        }
        #endregion
    }
}
