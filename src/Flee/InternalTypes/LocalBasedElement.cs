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
    /// Wraps an expression element so that it is loaded from a local slot
    /// </summary>
    internal class LocalBasedElement : ExpressionElement
    {
        private readonly int _index;

        private readonly ExpressionElement _target;
        public LocalBasedElement(ExpressionElement target, int index)
        {
            _target = target;
            _index = index;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Utility.EmitLoadLocal(ilg, _index);
        }

        public override System.Type ResultType => _target.ResultType;
    }
}
