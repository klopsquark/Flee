#nullable enable
using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
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

#pragma warning disable SYSLIB0051, CS0672 // Formatter-based serialization is obsolete on .NET 8+; kept for API compatibility (doc/deferred.md, D-19)
        private ExpressionCompileException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
            _reason = (CompileExceptionReason)info.GetInt32("Reason");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Reason", Convert.ToInt32(_reason));
        }
#pragma warning restore SYSLIB0051, CS0672

        public override string Message
        {
            get
            {
                if (_reason == CompileExceptionReason.SyntaxError)
                {
                    // Only the constructor taking the parser exception sets SyntaxError, and it passes
                    // that exception on as the inner exception.
                    Exception innerEx = this.InnerException!;
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
