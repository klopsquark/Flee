using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class GenericVariable<T> : IVariable, IGenericVariable<T>
    {


        public object Value;
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
