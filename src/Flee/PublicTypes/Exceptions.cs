using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public enum CompileExceptionReason
    {
        SyntaxError,
        ConstantOverflow,
        TypeMismatch,
        UndefinedName,
        FunctionHasNoReturnValue,
        InvalidExplicitCast,
        AmbiguousMatch,
        AccessDenied,
        InvalidFormat
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable()]
    public sealed class ExpressionCompileException : Exception
    {
        private readonly CompileExceptionReason _reason;
        internal ExpressionCompileException(string message, CompileExceptionReason reason) : base(message)
        {
            _reason = reason;
        }

        internal ExpressionCompileException(ParserLogException parseException) : base(string.Empty, parseException)
        {
            _reason = CompileExceptionReason.SyntaxError;
        }

        private ExpressionCompileException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
            _reason = (CompileExceptionReason)info.GetInt32("Reason");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Reason", Convert.ToInt32(_reason));
        }

        public override string Message
        {
            get
            {
                if (_reason == CompileExceptionReason.SyntaxError)
                {
                    Exception innerEx = this.InnerException;
                    string msg = $"{Utility.GetCompileErrorMessage(CompileErrorResourceKeys.SyntaxError)}: {innerEx.Message}";
                    return msg;
                }
                else
                {
                    return base.Message;
                }
            }
        }

        public CompileExceptionReason Reason => _reason;
    }
}
