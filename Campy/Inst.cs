using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campy.Utils;

namespace Campy
{
    public class Inst
    {
        protected Mono.Cecil.Cil.Instruction _instruction;
        protected int _stack_level_in;
        protected int _stack_level_out;
        
        protected State _state_in;
        protected State _state_out;


        protected static List<Inst> _call_instructions = new List<Inst>();

        public static List<Inst> CallInstructions
        {
            get { return _call_instructions; }
        }

        protected CFG _graph;

        public CFG.CFGVertex Block
        {
            get {
                return _graph.partition_of_instructions[this._instruction];
            }
        }

        public Mono.Cecil.Cil.Instruction Instruction
        {
            get
            {
                return _instruction;
            }
        }

        public int StackLevelIn
        {
            get
            {
                return _stack_level_in;
            }
            set
            {
                _stack_level_in = value;
            }
        }

        public int StackLevelOut
        {
            get
            {
                return _stack_level_out;
            }
            set
            {
                _stack_level_out = value;
            }
        }

        public State StateIn
        {
            get
            {
                return _state_in;
            }
            set
            {
                _state_in = value;
            }
        }

        public State StateOut
        {
            get
            {
                return _state_out;
            }
            set
            {
                _state_out = value;
            }
        }

        public override string ToString()
        {
            return _instruction.ToString();
        }

        public Inst(Mono.Cecil.Cil.Instruction i, CFG graph)
        {
            _instruction = i;
            _stack_level_in = 0;
            _stack_level_out = 0;
            _graph = graph;
            if (i.OpCode.FlowControl == Mono.Cecil.Cil.FlowControl.Call)
            {
                Inst._call_instructions.Add(this);
            }
        }

        public Mono.Cecil.Cil.OpCode OpCode
        {
            get
            {
                return _instruction.OpCode;
            }
        }

        public object Operand
        {
            get
            {
                return _instruction.Operand;
            }
        }

        virtual public void Execute(ref State state)
        {
        }

        virtual public void ComputeStackLevel(ref int level_after)
        {
        }

        virtual public void ComputeSSA(ref State state)
        {
        }

        static public Inst Wrap(Mono.Cecil.Cil.Instruction i, CFG g)
        {
            // Wrap instruction with semantics, def/use/kill properties.
            Mono.Cecil.Cil.OpCode op = i.OpCode;
            switch (op.Code)
            {
                case Mono.Cecil.Cil.Code.Add:
                    return new i_add(i, g);
                case Mono.Cecil.Cil.Code.Add_Ovf:
                    return new i_add_ovf(i, g);
                case Mono.Cecil.Cil.Code.Add_Ovf_Un:
                    return new i_add_ovf_un(i, g);
                case Mono.Cecil.Cil.Code.And:
                    return new i_and(i, g);
                case Mono.Cecil.Cil.Code.Arglist:
                    return new i_arglist(i, g);
                case Mono.Cecil.Cil.Code.Beq:
                    return new i_beq(i, g);
                case Mono.Cecil.Cil.Code.Beq_S:
                    return new i_beq(i, g);
                case Mono.Cecil.Cil.Code.Bge:
                    return new i_bge(i, g);
                case Mono.Cecil.Cil.Code.Bge_S:
                    return new i_bge_s(i, g);
                case Mono.Cecil.Cil.Code.Bge_Un:
                    return new i_bge_un(i, g);
                case Mono.Cecil.Cil.Code.Bge_Un_S:
                    return new i_bge_un_s(i, g);
                case Mono.Cecil.Cil.Code.Bgt:
                    return new i_bgt(i, g);
                case Mono.Cecil.Cil.Code.Bgt_S:
                    return new i_bgt_s(i, g);
                case Mono.Cecil.Cil.Code.Bgt_Un:
                    return new i_bgt_un(i, g);
                case Mono.Cecil.Cil.Code.Bgt_Un_S:
                    return new i_bgt_un_s(i, g);
                case Mono.Cecil.Cil.Code.Ble:
                    return new i_ble(i, g);
                case Mono.Cecil.Cil.Code.Ble_S:
                    return new i_ble_s(i, g);
                case Mono.Cecil.Cil.Code.Ble_Un:
                    return new i_ble_un(i, g);
                case Mono.Cecil.Cil.Code.Ble_Un_S:
                    return new i_ble_un_s(i, g);
                case Mono.Cecil.Cil.Code.Blt:
                    return new i_blt(i, g);
                case Mono.Cecil.Cil.Code.Blt_S:
                    return new i_blt_s(i, g);
                case Mono.Cecil.Cil.Code.Blt_Un:
                    return new i_blt_un(i, g);
                case Mono.Cecil.Cil.Code.Blt_Un_S:
                    return new i_blt_un_s(i, g);
                case Mono.Cecil.Cil.Code.Bne_Un:
                    return new i_bne_un(i, g);
                case Mono.Cecil.Cil.Code.Bne_Un_S:
                    return new i_bne_un_s(i, g);
                case Mono.Cecil.Cil.Code.Box:
                    return new i_box(i, g);
                case Mono.Cecil.Cil.Code.Br:
                    return new i_br(i, g);
                case Mono.Cecil.Cil.Code.Br_S:
                    return new i_br_s(i, g);
                case Mono.Cecil.Cil.Code.Break:
                    return new i_break(i, g);
                case Mono.Cecil.Cil.Code.Brfalse:
                    return new i_brfalse(i, g);
                case Mono.Cecil.Cil.Code.Brfalse_S:
                    return new i_brfalse_s(i, g);
                // Missing brnull
                // Missing brzero
                case Mono.Cecil.Cil.Code.Brtrue:
                    return new i_brtrue(i, g);
                case Mono.Cecil.Cil.Code.Brtrue_S:
                    return new i_brtrue_s(i, g);
                case Mono.Cecil.Cil.Code.Call:
                    return new i_call(i, g);
                case Mono.Cecil.Cil.Code.Calli:
                    return new i_calli(i, g);
                case Mono.Cecil.Cil.Code.Callvirt:
                    return new i_callvirt(i, g);
                case Mono.Cecil.Cil.Code.Castclass:
                    return new i_castclass(i, g);
                case Mono.Cecil.Cil.Code.Ceq:
                    return new i_ceq(i, g);
                case Mono.Cecil.Cil.Code.Cgt:
                    return new i_cgt(i, g);
                case Mono.Cecil.Cil.Code.Cgt_Un:
                    return new i_cgt_un(i, g);
                case Mono.Cecil.Cil.Code.Ckfinite:
                    return new i_ckfinite(i, g);
                case Mono.Cecil.Cil.Code.Clt:
                    return new i_clt(i, g);
                case Mono.Cecil.Cil.Code.Clt_Un:
                    return new i_clt_un(i, g);
                case Mono.Cecil.Cil.Code.Constrained:
                    return new i_constrained(i, g);
                case Mono.Cecil.Cil.Code.Conv_I1:
                    return new i_conv_i1(i, g);
                case Mono.Cecil.Cil.Code.Conv_I2:
                    return new i_conv_i2(i, g);
                case Mono.Cecil.Cil.Code.Conv_I4:
                    return new i_conv_i4(i, g);
                case Mono.Cecil.Cil.Code.Conv_I8:
                    return new i_conv_i8(i, g);
                case Mono.Cecil.Cil.Code.Conv_I:
                    return new i_conv_i(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I1:
                    return new i_conv_ovf_i1(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I1_Un:
                    return new i_conv_ovf_i1_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I2:
                    return new i_conv_ovf_i2(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I2_Un:
                    return new i_conv_ovf_i2_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I4:
                    return new i_conv_ovf_i4(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I4_Un:
                    return new i_conv_ovf_i4_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I8:
                    return new i_conv_ovf_i8(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I8_Un:
                    return new i_conv_ovf_i8_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I:
                    return new i_conv_ovf_i(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I_Un:
                    return new i_conv_ovf_i_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U1:
                    return new i_conv_ovf_u1(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U1_Un:
                    return new i_conv_ovf_u1_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U2:
                    return new i_conv_ovf_u2(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U2_Un:
                    return new i_conv_ovf_u2_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U4:
                    return new i_conv_ovf_u4(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U4_Un:
                    return new i_conv_ovf_u4_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U8:
                    return new i_conv_ovf_u8(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U8_Un:
                    return new i_conv_ovf_u8_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U:
                    return new i_conv_ovf_u(i, g);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U_Un:
                    return new i_conv_ovf_u_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_R4:
                    return new i_conv_r4(i, g);
                case Mono.Cecil.Cil.Code.Conv_R8:
                    return new i_conv_r8(i, g);
                case Mono.Cecil.Cil.Code.Conv_R_Un:
                    return new i_conv_r_un(i, g);
                case Mono.Cecil.Cil.Code.Conv_U1:
                    return new i_conv_u1(i, g);
                case Mono.Cecil.Cil.Code.Conv_U2:
                    return new i_conv_u2(i, g);
                case Mono.Cecil.Cil.Code.Conv_U4:
                    return new i_conv_u4(i, g);
                case Mono.Cecil.Cil.Code.Conv_U8:
                    return new i_conv_u8(i, g);
                case Mono.Cecil.Cil.Code.Conv_U:
                    return new i_conv_u(i, g);
                case Mono.Cecil.Cil.Code.Cpblk:
                    return new i_cpblk(i, g);
                case Mono.Cecil.Cil.Code.Cpobj:
                    return new i_cpobj(i, g);
                case Mono.Cecil.Cil.Code.Div:
                    return new i_div(i, g);
                case Mono.Cecil.Cil.Code.Div_Un:
                    return new i_div_un(i, g);
                case Mono.Cecil.Cil.Code.Dup:
                    return new i_dup(i, g);
                case Mono.Cecil.Cil.Code.Endfilter:
                    return new i_endfilter(i, g);
                case Mono.Cecil.Cil.Code.Endfinally:
                    return new i_endfinally(i, g);
                case Mono.Cecil.Cil.Code.Initblk:
                    return new i_initblk(i, g);
                case Mono.Cecil.Cil.Code.Initobj:
                    return new i_initobj(i, g);
                case Mono.Cecil.Cil.Code.Isinst:
                    return new i_isinst(i, g);
                case Mono.Cecil.Cil.Code.Jmp:
                    return new i_jmp(i, g);
                case Mono.Cecil.Cil.Code.Ldarg:
                    return new i_ldarg(i, g);
                case Mono.Cecil.Cil.Code.Ldarg_0:
                    return new i_ldarg_0(i, g);
                case Mono.Cecil.Cil.Code.Ldarg_1:
                    return new i_ldarg_1(i, g);
                case Mono.Cecil.Cil.Code.Ldarg_2:
                    return new i_ldarg_2(i, g);
                case Mono.Cecil.Cil.Code.Ldarg_3:
                    return new i_ldarg_3(i, g);
                case Mono.Cecil.Cil.Code.Ldarg_S:
                    return new i_ldarg_s(i, g);
                case Mono.Cecil.Cil.Code.Ldarga:
                    return new i_ldarga(i, g);
                case Mono.Cecil.Cil.Code.Ldarga_S:
                    return new i_ldarga_s(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4:
                    return new i_ldc_i4(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_0:
                    return new i_ldc_i4_0(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_1:
                    return new i_ldc_i4_1(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_2:
                    return new i_ldc_i4_2(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_3:
                    return new i_ldc_i4_3(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_4:
                    return new i_ldc_i4_4(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_5:
                    return new i_ldc_i4_5(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_6:
                    return new i_ldc_i4_6(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_7:
                    return new i_ldc_i4_7(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_8:
                    return new i_ldc_i4_8(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_M1:
                    return new i_ldc_i4_m1(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I4_S:
                    return new i_ldc_i4_s(i, g);
                case Mono.Cecil.Cil.Code.Ldc_I8:
                    return new i_ldc_i8(i, g);
                case Mono.Cecil.Cil.Code.Ldc_R4:
                    return new i_ldc_r4(i, g);
                case Mono.Cecil.Cil.Code.Ldc_R8:
                    return new i_ldc_r8(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_Any:
                    return new i_ldelem_any(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_I1:
                    return new i_ldelem_i1(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_I2:
                    return new i_ldelem_i2(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_I4:
                    return new i_ldelem_i4(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_I8:
                    return new i_ldelem_i8(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_I:
                    return new i_ldelem_i(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_R4:
                    return new i_ldelem_r4(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_R8:
                    return new i_ldelem_r8(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_Ref:
                    return new i_ldelem_ref(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_U1:
                    return new i_ldelem_u1(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_U2:
                    return new i_ldelem_u2(i, g);
                case Mono.Cecil.Cil.Code.Ldelem_U4:
                    return new i_ldelem_u4(i, g);
                case Mono.Cecil.Cil.Code.Ldelema:
                    return new i_ldelema(i, g);
                case Mono.Cecil.Cil.Code.Ldfld:
                    return new i_ldfld(i, g);
                case Mono.Cecil.Cil.Code.Ldflda:
                    return new i_ldflda(i, g);
                case Mono.Cecil.Cil.Code.Ldftn:
                    return new i_ldftn(i, g);
                case Mono.Cecil.Cil.Code.Ldind_I1:
                    return new i_ldind_i1(i, g);
                case Mono.Cecil.Cil.Code.Ldind_I2:
                    return new i_ldind_i2(i, g);
                case Mono.Cecil.Cil.Code.Ldind_I4:
                    return new i_ldind_i4(i, g);
                case Mono.Cecil.Cil.Code.Ldind_I8:
                    return new i_ldind_i8(i, g);
                case Mono.Cecil.Cil.Code.Ldind_I:
                    return new i_ldind_i(i, g);
                case Mono.Cecil.Cil.Code.Ldind_R4:
                    return new i_ldind_r4(i, g);
                case Mono.Cecil.Cil.Code.Ldind_R8:
                    return new i_ldind_r8(i, g);
                case Mono.Cecil.Cil.Code.Ldind_Ref:
                    return new i_ldind_ref(i, g);
                case Mono.Cecil.Cil.Code.Ldind_U1:
                    return new i_ldind_u1(i, g);
                case Mono.Cecil.Cil.Code.Ldind_U2:
                    return new i_ldind_u2(i, g);
                case Mono.Cecil.Cil.Code.Ldind_U4:
                    return new i_ldind_u4(i, g);
                case Mono.Cecil.Cil.Code.Ldlen:
                    return new i_ldlen(i, g);
                case Mono.Cecil.Cil.Code.Ldloc:
                    return new i_ldloc(i, g);
                case Mono.Cecil.Cil.Code.Ldloc_0:
                    return new i_ldloc_0(i, g);
                case Mono.Cecil.Cil.Code.Ldloc_1:
                    return new i_ldloc_1(i, g);
                case Mono.Cecil.Cil.Code.Ldloc_2:
                    return new i_ldloc_2(i, g);
                case Mono.Cecil.Cil.Code.Ldloc_3:
                    return new i_ldloc_3(i, g);
                case Mono.Cecil.Cil.Code.Ldloc_S:
                    return new i_ldloc_s(i, g);
                case Mono.Cecil.Cil.Code.Ldloca:
                    return new i_ldloca(i, g);
                case Mono.Cecil.Cil.Code.Ldloca_S:
                    return new i_ldloca_s(i, g);
                case Mono.Cecil.Cil.Code.Ldnull:
                    return new i_ldnull(i, g);
                case Mono.Cecil.Cil.Code.Ldobj:
                    return new i_ldobj(i, g);
                case Mono.Cecil.Cil.Code.Ldsfld:
                    return new i_ldsfld(i, g);
                case Mono.Cecil.Cil.Code.Ldsflda:
                    return new i_ldsflda(i, g);
                case Mono.Cecil.Cil.Code.Ldstr:
                    return new i_ldstr(i, g);
                case Mono.Cecil.Cil.Code.Ldtoken:
                    return new i_ldtoken(i, g);
                case Mono.Cecil.Cil.Code.Ldvirtftn:
                    return new i_ldvirtftn(i, g);
                case Mono.Cecil.Cil.Code.Leave:
                    return new i_leave(i, g);
                case Mono.Cecil.Cil.Code.Leave_S:
                    return new i_leave_s(i, g);
                case Mono.Cecil.Cil.Code.Localloc:
                    return new i_localloc(i, g);
                case Mono.Cecil.Cil.Code.Mkrefany:
                    return new i_mkrefany(i, g);
                case Mono.Cecil.Cil.Code.Mul:
                    return new i_mul(i, g);
                case Mono.Cecil.Cil.Code.Mul_Ovf:
                    return new i_mul_ovf(i, g);
                case Mono.Cecil.Cil.Code.Mul_Ovf_Un:
                    return new i_mul_ovf_un(i, g);
                case Mono.Cecil.Cil.Code.Neg:
                    return new i_neg(i, g);
                case Mono.Cecil.Cil.Code.Newarr:
                    return new i_newarr(i, g);
                case Mono.Cecil.Cil.Code.Newobj:
                    return new i_newobj(i, g);
                case Mono.Cecil.Cil.Code.No:
                    return new i_no(i, g);
                case Mono.Cecil.Cil.Code.Nop:
                    return new i_nop(i, g);
                case Mono.Cecil.Cil.Code.Not:
                    return new i_not(i, g);
                case Mono.Cecil.Cil.Code.Or:
                    return new i_or(i, g);
                case Mono.Cecil.Cil.Code.Pop:
                    return new i_pop(i, g);
                case Mono.Cecil.Cil.Code.Readonly:
                    return new i_readonly(i, g);
                case Mono.Cecil.Cil.Code.Refanytype:
                    return new i_refanytype(i, g);
                case Mono.Cecil.Cil.Code.Refanyval:
                    return new i_refanyval(i, g);
                case Mono.Cecil.Cil.Code.Rem:
                    return new i_rem(i, g);
                case Mono.Cecil.Cil.Code.Rem_Un:
                    return new i_rem_un(i, g);
                case Mono.Cecil.Cil.Code.Ret:
                    return new i_ret(i, g);
                case Mono.Cecil.Cil.Code.Rethrow:
                    return new i_rethrow(i, g);
                case Mono.Cecil.Cil.Code.Shl:
                    return new i_shl(i, g);
                case Mono.Cecil.Cil.Code.Shr:
                    return new i_shr(i, g);
                case Mono.Cecil.Cil.Code.Shr_Un:
                    return new i_shr_un(i, g);
                case Mono.Cecil.Cil.Code.Sizeof:
                    return new i_sizeof(i, g);
                case Mono.Cecil.Cil.Code.Starg:
                    return new i_starg(i, g);
                case Mono.Cecil.Cil.Code.Starg_S:
                    return new i_starg_s(i, g);
                case Mono.Cecil.Cil.Code.Stelem_Any:
                    return new i_stelem_any(i, g);
                case Mono.Cecil.Cil.Code.Stelem_I1:
                    return new i_stelem_i1(i, g);
                case Mono.Cecil.Cil.Code.Stelem_I2:
                    return new i_stelem_i2(i, g);
                case Mono.Cecil.Cil.Code.Stelem_I4:
                    return new i_stelem_i4(i, g);
                case Mono.Cecil.Cil.Code.Stelem_I8:
                    return new i_stelem_i8(i, g);
                case Mono.Cecil.Cil.Code.Stelem_I:
                    return new i_stelem_i(i, g);
                case Mono.Cecil.Cil.Code.Stelem_R4:
                    return new i_stelem_r4(i, g);
                case Mono.Cecil.Cil.Code.Stelem_R8:
                    return new i_stelem_r8(i, g);
                case Mono.Cecil.Cil.Code.Stelem_Ref:
                    return new i_stelem_ref(i, g);
                case Mono.Cecil.Cil.Code.Stfld:
                    return new i_stfld(i, g);
                case Mono.Cecil.Cil.Code.Stind_I1:
                    return new i_stind_i1(i, g);
                case Mono.Cecil.Cil.Code.Stind_I2:
                    return new i_stind_i2(i, g);
                case Mono.Cecil.Cil.Code.Stind_I4:
                    return new i_stind_i4(i, g);
                case Mono.Cecil.Cil.Code.Stind_I8:
                    return new i_stind_i8(i, g);
                case Mono.Cecil.Cil.Code.Stind_I:
                    return new i_stind_i(i, g);
                case Mono.Cecil.Cil.Code.Stind_R4:
                    return new i_stind_r4(i, g);
                case Mono.Cecil.Cil.Code.Stind_R8:
                    return new i_stind_r8(i, g);
                case Mono.Cecil.Cil.Code.Stind_Ref:
                    return new i_stind_ref(i, g);
                case Mono.Cecil.Cil.Code.Stloc:
                    return new i_stloc(i, g);
                case Mono.Cecil.Cil.Code.Stloc_0:
                    return new i_stloc_0(i, g);
                case Mono.Cecil.Cil.Code.Stloc_1:
                    return new i_stloc_1(i, g);
                case Mono.Cecil.Cil.Code.Stloc_2:
                    return new i_stloc_2(i, g);
                case Mono.Cecil.Cil.Code.Stloc_3:
                    return new i_stloc_3(i, g);
                case Mono.Cecil.Cil.Code.Stloc_S:
                    return new i_stloc_s(i, g);
                case Mono.Cecil.Cil.Code.Stobj:
                    return new i_stobj(i, g);
                case Mono.Cecil.Cil.Code.Stsfld:
                    return new i_stsfld(i, g);
                case Mono.Cecil.Cil.Code.Sub:
                    return new i_sub(i, g);
                case Mono.Cecil.Cil.Code.Sub_Ovf:
                    return new i_sub_ovf(i, g);
                case Mono.Cecil.Cil.Code.Sub_Ovf_Un:
                    return new i_sub_ovf_un(i, g);
                case Mono.Cecil.Cil.Code.Switch:
                    return new i_switch(i, g);
                case Mono.Cecil.Cil.Code.Tail:
                    return new i_tail(i, g);
                case Mono.Cecil.Cil.Code.Throw:
                    return new i_throw(i, g);
                case Mono.Cecil.Cil.Code.Unaligned:
                    return new i_unaligned(i, g);
                case Mono.Cecil.Cil.Code.Unbox:
                    return new i_unbox(i, g);
                case Mono.Cecil.Cil.Code.Unbox_Any:
                    return new i_unbox_any(i, g);
                case Mono.Cecil.Cil.Code.Volatile:
                    return new i_volatile(i, g);
                case Mono.Cecil.Cil.Code.Xor:
                    return new i_xor(i, g);
                default:
                    throw new Exception("Unknown instruction type " + i);
            }
        }
    }

    class i_add : Inst
    {
        public i_add(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.add(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_add_ovf : Inst
    {
        public i_add_ovf(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.add(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_add_ovf_un : Inst
    {
        public i_add_ovf_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.add(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_and : Inst
    {
        public i_and(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.and(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_arglist : Inst
    {
        public i_arglist(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_beq : Inst
    {
        public i_beq(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.eq, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_beq_s : Inst
    {
        public i_beq_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.eq, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bge : Inst
    {
        public i_bge(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.ge, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bge_un : Inst
    {
        public i_bge_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.ge, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bge_un_s : Inst
    {
        public i_bge_un_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.ge, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bge_s : Inst
    {
        public i_bge_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.ge, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bgt : Inst
    {
        public i_bgt(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.gt, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bgt_s : Inst
    {
        public i_bgt_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.gt, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bgt_un : Inst
    {
        public i_bgt_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.gt, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bgt_un_s : Inst
    {
        public i_bgt_un_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.gt, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_ble : Inst
    {
        public i_ble(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.le, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_ble_s : Inst
    {
        public i_ble_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.le, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_ble_un : Inst
    {
        public i_ble_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.le, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_ble_un_s : Inst
    {
        public i_ble_un_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.le, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_blt : Inst
    {
        public i_blt(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.lt, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_blt_s : Inst
    {
        public i_blt_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.lt, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_blt_un : Inst
    {
        public i_blt_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.lt, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_blt_un_s : Inst
    {
        public i_blt_un_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.lt, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bne_un : Inst
    {
        public i_bne_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.ne, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_bne_un_s : Inst
    {
        public i_bne_un_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                ssa.binary_expression(SSA.Operator.ne, state._stack.Pop(), state._stack.Pop()),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_box : Inst
    {
        public i_box(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_br : Inst
    {
        public i_br(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_br_s : Inst
    {
        public i_br_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.unconditional_branch(
                this,
                ssa.block(this._graph.FindEntry(op)));
        }
    }

    class i_brfalse : Inst
    {
        public i_brfalse(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                state._stack.Pop(),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_break : Inst
    {
        public i_break(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_brfalse_s : Inst
    {
        public i_brfalse_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                state._stack.Pop(),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_brtrue : Inst
    {
        public i_brtrue(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                state._stack.Pop(),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_brtrue_s : Inst
    {
        public i_brtrue_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            Mono.Cecil.Cil.Instruction op = this.Operand as Mono.Cecil.Cil.Instruction;
            SSA.Branch b = ssa.conditional_branch(
                this,
                state._stack.Pop(),
                ssa.block(this._graph.FindEntry(op)),
                ssa.block(this._graph.FindEntry(op.Next)));
        }
    }

    class i_call : Inst
    {
        public i_call(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
            //// Successor is fallthrough.
            //int args = 0;
            //int ret = 0;
            //object method = this.Operand;
            //if (method as Mono.Cecil.MethodReference != null)
            //{
            //    Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
            //    if (mr.HasThis)
            //        args++;
            //    args += mr.Parameters.Count;
            //    if (mr.MethodReturnType != null)
            //    {
            //        Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
            //        Mono.Cecil.TypeReference tr = rt.ReturnType;
            //        if (!tr.FullName.Equals("System.Void"))
            //            ret++;
            //    }
            //}
            //if (args > ret)
            //    for (int i = 0; i < args - ret; ++i)
            //        state._value_stack.Pop();
            //else
            //    for (int i = 0; i < ret - args; ++i)
            //        state._value_stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            object method = this.Operand;
            if (method as Mono.Cecil.MethodReference != null)
            {
                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
                if (mr.HasThis)
                    args++;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
            }
            level_after = level_after + ret - args;
        }
        
        public override void ComputeSSA(ref State state)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            object method = this.Operand;
            if (method as Mono.Cecil.MethodReference != null)
            {
                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
                if (mr.HasThis)
                    args++;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
            }
            if (args > ret)
                for (int i = 0; i < args - ret; ++i)
                    state._stack.Pop();
            else
                for (int i = 0; i < ret - args; ++i)
                    state._stack.Push((SSA.Variable)null);
        }

    }

    class i_calli : Inst
    {
        public i_calli(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            args++; // The function is on the stack.
            object method = this.Operand;
            if (method as Mono.Cecil.CallSite != null)
            {
                Mono.Cecil.CallSite mr = method as Mono.Cecil.CallSite;
                if (mr.HasThis)
                    args++;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
            }
            level_after = level_after + ret - args;
        }

        public override void ComputeSSA(ref State state)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            args++; // The function is on the stack.
            object method = this.Operand;
            if (method as Mono.Cecil.CallSite != null)
            {
                Mono.Cecil.CallSite mr = method as Mono.Cecil.CallSite;
                if (mr.HasThis)
                    args++;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
            }
            if (args > ret)
                for (int i = 0; i < args - ret; ++i)
                    state._stack.Pop();
            else
                for (int i = 0; i < ret - args; ++i)
                    state._stack.Push((SSA.Variable)null);
        }
    }

    class i_callvirt : Inst
    {
        public i_callvirt(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
            //// Successor is fallthrough.
            //int args = 0;
            //int ret = 0;
            //object method = this.Operand;
            //if (method as Mono.Cecil.MethodReference != null)
            //{
            //    Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
            //    if (mr.HasThis)
            //        args++;
            //    args += mr.Parameters.Count;
            //    if (mr.MethodReturnType != null)
            //    {
            //        Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
            //        Mono.Cecil.TypeReference tr = rt.ReturnType;
            //        if (!tr.FullName.Equals("System.Void"))
            //            ret++;
            //    }
            //}
            //if (args > ret)
            //    for (int i = 0; i < args - ret; ++i)
            //        state._value_stack.Pop();
            //else
            //    for (int i = 0; i < ret - args; ++i)
            //        state._value_stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            object method = this.Operand;
            if (method as Mono.Cecil.MethodReference != null)
            {
                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
                if (mr.HasThis)
                    args++;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
            }
            level_after = level_after + ret - args;
        }

        public override void ComputeSSA(ref State state)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            object method = this.Operand;
            if (method as Mono.Cecil.MethodReference != null)
            {
                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
                if (mr.HasThis)
                    args++;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
            }
            if (args > ret)
                for (int i = 0; i < args - ret; ++i)
                    state._stack.Pop();
            else
                for (int i = 0; i < ret - args; ++i)
                    state._stack.Push((SSA.Variable)null);
        }
    }

    class i_castclass : Inst
    {
        public i_castclass(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ceq : Inst
    {
        public i_ceq(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.ceq(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_cgt : Inst
    {
        public i_cgt(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.cgt(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_cgt_un : Inst
    {
        public i_cgt_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.cgt(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_ckfinite : Inst
    {
        public i_ckfinite(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_clt : Inst
    {
        public i_clt(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.clt(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_clt_un : Inst
    {
        public i_clt_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.clt(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_constrained : Inst
    {
        public i_constrained(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_i1 : Inst
    {
        public i_conv_i1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_conv_i2 : Inst
    {
        public i_conv_i2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_conv_i4 : Inst
    {
        public i_conv_i4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_i8 : Inst
    {
        public i_conv_i8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_i : Inst
    {
        public i_conv_i(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i1 : Inst
    {
        public i_conv_ovf_i1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i1_un : Inst
    {
        public i_conv_ovf_i1_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i2 : Inst
    {
        public i_conv_ovf_i2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i2_un : Inst
    {
        public i_conv_ovf_i2_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i4 : Inst
    {
        public i_conv_ovf_i4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i4_un : Inst
    {
        public i_conv_ovf_i4_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i8 : Inst
    {
        public i_conv_ovf_i8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i8_un : Inst
    {
        public i_conv_ovf_i8_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i : Inst
    {
        public i_conv_ovf_i(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i_un : Inst
    {
        public i_conv_ovf_i_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u1 : Inst
    {
        public i_conv_ovf_u1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u1_un : Inst
    {
        public i_conv_ovf_u1_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u2 : Inst
    {
        public i_conv_ovf_u2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u2_un : Inst
    {
        public i_conv_ovf_u2_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u4 : Inst
    {
        public i_conv_ovf_u4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u4_un : Inst
    {
        public i_conv_ovf_u4_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u8 : Inst
    {
        public i_conv_ovf_u8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u8_un : Inst
    {
        public i_conv_ovf_u8_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u : Inst
    {
        public i_conv_ovf_u(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u_un : Inst
    {
        public i_conv_ovf_u_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_r4 : Inst
    {
        public i_conv_r4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_r8 : Inst
    {
        public i_conv_r8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_r_un : Inst
    {
        public i_conv_r_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u1 : Inst
    {
        public i_conv_u1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u2 : Inst
    {
        public i_conv_u2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u4 : Inst
    {
        public i_conv_u4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u8 : Inst
    {
        public i_conv_u8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u : Inst
    {
        public i_conv_u(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_cpblk : Inst
    {
        public i_cpblk(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_cpobj : Inst
    {
        public i_cpobj(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_div : Inst
    {
        public i_div(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.div(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_div_un : Inst
    {
        public i_div_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.div(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_dup : Inst
    {
        public i_dup(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._stack.PeekTop()));
        }
    }

    class i_endfilter : Inst
    {
        public i_endfilter(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
            //state._value_stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_endfinally : Inst
    {
        public i_endfinally(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_initblk : Inst
    {
        public i_initblk(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
            //state._value_stack.Pop();
            //state._value_stack.Pop();
            //state._value_stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 3;
        }
    }

    class i_initobj : Inst
    {
        public i_initobj(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_isinst : Inst
    {
        public i_isinst(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_jmp : Inst
    {
        public i_jmp(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_ldarg : Inst
    {
        int _arg;

        public i_ldarg(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int ar = pr.Index;
            _arg = ar;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._arguments[_arg]));
        }
    }

    class i_ldarg_0 : Inst
    {
        int _arg = 0;

        public i_ldarg_0(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._arguments[_arg]));
        }
    }

    class i_ldarg_1 : Inst
    {
        int _arg = 1;

        public i_ldarg_1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._arguments[_arg]));
        }
    }

    class i_ldarg_2 : Inst
    {
        int _arg = 2;

        public i_ldarg_2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._arguments[_arg]));
        }
    }

    class i_ldarg_3 : Inst
    {
        int _arg = 3;

        public i_ldarg_3(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._arguments[_arg]));
        }
    }

    class i_ldarg_s : Inst
    {
        int _arg;

        public i_ldarg_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int ar = pr.Index;
            _arg = ar;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._arguments[_arg]));
        }
    }

    class i_ldarga : Inst
    {
        int _arg;

        public i_ldarga(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(
                ssa.inst_load(this, new SSA.Variable(),
                    ssa.inst_address_of(this, state._arguments[_arg])));
        }
    }

    class i_ldarga_s : Inst
    {
        int _arg;

        public i_ldarga_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(
                ssa.inst_load(this, new SSA.Variable(),
                    ssa.inst_address_of(this, state._arguments[_arg])));
        }
    }

    class i_ldc_i4 : Inst
    {
        int _arg;

        public i_ldc_i4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = default(int);
            object o = i.Operand;
            if (o != null)
            {
                // Fuck C# casting in the way of just getting
                // a plain ol' int.
                for (; ; )
                {
                    bool success = false;
                    try
                    {
                        byte? o3 = (byte?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        sbyte? o3 = (sbyte?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        short? o3 = (short?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        ushort? o3 = (ushort?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        int? o3 = (int?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    throw new Exception("Cannot convert ldc_i4. Unknown type of operand. F... Mono.");
                }
            }
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_0 : Inst
    {
        int _arg;

        public i_ldc_i4_0(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 0;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_1 : Inst
    {
        int _arg;

        public i_ldc_i4_1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 1;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_2 : Inst
    {
        int _arg;

        public i_ldc_i4_2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 2;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_3 : Inst
    {
        int _arg;

        public i_ldc_i4_3(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 3;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_4 : Inst
    {
        int _arg;

        public i_ldc_i4_4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 4;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_5 : Inst
    {
        int _arg;

        public i_ldc_i4_5(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 5;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_6 : Inst
    {
        int _arg;

        public i_ldc_i4_6(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 6;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_7 : Inst
    {
        int _arg;

        public i_ldc_i4_7(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 7;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_8 : Inst
    {
        int _arg;

        public i_ldc_i4_8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 8;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_m1 : Inst
    {
        int _arg;

        public i_ldc_i4_m1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = -1;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i4_s : Inst
    {
        int _arg;

        public i_ldc_i4_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = default(int);
            object o = i.Operand;
            if (o != null)
            {
                // Fuck C# casting in the way of just getting
                // a plain ol' int.
                for (; ; )
                {
                    bool success = false;
                    try
                    {
                        byte? o3 = (byte?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        sbyte? o3 = (sbyte?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        short? o3 = (short?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        ushort? o3 = (ushort?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        int? o3 = (int?)o;
                        arg = (int)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    throw new Exception("Cannot convert ldc_i4. Unknown type of operand. F... Mono.");
                }
            }
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer32(this, new SSA.Variable(), _arg));
        }
    }

    class i_ldc_i8 : Inst
    {
        Int64 _arg;

        public i_ldc_i8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Int64 arg = default(Int64);
            object o = i.Operand;
            if (o != null)
            {
                // Fuck C# casting in the way of just getting
                // a plain ol' int.
                for (; ; )
                {
                    bool success = false;
                    try
                    {
                        byte? o3 = (byte?)o;
                        arg = (Int64)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        sbyte? o3 = (sbyte?)o;
                        arg = (Int64)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        short? o3 = (short?)o;
                        arg = (Int64)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        ushort? o3 = (ushort?)o;
                        arg = (Int64)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        int? o3 = (int?)o;
                        arg = (Int64)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    throw new Exception("Cannot convert ldc_i4. Unknown type of operand. F... Mono.");
                }
            }
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.integer64(this, _arg));
        }
    }

    class i_ldc_r4 : Inst
    {
        Single _arg;

        public i_ldc_r4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Single arg = default(Single);
            object o = i.Operand;
            if (o != null)
            {
                // Fuck C# casting in the way of just getting
                // a plain ol' int.
                for (; ; )
                {
                    bool success = false;
                    try
                    {
                        byte? o3 = (byte?)o;
                        arg = (Single)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        sbyte? o3 = (sbyte?)o;
                        arg = (Single)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        short? o3 = (short?)o;
                        arg = (Single)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        ushort? o3 = (ushort?)o;
                        arg = (Single)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        int? o3 = (int?)o;
                        arg = (Single)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        Single? o3 = (Single?)o;
                        arg = (Single)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    throw new Exception("Cannot convert ldc_i4. Unknown type of operand. F... Mono.");
                }
            }
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.floatingpoint32(this, _arg));
        }
    }

    class i_ldc_r8 : Inst
    {
        Double _arg;

        public i_ldc_r8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Double arg = default(Double);
            object o = i.Operand;
            if (o != null)
            {
                // Fuck C# casting in the way of just getting
                // a plain ol' int.
                for (; ; )
                {
                    bool success = false;
                    try
                    {
                        byte? o3 = (byte?)o;
                        arg = (Double)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        sbyte? o3 = (sbyte?)o;
                        arg = (Double)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        short? o3 = (short?)o;
                        arg = (Double)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        ushort? o3 = (ushort?)o;
                        arg = (Double)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        int? o3 = (int?)o;
                        arg = (Double)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        Single? o3 = (Single?)o;
                        arg = (Double)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    try
                    {
                        Double? o3 = (Double?)o;
                        arg = (Double)o3;
                        success = true;
                    }
                    catch { }
                    if (success) break;
                    throw new Exception("Cannot convert ldc_i4. Unknown type of operand. F... Mono.");
                }
            }
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.floatingpoint64(this, _arg));
        }
    }

    class i_ldelem_any : Inst
    {
        public i_ldelem_any(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_i1 : Inst
    {
        public i_ldelem_i1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_i2 : Inst
    {
        public i_ldelem_i2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_i4 : Inst
    {
        public i_ldelem_i4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_i8 : Inst
    {
        public i_ldelem_i8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_i : Inst
    {
        public i_ldelem_i(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_r4 : Inst
    {
        public i_ldelem_r4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_r8 : Inst
    {
        public i_ldelem_r8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_ref : Inst
    {
        public i_ldelem_ref(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_u1 : Inst
    {
        public i_ldelem_u1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_u2 : Inst
    {
        public i_ldelem_u2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelem_u4 : Inst
    {
        public i_ldelem_u4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldelema : Inst
    {
        public i_ldelema(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value v1 = state._stack.Pop();
            SSA.Value v2 = state._stack.Pop();
            state._stack.Push(ssa.inst_load_element(this, v2, v1));
        }
    }

    class i_ldfld : Inst
    {
        public i_ldfld(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value obj = state._stack.Pop();
            Mono.Cecil.FieldReference field = (Mono.Cecil.FieldReference)this._instruction.Operand;
            state._stack.Push(ssa.inst_load_field(this, obj, field));
        }
    }

    class i_ldflda : Inst
    {
        public i_ldflda(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            // no change.
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value obj = state._stack.Pop();
            Mono.Cecil.FieldReference field = (Mono.Cecil.FieldReference)this._instruction.Operand;
            state._stack.Push(ssa.inst_load_field(this, obj, field));
        }
    }

    class i_ldftn : Inst
    {
        public i_ldftn(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            object o = _instruction.Operand;
            Mono.Cecil.MethodReference mr = o as Mono.Cecil.MethodReference;
            // Convert method reference into block.
            CFG.CFGVertex n = _graph.FindEntry(mr);
            SSA.Block b = new SSA.Block(n);
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), b));
        }
    }

    class i_ldind_i1 : Inst
    {
        public i_ldind_i1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_i2 : Inst
    {
        public i_ldind_i2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_i4 : Inst
    {
        public i_ldind_i4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_i8 : Inst
    {
        public i_ldind_i8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_i : Inst
    {
        public i_ldind_i(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_r4 : Inst
    {
        public i_ldind_r4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_r8 : Inst
    {
        public i_ldind_r8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_ref : Inst
    {
        public i_ldind_ref(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_u1 : Inst
    {
        public i_ldind_u1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_u2 : Inst
    {
        public i_ldind_u2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldind_u4 : Inst
    {
        public i_ldind_u4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), ssa.inst_indirect_of(this, state._stack.Pop())));
        }
    }

    class i_ldlen : Inst
    {
        public i_ldlen(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldloc : Inst
    {
        int _arg;

        public i_ldloc(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._locals[_arg]));
        }
    }

    class i_ldloc_0 : Inst
    {
        int _arg;

        public i_ldloc_0(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 0;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._locals[_arg]));
        }
    }

    class i_ldloc_1 : Inst
    {
        int _arg;

        public i_ldloc_1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 1;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._locals[_arg]));
        }
    }

    class i_ldloc_2 : Inst
    {
        int _arg;

        public i_ldloc_2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 2;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._locals[_arg]));
        }
    }

    class i_ldloc_3 : Inst
    {
        int _arg;

        public i_ldloc_3(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 3;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._locals[_arg]));
        }
    }

    class i_ldloc_s : Inst
    {
        int _arg;

        public i_ldloc_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.Cil.VariableReference pr = i.Operand as Mono.Cecil.Cil.VariableReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), state._locals[_arg]));
        }
    }

    class i_ldloca : Inst
    {
        int _arg;

        public i_ldloca(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.Cil.VariableDefinition pr = i.Operand as Mono.Cecil.Cil.VariableDefinition;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(
                ssa.inst_load(this, new SSA.Variable(),
                    ssa.inst_address_of(this, state._locals[_arg])));
        }
    }

    class i_ldloca_s : Inst
    {
        int _arg;

        public i_ldloca_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.Cil.VariableDefinition pr = i.Operand as Mono.Cecil.Cil.VariableDefinition;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(
                ssa.inst_load(this, new SSA.Variable(),
                    ssa.inst_address_of(this, state._locals[_arg])));
        }
    }

    class i_ldnull : Inst
    {
        public i_ldnull(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(
                ssa.inst_load(this, new SSA.Variable(),
                    null));
        }
    }

    class i_ldobj : Inst
    {
        public i_ldobj(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value src = state._stack.Pop();
            state._stack.Push(
                ssa.inst_load(this, new SSA.Variable(),
                    null));
        }
    }

    class i_ldsfld : Inst
    {
        public i_ldsfld(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
        
        public override void ComputeSSA(ref State state)
        {
            Mono.Cecil.FieldReference field = (Mono.Cecil.FieldReference)this._instruction.Operand;
            SSA ssa = SSA.Singleton();
            state._stack.Push(
                ssa.inst_load(this, new SSA.Variable(),
                    new SSA.StaticField(field)));
        }
    }

    class i_ldsflda : Inst
    {
        public i_ldsflda(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            Mono.Cecil.FieldReference field = (Mono.Cecil.FieldReference)this._instruction.Operand;
            SSA ssa = SSA.Singleton();
            state._stack.Push(
                ssa.inst_load(this, new SSA.Variable(),
                new SSA.AddressOf(new SSA.StaticField(field))));
        }
    }

    class i_ldstr : Inst
    {
        public i_ldstr(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(
                ssa.inst_load(this, new SSA.Variable(),
                    null));
        }
    }

    class i_ldtoken : Inst
    {
        public i_ldtoken(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this, new SSA.Variable(), (SSA.Value)null));
        }
    }

    class i_ldvirtftn : Inst
    {
        public i_ldvirtftn(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_leave : Inst
    {
        public i_leave(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_leave_s : Inst
    {
        public i_leave_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_localloc : Inst
    {
        public i_localloc(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_mkrefany : Inst
    {
        public i_mkrefany(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_mul : Inst
    {
        public i_mul(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.mul(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_mul_ovf : Inst
    {
        public i_mul_ovf(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.mul(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_mul_ovf_un : Inst
    {
        public i_mul_ovf_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.mul(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_neg : Inst
    {
        public i_neg(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.neg(this, state._stack.Pop()));
        }
    }

    class i_newarr : Inst
    {
        public i_newarr(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            object method = this.Operand;
            if (method as Mono.Cecil.MethodReference != null)
            {
                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
                ret++;
            }
            level_after = level_after + ret - args;
        }

        public override void ComputeSSA(ref State state)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            object method = this.Operand;
            if (method as Mono.Cecil.MethodReference != null)
            {
                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
                ret++;
            }
            if (args > ret)
                for (int i = 0; i < args - ret; ++i)
                    state._stack.Pop();
            else
                for (int i = 0; i < ret - args; ++i)
                    state._stack.Push((SSA.Variable)null);
        }

    }

    class i_newobj : Inst
    {
        public i_newobj(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            object method = this.Operand;
            if (method as Mono.Cecil.MethodReference != null)
            {
                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
                ret++;
            }
            level_after = level_after + ret - args;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Variable v = new SSA.Variable();
            int args = 0;
            int ret = 0;
            object method = this.Operand;
            Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
            if (mr == null)
            {
                SSA.Structure a = ssa.alloc_structure(state, null);
                ssa.inst_assign(this, v, a);
            }
            else
            {
                Mono.Cecil.TypeReference tr = (Mono.Cecil.TypeReference)mr.DeclaringType;
                Mono.Cecil.TypeDefinition td = tr.Resolve();
                SSA.Structure a = ssa.alloc_structure(state, tr);
                ssa.inst_assign(this, v, a);

                // Count args + return value.
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference trr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    if (tr.FullName.Contains(' '))
                    {
                        String[] sp = tr.FullName.Split(' ');
                        if (!sp[0].Equals("System.Void"))
                            ret++;
                    }
                    else
                    {
                        if (!tr.FullName.Equals("System.Void"))
                            ret++;
                    }
                }
                ret++;

                Mono.Cecil.Cil.MethodBody mb = mr.Resolve().Body;
                Type srtr = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionType(tr);

                for (int i = 0; i < args; ++i)
                    state._stack.Pop();
                if (ret > 0)
                {
                    state._stack.Push(v);
                }
            }
        }
    }

    class i_no : Inst
    {
        public i_no(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_nop : Inst
    {
        public i_nop(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_not : Inst
    {
        public i_not(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.not(this, state._stack.Pop()));
        }
    }

    class i_or : Inst
    {
        public i_or(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.or(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_pop : Inst
    {
        public i_pop(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            state._stack.Pop();
        }
    }

    class i_readonly : Inst
    {
        public i_readonly(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_refanytype : Inst
    {
        public i_refanytype(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_refanyval : Inst
    {
        public i_refanyval(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_rem : Inst
    {
        public i_rem(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.mod(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_rem_un : Inst
    {
        public i_rem_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.mod(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_ret : Inst
    {
        public i_ret(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            // There are really two different stacks here:
            // one for the called method, and the other for the caller of the method.
            // When returning, the stack of the method is pretty much unchanged.
            // In fact the top of stack often contains the return value from the method.
            // Back in the caller, the stack is popped of all arguments to the callee.
            // And, the return value is pushed on the top of stack.
            // This is handled by the call instruction.
        }
    }

    class i_rethrow : Inst
    {
        public i_rethrow(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_shl : Inst
    {
        public i_shl(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this,
                new SSA.Variable(),
                ssa.binary_expression(SSA.Operator.shl,
                    state._stack.Pop(), state._stack.Pop())));
        }
    }

    class i_shr : Inst
    {
        public i_shr(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this,
                new SSA.Variable(),
                ssa.binary_expression(SSA.Operator.shr,
                    state._stack.Pop(), state._stack.Pop())));
        }
    }

    class i_shr_un : Inst
    {
        public i_shr_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.inst_load(this,
                new SSA.Variable(),
                ssa.binary_expression(SSA.Operator.shr_un,
                    state._stack.Pop(), state._stack.Pop())));
        }
    }

    class i_sizeof : Inst
    {
        public i_sizeof(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_starg : Inst
    {
        int _arg;

        public i_starg(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, state._arguments, _arg, state._stack.Pop());
        }
    }

    class i_starg_s : Inst
    {
        int _arg;

        public i_starg_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, state._arguments, _arg, state._stack.Pop());
        }
    }

    class i_stelem_any : Inst
    {
        public i_stelem_any(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value index = state._stack.Pop();
            SSA.Value arr = state._stack.Pop();
            ssa.inst_store_element(this, arr, index, val);
        }
    }

    class i_stelem_i1 : Inst
    {
        public i_stelem_i1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value index = state._stack.Pop();
            SSA.Value arr = state._stack.Pop();
            ssa.inst_store_element(this, arr, index, val);
        }
    }

    class i_stelem_i2 : Inst
    {
        public i_stelem_i2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value index = state._stack.Pop();
            SSA.Value arr = state._stack.Pop();
            ssa.inst_store_element(this, arr, index, val);
        }
    }

    class i_stelem_i4 : Inst
    {
        public i_stelem_i4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value index = state._stack.Pop();
            SSA.Value arr = state._stack.Pop();
            ssa.inst_store_element(this, arr, index, val);
        }
    }

    class i_stelem_i8 : Inst
    {
        public i_stelem_i8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value index = state._stack.Pop();
            SSA.Value arr = state._stack.Pop();
            ssa.inst_store_element(this, arr, index, val);
        }
    }

    class i_stelem_i : Inst
    {
        public i_stelem_i(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value index = state._stack.Pop();
            SSA.Value arr = state._stack.Pop();
            ssa.inst_store_element(this, arr, index, val);
        }
    }

    class i_stelem_r4 : Inst
    {
        public i_stelem_r4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value index = state._stack.Pop();
            SSA.Value arr = state._stack.Pop();
            ssa.inst_store_element(this, arr, index, val);
        }
    }

    class i_stelem_r8 : Inst
    {
        public i_stelem_r8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value index = state._stack.Pop();
            SSA.Value arr = state._stack.Pop();
            ssa.inst_store_element(this, arr, index, val);
        }
    }

    class i_stelem_ref : Inst
    {
        public i_stelem_ref(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value index = state._stack.Pop();
            SSA.Value arr = state._stack.Pop();
            ssa.inst_store_element(this, arr, index, val);
        }
    }

    class i_stfld : Inst
    {
        public i_stfld(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            SSA.Value obj = state._stack.Pop();
            Mono.Cecil.FieldReference field = (Mono.Cecil.FieldReference)this._instruction.Operand;
            ssa.inst_store_field(this, obj, field, val);
        }
    }

    class i_stind_i1 : Inst
    {
        public i_stind_i1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, ssa.inst_indirect_of(this, state._stack.Pop()), state._stack.Pop());
        }
    }

    class i_stind_i2 : Inst
    {
        public i_stind_i2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, ssa.inst_indirect_of(this, state._stack.Pop()), state._stack.Pop());
        }
    }

    class i_stind_i4 : Inst
    {
        public i_stind_i4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, ssa.inst_indirect_of(this, state._stack.Pop()), state._stack.Pop());
        }
    }

    class i_stind_i8 : Inst
    {
        public i_stind_i8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, ssa.inst_indirect_of(this, state._stack.Pop()), state._stack.Pop());
        }
    }

    class i_stind_i : Inst
    {
        public i_stind_i(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, ssa.inst_indirect_of(this, state._stack.Pop()), state._stack.Pop());
        }
    }

    class i_stind_r4 : Inst
    {
        public i_stind_r4(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, ssa.inst_indirect_of(this, state._stack.Pop()), state._stack.Pop());
        }
    }

    class i_stind_r8 : Inst
    {
        public i_stind_r8(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, ssa.inst_indirect_of(this, state._stack.Pop()), state._stack.Pop());
        }
    }

    class i_stind_ref : Inst
    {
        public i_stind_ref(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, ssa.inst_indirect_of(this, state._stack.Pop()), state._stack.Pop());
        }
    }

    class i_stloc : Inst
    {
        int _arg;

        public i_stloc(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, state._locals, _arg, state._stack.Pop());
        }
    }

    class i_stloc_0 : Inst
    {
        int _arg;

        public i_stloc_0(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 0;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, state._locals, _arg, state._stack.Pop());
        }
    }

    class i_stloc_1 : Inst
    {
        int _arg;

        public i_stloc_1(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 1;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, state._locals, _arg, state._stack.Pop());
        }
    }

    class i_stloc_2 : Inst
    {
        int _arg;

        public i_stloc_2(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 2;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, state._locals, _arg, state._stack.Pop());
        }
    }

    class i_stloc_3 : Inst
    {
        int _arg;

        public i_stloc_3(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            int arg = 3;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, state._locals, _arg, state._stack.Pop());
        }
    }

    class i_stloc_s : Inst
    {
        int _arg;

        public i_stloc_s(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
            Mono.Cecil.Cil.VariableReference pr = i.Operand as Mono.Cecil.Cil.VariableReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_store(this, state._locals, _arg, state._stack.Pop());
        }
    }

    class i_stobj : Inst
    {
        public i_stobj(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }
    }

    class i_stsfld : Inst
    {
        public i_stsfld(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            Mono.Cecil.FieldReference field = (Mono.Cecil.FieldReference)this._instruction.Operand;
            SSA ssa = SSA.Singleton();
            SSA.Value val = state._stack.Pop();
            ssa.inst_assign(this,
                new SSA.StaticField(field),
                val);
        }
    }

    class i_sub : Inst
    {
        public i_sub(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.sub(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_sub_ovf : Inst
    {
        public i_sub_ovf(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.sub(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_sub_ovf_un : Inst
    {
        public i_sub_ovf_un(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.sub(this, state._stack.Pop(), state._stack.Pop()));
        }
    }

    class i_switch : Inst
    {
        public i_switch(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            ssa.inst_switch(this, state._stack.Pop());
        }
    }

    class i_tail : Inst
    {
        public i_tail(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }
    }

    class i_throw : Inst
    {
        public i_throw(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_unaligned : Inst
    {
        public i_unaligned(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_unbox : Inst
    {
        public i_unbox(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_unbox_any : Inst
    {
        public i_unbox_any(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_volatile : Inst
    {
        public i_volatile(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_xor : Inst
    {
        public i_xor(Mono.Cecil.Cil.Instruction i, CFG g)
            : base(i, g)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }

        public override void ComputeSSA(ref State state)
        {
            SSA ssa = SSA.Singleton();
            state._stack.Push(ssa.xor(this, state._stack.Pop(), state._stack.Pop()));
        }
    }
}
