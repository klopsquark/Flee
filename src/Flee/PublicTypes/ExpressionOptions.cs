#nullable enable
using System.Reflection;
using System.Globalization;
using Flee.InternalTypes;


namespace Flee.PublicTypes
{
    /// <summary>
    /// Allows customization of expression compilation.
    /// </summary>
    /// <remarks>
    /// Use this class when you need to customize how an expression is compiled.  For example: by setting
    /// the <see cref="Checked">Checked</see> property to true,
    /// you can cause all arithmetic and conversion operations to check for overflow.
    /// </remarks>
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
        /// <summary>Gets or sets the type of the expression's result.</summary>
        /// <value>
        /// A <see cref="Type"/> indicating the desired result type.
        /// </value>
        /// <remarks>
        /// Use this property to convert the result of an expression to a particular type.  Essentially, it acts as an implicit conversion from the final
        /// result of the expression to the given type.  When this property is set, the expression will attempt to convert its result to the set value.
        /// If the conversion is invalid, an <see cref="ExpressionCompileException"/> will be thrown.
        /// <para>
        /// <see cref="ExpressionContext.CompileGeneric{TResultType}"/> uses its type argument as the result type, whatever this property holds.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">The property is set to <see langword="null"/>.</exception>
        public Type? ResultType
        {
            get { return _properties.GetValue<Type>("ResultType"); }
            set
            {
                Utility.AssertNotNull(value, "value");
                _properties.SetValue("ResultType", value);
            }
        }

        /// <summary>
        /// Gets or sets whether arithmetic and conversion operations check for overflow.
        /// </summary>
        /// <value>True to emit overflow checks.  False to emit no overflow checks.</value>
        /// <remarks>
        /// Setting this property to true will cause all arithmetic and conversion operations to emit overflow checks.  When
        /// one of those operations is executed and the resultant value cannot fit into the result type, an <see cref="OverflowException">OverflowException</see>
        /// will be thrown.  The default is false.
        /// </remarks>
        public bool Checked
        {
            get { return _properties.GetValue<bool>("Checked"); }
            set { _properties.SetValue("Checked", value); }
        }

        /// <summary>
        /// Gets or sets a value that determines how strings will be compared.
        /// </summary>
        /// <value>The type of string comparison to use.</value>
        /// <remarks>
        /// Use this property to control the type of string comparison used in an expression that compares two strings.  For example: the result of
        /// the expression <c>"string" = "STRING"</c> will be <see langword="true"/> if the string comparison is set to ignore case
        /// and <see langword="false"/> otherwise.  The default is Ordinal.
        /// </remarks>
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
        /// <value>The stored flag.  The compiler never reads it.</value>
        /// <remarks>
        /// The original Flee documentation described this property as saving the expression's IL to the assembly "Expression.dll" on disk, for
        /// inspection with a disassembler.  This version ignores the property: setting it changes nothing about how expressions are compiled.
        /// </remarks>
        [Obsolete("EmitToAssembly has no effect: generated IL cannot be saved to an assembly on this platform.")]
        public bool EmitToAssembly
        {
            get { return _properties.GetValue<bool>("EmitToAssembly"); }
            set { _properties.SetValue("EmitToAssembly", value); }
        }

        /// <summary>
        /// Determines which members on the expression owner are accessible.
        /// </summary>
        /// <value>A combination of BindingFlags that determine which members are accessible.</value>
        /// <remarks>
        /// Using this property, you can control which members on the expression owner are accessible from an expression.  For example: if users
        /// will be inputing expressions, you can prevent private members on the expression owner from being used.  You can use the
        /// <see cref="ExpressionOwnerMemberAccessAttribute"/> attribute on individual members to override the access set using this property.
        /// <para>
        /// Note: the default is to only allow access to public members on the expression owner.  Currently, only the Public and NonPublic values of the BindingFlags enumeration
        /// are used.
        /// </para>
        /// </remarks>
        public BindingFlags OwnerMemberAccess
        {
            get { return _properties.GetValue<BindingFlags>("OwnerMemberAccess"); }
            set { _properties.SetValue("OwnerMemberAccess", value); }
        }

        /// <summary>
        /// Determines how an expression matches member and variable names
        /// </summary>
        /// <value>True to respect case when matching; False to ignore case</value>
        /// <remarks>
        /// Use this property to control how an expression resolves member and variable names.  If set to true, variable and member
        /// names will be matched in a case-sensitive manner.  When false, case will be ignored.  The default is false.
        /// <para>
        /// Changing this property on a context removes all variables from the context's <see cref="ExpressionContext.Variables"/>.
        /// </para>
        /// </remarks>
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

        /// <summary>Gets or sets whether all integer literals are treated as doubles</summary>
        /// <value>True to treat all integer literals as doubles; False to use integers for integral numbers and floating point for real numbers (ie: any number with a decimal point)</value>
        /// <remarks>
        /// Use this property to force all integer literals to doubles.  When set to true, an expression like "1/2" will return 0.5 since both integer
        /// literals are treated as doubles.  When false, the same expression will return 0 since an integer division will be performed.
        /// </remarks>
        public bool IntegersAsDoubles
        {
            get { return _properties.GetValue<bool>("IntegersAsDoubles"); }
            set { _properties.SetValue("IntegersAsDoubles", value); }
        }

        /// <summary>Gets or sets the culture to use when parsing expressions</summary>
        /// <value>The culture to use</value>
        /// <remarks>
        /// Use this property to allow for parsing of expressions using culture-specific tokens.  This is useful, for example, when you
        /// wish to parse numbers using a culture-specific decimal separator.
        /// <para>
        /// The default is the current culture at the time the context is created.  Setting a culture with a different LCID copies its
        /// decimal separator, list separator and short date pattern to <see cref="ExpressionParserOptions.DecimalSeparator"/>,
        /// <see cref="ExpressionParserOptions.FunctionArgumentSeparator"/> and <see cref="ExpressionParserOptions.DateTimeFormat"/>,
        /// and recreates the parser.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">The property is set to <see langword="null"/>.</exception>
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

        /// <summary>Gets or sets the data type used for real literals when no explicit type is specified.</summary>
        /// <value>A value specifying the type to use</value>
        /// <remarks>
        /// Use this property to set the data type that will be used to represent real literals.  For example: if set to Decimal, all real literals (ie: 100.45) will be parsed into
        /// a System.Decimal value.  The default is Double.
        /// </remarks>
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
