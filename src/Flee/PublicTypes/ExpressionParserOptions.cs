using System.Globalization;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    public class ExpressionParserOptions
    {
        private PropertyDictionary _properties;
        private readonly ExpressionContext _owner;
        private readonly CultureInfo _parseCulture;

        private NumberStyles NumberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.None;
        internal ExpressionParserOptions(ExpressionContext owner)
        {
            _owner = owner;
            _properties = new PropertyDictionary();
            _parseCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            this.InitializeProperties();
        }

        #region "Methods - Public"

        public void RecreateParser()
        {
            _owner.RecreateParser();
        }

        #endregion

        #region "Methods - Internal"

        internal ExpressionParserOptions Clone()
        {
            ExpressionParserOptions copy = (ExpressionParserOptions)this.MemberwiseClone();
            copy._properties = _properties.Clone();
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

        public string DateTimeFormat
        {
            get { return _properties.GetValue<string>("DateTimeFormat"); }
            set { _properties.SetValue("DateTimeFormat", value); }
        }

        public bool RequireDigitsBeforeDecimalPoint
        {
            get { return _properties.GetValue<bool>("RequireDigitsBeforeDecimalPoint"); }
            set { _properties.SetValue("RequireDigitsBeforeDecimalPoint", value); }
        }

        public char DecimalSeparator
        {
            get { return _properties.GetValue<char>("DecimalSeparator"); }
            set
            {
                _properties.SetValue("DecimalSeparator", value);
                _parseCulture.NumberFormat.NumberDecimalSeparator = value.ToString();
            }
        }

        public char FunctionArgumentSeparator
        {
            get { return _properties.GetValue<char>("FunctionArgumentSeparator"); }
            set { _properties.SetValue("FunctionArgumentSeparator", value); }
        }

        #endregion
    }
}
