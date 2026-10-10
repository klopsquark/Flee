#nullable enable
using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Defines values to indicate why compilation of an expression failed.
    /// </summary>
    /// <remarks>
    /// When compilation of an expression fails, an <see cref="ExpressionCompileException"/> will be thrown.
    /// The <see cref="ExpressionCompileException.Reason">Reason</see> property
    /// on the exception will contain a value from this enumeration.  You can use that value to determine how to handle the exception.
    /// For example: if the Reason is a SyntaxError, you can display an error message tailored to syntax errors.
    /// </remarks>
    public enum CompileExceptionReason
    {
        /// <summary>The expression text is not parsable because it does not meet the syntax rules of the expression grammar.</summary>
        SyntaxError,
        /// <summary>A constant expression cannot be represented in its type.</summary>
        ConstantOverflow,
        /// <summary>The operation is invalid for the given type.</summary>
        TypeMismatch,
        /// <summary>The expression references a name that cannot be resolved.</summary>
        UndefinedName,
        /// <summary>The expression calls a function that does not return a value.</summary>
        FunctionHasNoReturnValue,
        /// <summary>The requested explicit cast is not valid for the given types.</summary>
        InvalidExplicitCast,
        /// <summary>More than one member matches the required criteria.</summary>
        AmbiguousMatch,
        /// <summary>Access to the specified member is not allowed.</summary>
        AccessDenied,
        /// <summary>The given value is not in the required format.</summary>
        InvalidFormat
    }
}
