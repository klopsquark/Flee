using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Base.Literals;
using Flee.ExpressionElements.Literals.Integral;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.ExpressionElements
{
    internal class ArithmeticElement : BinaryExpressionElement
    {
        // Looked up once per process, not in every constructor call (R-033).
        private static readonly MethodInfo PowerMethodInfo = typeof(Math).GetMethod("Pow", BindingFlags.Public | BindingFlags.Static);
        private static readonly MethodInfo StringConcatMethodInfo = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }, null);
        private static readonly MethodInfo ObjectConcatMethodInfo = typeof(string).GetMethod("Concat", new Type[] { typeof(object), typeof(object) }, null);
        private BinaryArithmeticOperation _operation;

        protected override void GetOperation(object operation)
        {
            _operation = (BinaryArithmeticOperation)operation;
        }

        protected override System.Type GetResultType(System.Type leftType, System.Type rightType)
        {
            Type binaryResultType = ImplicitConverter.GetBinaryResultType(leftType, rightType);
            MethodInfo overloadedMethod = this.GetOverloadedArithmeticOperator();

            // Is an overloaded operator defined for our left and right children?
            if ((overloadedMethod != null))
            {
                // Yes, so use its return type
                return overloadedMethod.ReturnType;
            }
            else if ((binaryResultType != null))
            {
                // Operands are primitive types.  Return computed result type unless we are doing a power operation
                if (_operation == BinaryArithmeticOperation.Power)
                {
                    return this.GetPowerResultType(leftType, rightType, binaryResultType);
                }
                else
                {
                    return binaryResultType;
                }
            }
            else if (this.IsEitherChildOfType(typeof(string)) && (_operation == BinaryArithmeticOperation.Add))
            {
                // String concatenation
                return typeof(string);
            }
            else
            {
                // Invalid types
                return null;
            }
        }

        private Type GetPowerResultType(Type leftType, Type rightType, Type binaryResultType)
        {
            if (this.IsOptimizablePower)
            {
                return leftType;
            }
            else
            {
                return typeof(double);
            }
        }

        private MethodInfo GetOverloadedArithmeticOperator()
        {
            // Get the name of the operator
            string name = GetOverloadedOperatorFunctionName(_operation);
            return base.GetOverloadedBinaryOperator(name, _operation);
        }

        private static string GetOverloadedOperatorFunctionName(BinaryArithmeticOperation op)
        {
            switch (op)
            {
                case BinaryArithmeticOperation.Add:
                    return "Addition";
                case BinaryArithmeticOperation.Subtract:
                    return "Subtraction";
                case BinaryArithmeticOperation.Multiply:
                    return "Multiply";
                case BinaryArithmeticOperation.Divide:
                    return "Division";
                case BinaryArithmeticOperation.Mod:
                    return "Modulus";
                case BinaryArithmeticOperation.Power:
                    return "Exponent";
                default:
                    throw new InvalidOperationException("Flee internal error: unknown operator type");
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            MethodInfo overloadedMethod = this.GetOverloadedArithmeticOperator();

            if ((overloadedMethod != null))
            {
                // Emit a call to an overloaded operator
                this.EmitOverloadedOperatorCall(overloadedMethod, ilg, services);
            }
            else if (this.IsEitherChildOfType(typeof(string)))
            {
                // One of our operands is a string so emit a concatenation
                this.EmitStringConcat(ilg, services);
            }
            else
            {
                // Emit a regular arithmetic operation			
                EmitArithmeticOperation(_operation, ilg, services);
            }
        }

        private static bool IsUnsignedForArithmetic(Type t)
        {
            return object.ReferenceEquals(t, typeof(UInt32)) || object.ReferenceEquals(t, typeof(UInt64));
        }

        /// <summary>
        /// Emit an arithmetic operation with handling for unsigned and checked contexts
        /// </summary>
        /// <param name="op"></param>
        /// <param name="ilg"></param>
        /// <param name="services"></param>
        private void EmitArithmeticOperation(BinaryArithmeticOperation op, FleeILGenerator ilg, IServiceProvider services)
        {
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
            bool unsigned = IsUnsignedForArithmetic(LeftChild.ResultType) && IsUnsignedForArithmetic(RightChild.ResultType);
            bool integral = Utility.IsIntegralType(LeftChild.ResultType) && Utility.IsIntegralType(RightChild.ResultType);
            bool emitOverflow = integral && options.Checked;

            EmitChildWithConvert(LeftChild, this.ResultType, ilg, services);

            if (!this.IsOptimizablePower)
            {
                EmitChildWithConvert(RightChild, this.ResultType, ilg, services);
            }

            switch (op)
            {
                case BinaryArithmeticOperation.Add:
                    if (emitOverflow)
                    {
                        if (unsigned)
                        {
                            ilg.Emit(OpCodes.Add_Ovf_Un);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Add_Ovf);
                        }
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Add);
                    }
                    break;
                case BinaryArithmeticOperation.Subtract:
                    if (emitOverflow)
                    {
                        if (unsigned)
                        {
                            ilg.Emit(OpCodes.Sub_Ovf_Un);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Sub_Ovf);
                        }
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                    break;
                case BinaryArithmeticOperation.Multiply:
                    this.EmitMultiply(ilg, emitOverflow, unsigned);
                    break;
                case BinaryArithmeticOperation.Divide:
                    if (unsigned)
                    {
                        ilg.Emit(OpCodes.Div_Un);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Div);
                    }
                    break;
                case BinaryArithmeticOperation.Mod:
                    if (unsigned)
                    {
                        ilg.Emit(OpCodes.Rem_Un);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Rem);
                    }
                    break;
                case BinaryArithmeticOperation.Power:
                    this.EmitPower(ilg, emitOverflow, unsigned);
                    break;
                default:
                    throw new InvalidOperationException("Flee internal error: unknown arithmetic operation");
            }
        }

        private void EmitPower(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
        {
            if (this.IsOptimizablePower)
            {
                this.EmitOptimizedPower(ilg, emitOverflow, unsigned);
            }
            else
            {
                ilg.Emit(OpCodes.Call, PowerMethodInfo);
            }
        }

        private void EmitOptimizedPower(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
        {
            Int32LiteralElement right = (Int32LiteralElement)RightChild;

            if (right.Value == 0)
            {
                ilg.Emit(OpCodes.Pop);
                IntegralLiteralElement.EmitLoad(1, ilg);
                ImplicitConverter.EmitImplicitNumericConvert(typeof(Int32), LeftChild.ResultType, ilg);
                return;
            }

            if (right.Value == 1)
            {
                return;
            }

            // Start at 1 since left operand has already been emited once
            for (int i = 1; i <= right.Value - 1; i++)
            {
                ilg.Emit(OpCodes.Dup);
            }

            for (int i = 1; i <= right.Value - 1; i++)
            {
                this.EmitMultiply(ilg, emitOverflow, unsigned);
            }
        }

        private void EmitMultiply(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
        {
            if (emitOverflow)
            {
                if (unsigned)
                {
                    ilg.Emit(OpCodes.Mul_Ovf_Un);
                }
                else
                {
                    ilg.Emit(OpCodes.Mul_Ovf);
                }
            }
            else
            {
                ilg.Emit(OpCodes.Mul);
            }
        }

        /// <summary>
        /// Emit a string concatenation
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="services"></param>
        private void EmitStringConcat(FleeILGenerator ilg, IServiceProvider services)
        {
            Type argType = default(Type);
            System.Reflection.MethodInfo concatMethodInfo = default(System.Reflection.MethodInfo);

            // Pick the most specific concat method
            if (this.AreBothChildrenOfType(typeof(string)))
            {
                concatMethodInfo = StringConcatMethodInfo;
                argType = typeof(string);
            }
            else
            {
                Debug.Assert(this.IsEitherChildOfType(typeof(string)), "one child must be a string");
                concatMethodInfo = ObjectConcatMethodInfo;
                argType = typeof(object);
            }

            // Emit the operands and call the function
            LeftChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(LeftChild.ResultType, argType, ilg);
            RightChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(RightChild.ResultType, argType, ilg);
            ilg.Emit(OpCodes.Call, concatMethodInfo);
        }

        private bool IsOptimizablePower
        {
            get
            {
                if (_operation != BinaryArithmeticOperation.Power || !(RightChild is Int32LiteralElement))
                {
                    return false;
                }

                Int32LiteralElement right = (Int32LiteralElement)RightChild;

                return right?.Value >= 0;
            }
        }
    }
}
