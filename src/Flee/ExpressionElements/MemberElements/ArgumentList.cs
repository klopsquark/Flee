using System.Collections;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.MemberElements
{
    [Obsolete("Encapsulates an argument list")]
    internal class ArgumentList
    {
        private readonly IList<ExpressionElement> _elements;
        public ArgumentList(ICollection elements)
        {
            ExpressionElement[] arr = new ExpressionElement[elements.Count];
            elements.CopyTo(arr, 0);
            _elements = arr;
        }

        private string[] GetArgumentTypeNames()
        {
            List<string> l = new List<string>();

            foreach (ExpressionElement e in _elements)
            {
                l.Add(e.ResultType.Name);
            }

            return l.ToArray();
        }

        public Type[] GetArgumentTypes()
        {
            List<Type> l = new List<Type>();

            foreach (ExpressionElement e in _elements)
            {
                l.Add(e.ResultType);
            }

            return l.ToArray();
        }

        public override string ToString()
        {
            string[] typeNames = this.GetArgumentTypeNames();
            return Utility.FormatList(typeNames);
        }

        public ExpressionElement[] ToArray()
        {
            ExpressionElement[] arr = new ExpressionElement[_elements.Count];
            _elements.CopyTo(arr, 0);
            return arr;
        }

        public ExpressionElement this[int index] => _elements[index];

        public int Count => _elements.Count;
    }
}
