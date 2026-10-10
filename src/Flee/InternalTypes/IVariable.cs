using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal interface IVariable
    {
        IVariable Clone();
        Type VariableType { get; }
        object ValueAsObject { get; set; }
    }
}
