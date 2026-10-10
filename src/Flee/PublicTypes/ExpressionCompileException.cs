#nullable enable
using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>
    /// The exception thrown when an expression cannot be compiled.
    /// </summary>
    /// <remarks>
    /// This exception is thrown whenever an expression cannot be compiled.
    /// The <see cref="Reason">Reason</see> property
    /// will contain a value indicating the specific cause of the exception.
    /// </remarks>
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

        /// <summary>
        /// Stores the exception's data, including the <see cref="Reason"/>, for formatter-based serialization.
        /// </summary>
        /// <param name="info">The object that receives the serialized data.</param>
        /// <param name="context">The source and destination of the serialization.</param>
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Reason", Convert.ToInt32(_reason));
        }
#pragma warning restore SYSLIB0051, CS0672

        /// <summary>
        /// Gets the message that describes why compilation failed.
        /// </summary>
        /// <value>
        /// For a <see cref="CompileExceptionReason.SyntaxError"/>, a syntax error prefix followed by the parser's message;
        /// otherwise the message the exception was created with.
        /// </value>
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

        /// <summary>
        /// Gets the reason why compilation failed.
        /// </summary>
        /// <value>A value indicating the cause of the exception</value>
        /// <remarks>Use this property to determine the reason why compilation failed.</remarks>
        public CompileExceptionReason Reason => _reason;
    }
}
