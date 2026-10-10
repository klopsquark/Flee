#nullable enable
using System.Globalization;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Holds settings which enable customization of the expression parser.
    /// </summary>
    /// <remarks>
    /// The parser reads <see cref="DecimalSeparator"/>, <see cref="FunctionArgumentSeparator"/> and
    /// <see cref="RequireDigitsBeforeDecimalPoint"/> when it is created: call <see cref="RecreateParser"/> after changing them.
    /// <see cref="DateTimeFormat"/> is read each time an expression is compiled.
    /// </remarks>
    public class ExpressionParserOptions
    {
        private PropertyDictionary _properties;
        private ExpressionContext _owner;
        private CultureInfo _parseCulture;
        // False in a copy until it changes the culture: every compile copies the options, so the
        // culture is only copied when a copy actually writes to it (D-28).
        private bool _ownsParseCulture = true;

        private NumberStyles NumberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.None;
        internal ExpressionParserOptions(ExpressionContext owner)
        {
            _owner = owner;
            _properties = new PropertyDictionary();
            _parseCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            this.InitializeProperties();
        }

        #region "Methods - Public"

        /// <summary>
        /// Updates the expression parser using the settings provided in this class.
        /// </summary>
        public void RecreateParser()
        {
            _owner.RecreateParser();
        }

        #endregion

        #region "Methods - Internal"

        internal ExpressionParserOptions Clone(ExpressionContext owner)
        {
            ExpressionParserOptions copy = (ExpressionParserOptions)this.MemberwiseClone();
            copy._properties = _properties.Clone();
            // The copy belongs to the new context, so recreating its parser leaves the original alone,
            // and it copies the culture before changing it (D-28).
            copy._owner = owner;
            copy._ownsParseCulture = false;
            _ownsParseCulture = false;
            return copy;
        }

        internal double ParseDouble(string image)
        {
            return double.Parse(image, NumberStyles, _parseCulture);
        }

        internal float ParseSingle(string image)
        {
            return float.Parse(image, NumberStyles, _parseCulture);
        }

        internal decimal ParseDecimal(string image)
        {
            return decimal.Parse(image, NumberStyles, _parseCulture);
        }
        #endregion

        #region "Methods - Private"

        private void InitializeProperties()
        {
            this.DateTimeFormat = "dd/MM/yyyy";
            this.RequireDigitsBeforeDecimalPoint = false;
            this.DecimalSeparator = '.';
            this.FunctionArgumentSeparator = ',';
        }

        #endregion

        #region "Properties - Public"

        /// <summary>Gets or sets the format to use for parsing DateTime literals</summary>
        /// <value>The format to use</value>
        /// <remarks>
        /// Use this property to set the format that will be used to parse DateTime literals.  Expressions which have DateTime literals that are not parseable using this format will fail to compile.
        /// <para>The default is the short date pattern of <see cref="ExpressionOptions.ParseCulture"/>.</para>
        /// </remarks>
        /// <example>When set to "dd-MM-yyyy", an expression such as <c>#04-07-2008#</c> would be parsed to the corresponding DateTime value.</example>
        public string DateTimeFormat
        {
            get { return _properties.GetValue<string>("DateTimeFormat"); }
            set { _properties.SetValue("DateTimeFormat", value); }
        }

        /// <summary>Gets or sets a value that determines if literals for real numbers must have digits before the decimal point.</summary>
        /// <value>True to require digits (ie: 0.56); False otherwise (ie: .56)</value>
        /// <remarks>The default is false.  Call <see cref="RecreateParser"/> after changing this property.</remarks>
        public bool RequireDigitsBeforeDecimalPoint
        {
            get { return _properties.GetValue<bool>("RequireDigitsBeforeDecimalPoint"); }
            set { _properties.SetValue("RequireDigitsBeforeDecimalPoint", value); }
        }

        /// <summary>Gets or sets the character to use as the decimal separator for real number literals.</summary>
        /// <remarks>
        /// The default is the decimal separator of <see cref="ExpressionOptions.ParseCulture"/>.  Call <see cref="RecreateParser"/>
        /// after changing this property.
        /// </remarks>
        public char DecimalSeparator
        {
            get { return _properties.GetValue<char>("DecimalSeparator"); }
            set
            {
                _properties.SetValue("DecimalSeparator", value);
                if (!_ownsParseCulture)
                {
                    _parseCulture = (CultureInfo)_parseCulture.Clone();
                    _ownsParseCulture = true;
                }
                _parseCulture.NumberFormat.NumberDecimalSeparator = value.ToString();
            }
        }

        /// <summary>Gets or sets the character to use to separate function arguments.</summary>
        /// <remarks>
        /// The default is the list separator of <see cref="ExpressionOptions.ParseCulture"/>.  Call <see cref="RecreateParser"/>
        /// after changing this property.
        /// </remarks>
        public char FunctionArgumentSeparator
        {
            get { return _properties.GetValue<char>("FunctionArgumentSeparator"); }
            set { _properties.SetValue("FunctionArgumentSeparator", value); }
        }

        #endregion
    }
}
