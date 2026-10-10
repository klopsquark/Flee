using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class GenericVariable<T> : IVariable, IGenericVariable<T>
    {
        // Starts as the type's default, so a variable created by DefineVariable reads like one set
        // to null through the indexer (D-27).
        public object Value = default(T);

        public IVariable Clone()
        {
            GenericVariable<T> copy = new GenericVariable<T> { Value = Value };
            return copy;
        }

        public object GetValue()
        {
            return Value;
        }

        public System.Type VariableType => typeof(T);

        public object ValueAsObject
        {
            get { return Value; }
            set
            {
                if (value == null)
                {
                    Value = default(T);
                }
                else
                {
                    Value = value;
                }
            }
        }
    }
}
