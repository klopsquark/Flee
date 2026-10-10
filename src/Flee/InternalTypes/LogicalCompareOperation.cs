using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal enum LogicalCompareOperation
    {
        LessThan,
        GreaterThan,
        Equal,
        NotEqual,
        LessThanOrEqual,
        GreaterThanOrEqual
    }
}
