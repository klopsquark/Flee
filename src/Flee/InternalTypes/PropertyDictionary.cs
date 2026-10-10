using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Helper class for storing strongly-typed properties
    /// </summary>
    internal class PropertyDictionary
    {
        private readonly Dictionary<string, object> _properties;
        public PropertyDictionary()
        {
            _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public PropertyDictionary Clone()
        {
            PropertyDictionary copy = new PropertyDictionary();

            foreach (KeyValuePair<string, object> pair in _properties)
            {
                copy.SetValue(pair.Key, pair.Value);
            }

            return copy;
        }

        public T GetValue<T>(string name)
        {
            object value = default(T);
            if (!_properties.TryGetValue(name, out value))
            {
                Debug.Fail($"Unknown property '{name}'");
            }
            return (T)value;
        }

        public void SetToDefault<T>(string name)
        {
            T value = default(T);
            this.SetValue(name, value);
        }

        public void SetValue(string name, object value)
        {
            _properties[name] = value;
        }

        public bool Contains(string name)
        {
            return _properties.ContainsKey(name);
        }
    }
}
