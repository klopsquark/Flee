using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class ShortCircuitInfo
    {

        public Stack Operands;
        public Stack Operators;
        private Dictionary<object, Label> Labels;

        public ShortCircuitInfo()
        {
            this.Operands = new Stack();
            this.Operators = new Stack();
            this.Labels = new Dictionary<object, Label>();
        }

        public void ClearTempState()
        {
            this.Operands.Clear();
            this.Operators.Clear();
        }

        public Label AddLabel(object key, Label lbl)
        {
            Labels.Add(key, lbl);
            return lbl;
        }

        public bool HasLabel(object key)
        {
            return Labels.ContainsKey(key);
        }

        public Label FindLabel(object key)
        {
            return Labels[key];
        }
    }
}
