using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    internal class FleeILGenerator
    {
        private ILGenerator _ilGenerator;
        private int _length;
        private int _labelCount;
        private readonly Dictionary<Type, LocalBuilder> _localBuilderTemp;
        private int _pass;
        private int _brContext;
        private BranchManager _bm;

        public FleeILGenerator(ILGenerator ilg)
        {
            _ilGenerator = ilg;
            _localBuilderTemp = new Dictionary<Type, LocalBuilder>();
            _length = 0;
            _pass = 1;
            _bm = new BranchManager();
        }

        public int GetTempLocalIndex(Type localType)
        {
            LocalBuilder local = null;

            if (!_localBuilderTemp.TryGetValue(localType, out local))
            {
                local = _ilGenerator.DeclareLocal(localType);
                _localBuilderTemp.Add(localType, local);
            }

            return local.LocalIndex;
        }

        /// <summary>
        /// after first pass, check for long branches.
        /// If any, we need to generate again.
        /// </summary>
        /// <returns></returns>
        public bool NeedsSecondPass()
        {
            return _bm.HasLongBranches();
        }

        /// <summary>
        /// need a new ILGenerator for 2nd pass. This can also
        /// get called for a 3rd pass when emitting to assembly.
        /// </summary>
        /// <param name="ilg"></param>
        public void PrepareSecondPass(ILGenerator ilg)
        {
            _bm.ComputeBranches();
            _localBuilderTemp.Clear();
            _ilGenerator = ilg;
            _length = 0;
            _pass++;
        }

        public void Emit(OpCode op)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op);
        }

        public void Emit(OpCode op, Type arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, ConstructorInfo arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, MethodInfo arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, FieldInfo arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, byte arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, sbyte arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, short arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, int arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, long arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, float arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, double arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, string arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void Emit(OpCode op, Label arg)
        {
            this.RecordOpcode(op);
            _ilGenerator.Emit(op, arg);
        }

        public void EmitBranch(Label arg)
        {
            if (_pass == 1)
            {
                _bm.AddBranch(this, arg);
                Emit(OpCodes.Br_S, arg);
            }
            else if (!_bm.IsLongBranch(this))
            {
                Emit(OpCodes.Br_S, arg);
            }
            else
            {
                Emit(OpCodes.Br, arg);
            }
        }

        public void EmitBranchFalse(Label arg)
        {
            if (_pass == 1)
            {
                _bm.AddBranch(this, arg);
                Emit(OpCodes.Brfalse_S, arg);
            }
            else if (!_bm.IsLongBranch(this))
            {
                Emit(OpCodes.Brfalse_S, arg);
            }
            else
            {
                Emit(OpCodes.Brfalse, arg);
            }
        }

        public void EmitBranchTrue(Label arg)
        {
            if (_pass == 1)
            {
                _bm.AddBranch(this, arg);
                Emit(OpCodes.Brtrue_S, arg);
            }
            else if (!_bm.IsLongBranch(this))
            {
                Emit(OpCodes.Brtrue_S, arg);
            }
            else
            {
                Emit(OpCodes.Brtrue, arg);
            }
        }

        public void MarkLabel(Label lbl)
        {
            _ilGenerator.MarkLabel(lbl);
            _bm.MarkLabel(this, lbl);
        }


        public Label DefineLabel()
        {
            _labelCount += 1;
            var label = _ilGenerator.DefineLabel();
            return label;
        }


        public LocalBuilder DeclareLocal(Type localType)
        {
            return _ilGenerator.DeclareLocal(localType);
        }

        private void RecordOpcode(OpCode op)
        {
            //Trace.WriteLine(String.Format("{0:x}: {1}", MyLength, op.Name))
            int operandLength = GetOpcodeOperandSize(op.OperandType);
            _length += op.Size + operandLength;
        }

        private static int GetOpcodeOperandSize(OperandType operand)
        {
            switch (operand)
            {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                default:
                    Debug.Fail("Unknown operand type");
                    break;
            }
            return 0;
        }

        [Conditional("DEBUG")]
        public void ValidateLength()
        {
            Debug.Assert(this.Length == this.ILGeneratorLength, "ILGenerator length mismatch");
        }

        public int Length => _length;

        public int LabelCount => _labelCount;

        private int ILGeneratorLength => Utility.GetILGeneratorLength(_ilGenerator);
    }
}
