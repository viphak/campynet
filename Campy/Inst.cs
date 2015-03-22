using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campy
{
    public class Inst
    {
        protected Mono.Cecil.Cil.Instruction _instruction;
        protected int _stack_level_in;
        protected int _stack_level_out;

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

        public override string ToString()
        {
            return _instruction.ToString();
        }

        public Inst(Mono.Cecil.Cil.Instruction i)
        {
            _def = ValueBase.Top;
            _use = ValueBase.Top;
            _kill = ValueBase.Top;
            _instruction = i;
            _stack_level_in = 0;
            _stack_level_out = 0;
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
            // No change.
        }

        protected ValueBase _def;
        protected ValueBase _kill;
        protected ValueBase _use;

        virtual public ValueBase Def()
        {
            return new ValueBase();
        }

        virtual public ValueBase Use()
        {
            return new ValueBase();
        }

        static public Inst Wrap(Mono.Cecil.Cil.Instruction i)
        {
            // Wrap instruction with semantics, def/use/kill properties.
            Mono.Cecil.Cil.OpCode op = i.OpCode;
            switch (op.Code)
            {
                case Mono.Cecil.Cil.Code.Add:
                    return new i_add(i);
                case Mono.Cecil.Cil.Code.Add_Ovf:
                    return new i_add_ovf(i);
                case Mono.Cecil.Cil.Code.Add_Ovf_Un:
                    return new i_add_ovf_un(i);
                case Mono.Cecil.Cil.Code.And:
                    return new i_and(i);
                case Mono.Cecil.Cil.Code.Arglist:
                    return new i_arglist(i);
                case Mono.Cecil.Cil.Code.Beq:
                    return new i_beq(i);
                case Mono.Cecil.Cil.Code.Beq_S:
                    return new i_beq(i);
                case Mono.Cecil.Cil.Code.Bge:
                    return new i_bge(i);
                case Mono.Cecil.Cil.Code.Bge_S:
                    return new i_bge_s(i);
                case Mono.Cecil.Cil.Code.Bgt:
                    return new i_bgt(i);
                case Mono.Cecil.Cil.Code.Bgt_S:
                    return new i_bgt_s(i);
                case Mono.Cecil.Cil.Code.Ble_S:
                    return new i_ble_s(i);
                case Mono.Cecil.Cil.Code.Ble:
                    return new i_ble(i);
                case Mono.Cecil.Cil.Code.Blt:
                    return new i_blt(i);
                case Mono.Cecil.Cil.Code.Blt_S:
                    return new i_blt_s(i);
                case Mono.Cecil.Cil.Code.Bne_Un:
                    return new i_bne_un(i);
                case Mono.Cecil.Cil.Code.Bne_Un_S:
                    return new i_bne_un_s(i);
                case Mono.Cecil.Cil.Code.Box:
                    return new i_box(i);
                case Mono.Cecil.Cil.Code.Br:
                    return new i_br(i);
                case Mono.Cecil.Cil.Code.Br_S:
                    return new i_br_s(i);
                case Mono.Cecil.Cil.Code.Break:
                    return new i_break(i);
                case Mono.Cecil.Cil.Code.Brfalse:
                    return new i_brfalse(i);
                case Mono.Cecil.Cil.Code.Brfalse_S:
                    return new i_brfalse_s(i);
                // Missing brnull
                // Missing brzero
                case Mono.Cecil.Cil.Code.Brtrue:
                    return new i_brtrue(i);
                case Mono.Cecil.Cil.Code.Brtrue_S:
                    return new i_brtrue_s(i);
                case Mono.Cecil.Cil.Code.Call:
                    return new i_call(i);
                case Mono.Cecil.Cil.Code.Calli:
                    return new i_calli(i);
                case Mono.Cecil.Cil.Code.Callvirt:
                    return new i_callvirt(i);
                case Mono.Cecil.Cil.Code.Castclass:
                    return new i_castclass(i);
                case Mono.Cecil.Cil.Code.Ceq:
                    return new i_ceq(i);
                case Mono.Cecil.Cil.Code.Cgt:
                    return new i_cgt(i);
                case Mono.Cecil.Cil.Code.Cgt_Un:
                    return new i_cgt_un(i);
                case Mono.Cecil.Cil.Code.Ckfinite:
                    return new i_ckfinite(i);
                case Mono.Cecil.Cil.Code.Clt:
                    return new i_clt(i);
                case Mono.Cecil.Cil.Code.Clt_Un:
                    return new i_clt_un(i);
                case Mono.Cecil.Cil.Code.Constrained:
                    return new i_constrained(i);
                case Mono.Cecil.Cil.Code.Conv_I1:
                    return new i_conv_i1(i);
                case Mono.Cecil.Cil.Code.Conv_I2:
                    return new i_conv_i2(i);
                case Mono.Cecil.Cil.Code.Conv_I4:
                    return new i_conv_i4(i);
                case Mono.Cecil.Cil.Code.Conv_I8:
                    return new i_conv_i8(i);
                case Mono.Cecil.Cil.Code.Conv_I:
                    return new i_conv_i(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I1:
                    return new i_conv_ovf_i1(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I1_Un:
                    return new i_conv_ovf_i1_un(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I2:
                    return new i_conv_ovf_i2(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I2_Un:
                    return new i_conv_ovf_i2_un(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I4:
                    return new i_conv_ovf_i4(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I4_Un:
                    return new i_conv_ovf_i4_un(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I8:
                    return new i_conv_ovf_i8(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I8_Un:
                    return new i_conv_ovf_i8_un(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I:
                    return new i_conv_ovf_i(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_I_Un:
                    return new i_conv_ovf_i_un(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U1:
                    return new i_conv_ovf_u1(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U1_Un:
                    return new i_conv_ovf_u1_un(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U2:
                    return new i_conv_ovf_u2(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U2_Un:
                    return new i_conv_ovf_u2_un(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U4:
                    return new i_conv_ovf_u4(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U4_Un:
                    return new i_conv_ovf_u4_un(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U8:
                    return new i_conv_ovf_u8(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U8_Un:
                    return new i_conv_ovf_u8_un(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U:
                    return new i_conv_ovf_u(i);
                case Mono.Cecil.Cil.Code.Conv_Ovf_U_Un:
                    return new i_conv_ovf_u_un(i);
                case Mono.Cecil.Cil.Code.Conv_R4:
                    return new i_conv_r4(i);
                case Mono.Cecil.Cil.Code.Conv_R8:
                    return new i_conv_r8(i);
                case Mono.Cecil.Cil.Code.Conv_R_Un:
                    return new i_conv_r_un(i);
                case Mono.Cecil.Cil.Code.Conv_U1:
                    return new i_conv_u1(i);
                case Mono.Cecil.Cil.Code.Conv_U2:
                    return new i_conv_u2(i);
                case Mono.Cecil.Cil.Code.Conv_U4:
                    return new i_conv_u4(i);
                case Mono.Cecil.Cil.Code.Conv_U8:
                    return new i_conv_u8(i);
                case Mono.Cecil.Cil.Code.Conv_U:
                    return new i_conv_u(i);
                case Mono.Cecil.Cil.Code.Cpblk:
                    return new i_cpblk(i);
                case Mono.Cecil.Cil.Code.Cpobj:
                    return new i_cpobj(i);
                case Mono.Cecil.Cil.Code.Div:
                    return new i_div(i);
                case Mono.Cecil.Cil.Code.Div_Un:
                    return new i_div_un(i);
                case Mono.Cecil.Cil.Code.Dup:
                    return new i_dup(i);
                case Mono.Cecil.Cil.Code.Endfilter:
                    return new i_endfilter(i);
                case Mono.Cecil.Cil.Code.Endfinally:
                    return new i_endfinally(i);
                case Mono.Cecil.Cil.Code.Initblk:
                    return new i_initblk(i);
                case Mono.Cecil.Cil.Code.Initobj:
                    return new i_initobj(i);
                case Mono.Cecil.Cil.Code.Isinst:
                    return new i_isinst(i);
                case Mono.Cecil.Cil.Code.Jmp:
                    return new i_jmp(i);
                case Mono.Cecil.Cil.Code.Ldarg:
                    return new i_ldarg(i);
                case Mono.Cecil.Cil.Code.Ldarg_0:
                    return new i_ldarg_0(i);
                case Mono.Cecil.Cil.Code.Ldarg_1:
                    return new i_ldarg_1(i);
                case Mono.Cecil.Cil.Code.Ldarg_2:
                    return new i_ldarg_2(i);
                case Mono.Cecil.Cil.Code.Ldarg_3:
                    return new i_ldarg_3(i);
                case Mono.Cecil.Cil.Code.Ldarg_S:
                    return new i_ldarg_s(i);
                case Mono.Cecil.Cil.Code.Ldarga:
                    return new i_ldarga(i);
                case Mono.Cecil.Cil.Code.Ldarga_S:
                    return new i_ldarga_s(i);
                case Mono.Cecil.Cil.Code.Ldc_I4:
                    return new i_ldc_i4(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_0:
                    return new i_ldc_i4_0(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_1:
                    return new i_ldc_i4_1(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_2:
                    return new i_ldc_i4_2(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_3:
                    return new i_ldc_i4_3(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_4:
                    return new i_ldc_i4_4(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_5:
                    return new i_ldc_i4_5(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_6:
                    return new i_ldc_i4_6(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_7:
                    return new i_ldc_i4_7(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_8:
                    return new i_ldc_i4_8(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_M1:
                    return new i_ldc_i4_m1(i);
                case Mono.Cecil.Cil.Code.Ldc_I4_S:
                    return new i_ldc_i4_s(i);
                case Mono.Cecil.Cil.Code.Ldc_I8:
                    return new i_ldc_i8(i);
                case Mono.Cecil.Cil.Code.Ldc_R4:
                    return new i_ldc_r4(i);
                case Mono.Cecil.Cil.Code.Ldc_R8:
                    return new i_ldc_r8(i);
                case Mono.Cecil.Cil.Code.Ldelem_Any:
                    return new i_ldelem_any(i);
                case Mono.Cecil.Cil.Code.Ldelem_I1:
                    return new i_ldelem_i1(i);
                case Mono.Cecil.Cil.Code.Ldelem_I2:
                    return new i_ldelem_i2(i);
                case Mono.Cecil.Cil.Code.Ldelem_I4:
                    return new i_ldelem_i4(i);
                case Mono.Cecil.Cil.Code.Ldelem_I8:
                    return new i_ldelem_i8(i);
                case Mono.Cecil.Cil.Code.Ldelem_I:
                    return new i_ldelem_i(i);
                case Mono.Cecil.Cil.Code.Ldelem_R4:
                    return new i_ldelem_r4(i);
                case Mono.Cecil.Cil.Code.Ldelem_R8:
                    return new i_ldelem_r8(i);
                case Mono.Cecil.Cil.Code.Ldelem_Ref:
                    return new i_ldelem_ref(i);
                case Mono.Cecil.Cil.Code.Ldelem_U1:
                    return new i_ldelem_u1(i);
                case Mono.Cecil.Cil.Code.Ldelem_U2:
                    return new i_ldelem_u2(i);
                case Mono.Cecil.Cil.Code.Ldelem_U4:
                    return new i_ldelem_u4(i);
                case Mono.Cecil.Cil.Code.Ldelema:
                    return new i_ldelema(i);
                case Mono.Cecil.Cil.Code.Ldfld:
                    return new i_ldfld(i);
                case Mono.Cecil.Cil.Code.Ldflda:
                    return new i_ldflda(i);
                case Mono.Cecil.Cil.Code.Ldftn:
                    return new i_ldftn(i);
                case Mono.Cecil.Cil.Code.Ldind_I1:
                    return new i_ldind_i1(i);
                case Mono.Cecil.Cil.Code.Ldind_I2:
                    return new i_ldind_i2(i);
                case Mono.Cecil.Cil.Code.Ldind_I4:
                    return new i_ldind_i4(i);
                case Mono.Cecil.Cil.Code.Ldind_I8:
                    return new i_ldind_i8(i);
                case Mono.Cecil.Cil.Code.Ldind_I:
                    return new i_ldind_i(i);
                case Mono.Cecil.Cil.Code.Ldind_R4:
                    return new i_ldind_r4(i);
                case Mono.Cecil.Cil.Code.Ldind_R8:
                    return new i_ldind_r8(i);
                case Mono.Cecil.Cil.Code.Ldind_Ref:
                    return new i_ldind_ref(i);
                case Mono.Cecil.Cil.Code.Ldind_U1:
                    return new i_ldind_u1(i);
                case Mono.Cecil.Cil.Code.Ldind_U2:
                    return new i_ldind_u2(i);
                case Mono.Cecil.Cil.Code.Ldind_U4:
                    return new i_ldind_u4(i);
                case Mono.Cecil.Cil.Code.Ldlen:
                    return new i_ldlen(i);
                case Mono.Cecil.Cil.Code.Ldloc:
                    return new i_ldloc(i);
                case Mono.Cecil.Cil.Code.Ldloc_0:
                    return new i_ldloc_0(i);
                case Mono.Cecil.Cil.Code.Ldloc_1:
                    return new i_ldloc_1(i);
                case Mono.Cecil.Cil.Code.Ldloc_2:
                    return new i_ldloc_2(i);
                case Mono.Cecil.Cil.Code.Ldloc_3:
                    return new i_ldloc_3(i);
                case Mono.Cecil.Cil.Code.Ldloc_S:
                    return new i_ldloc_s(i);
                case Mono.Cecil.Cil.Code.Ldloca:
                    return new i_ldloca(i);
                case Mono.Cecil.Cil.Code.Ldloca_S:
                    return new i_ldloca_s(i);
                case Mono.Cecil.Cil.Code.Ldnull:
                    return new i_ldnull(i);
                case Mono.Cecil.Cil.Code.Ldobj:
                    return new i_ldobj(i);
                case Mono.Cecil.Cil.Code.Ldsfld:
                    return new i_ldsfld(i);
                case Mono.Cecil.Cil.Code.Ldsflda:
                    return new i_ldsflda(i);
                case Mono.Cecil.Cil.Code.Ldstr:
                    return new i_ldstr(i);
                case Mono.Cecil.Cil.Code.Ldtoken:
                    return new i_ldtoken(i);
                case Mono.Cecil.Cil.Code.Ldvirtftn:
                    return new i_ldvirtftn(i);
                case Mono.Cecil.Cil.Code.Leave:
                    return new i_leave(i);
                case Mono.Cecil.Cil.Code.Leave_S:
                    return new i_leave_s(i);
                case Mono.Cecil.Cil.Code.Localloc:
                    return new i_localloc(i);
                case Mono.Cecil.Cil.Code.Mkrefany:
                    return new i_mkrefany(i);
                case Mono.Cecil.Cil.Code.Mul:
                    return new i_mul(i);
                case Mono.Cecil.Cil.Code.Mul_Ovf:
                    return new i_mul_ovf(i);
                case Mono.Cecil.Cil.Code.Mul_Ovf_Un:
                    return new i_mul_ovf_un(i);
                case Mono.Cecil.Cil.Code.Neg:
                    return new i_neg(i);
                case Mono.Cecil.Cil.Code.Newarr:
                    return new i_newarr(i);
                case Mono.Cecil.Cil.Code.Newobj:
                    return new i_newobj(i);
                case Mono.Cecil.Cil.Code.No:
                    return new i_no(i);
                case Mono.Cecil.Cil.Code.Nop:
                    return new i_nop(i);
                case Mono.Cecil.Cil.Code.Not:
                    return new i_not(i);
                case Mono.Cecil.Cil.Code.Or:
                    return new i_or(i);
                case Mono.Cecil.Cil.Code.Pop:
                    return new i_pop(i);
                case Mono.Cecil.Cil.Code.Readonly:
                    return new i_readonly(i);
                case Mono.Cecil.Cil.Code.Refanytype:
                    return new i_refanytype(i);
                case Mono.Cecil.Cil.Code.Refanyval:
                    return new i_refanyval(i);
                case Mono.Cecil.Cil.Code.Rem:
                    return new i_rem(i);
                case Mono.Cecil.Cil.Code.Rem_Un:
                    return new i_rem_un(i);
                case Mono.Cecil.Cil.Code.Ret:
                    return new i_ret(i);
                case Mono.Cecil.Cil.Code.Rethrow:
                    return new i_rethrow(i);
                case Mono.Cecil.Cil.Code.Shl:
                    return new i_shl(i);
                case Mono.Cecil.Cil.Code.Shr:
                    return new i_shr(i);
                case Mono.Cecil.Cil.Code.Shr_Un:
                    return new i_shr_un(i);
                case Mono.Cecil.Cil.Code.Sizeof:
                    return new i_sizeof(i);
                case Mono.Cecil.Cil.Code.Starg:
                    return new i_starg(i);
                case Mono.Cecil.Cil.Code.Starg_S:
                    return new i_starg_s(i);
                case Mono.Cecil.Cil.Code.Stelem_Any:
                    return new i_stelem_any(i);
                case Mono.Cecil.Cil.Code.Stelem_I1:
                    return new i_stelem_i1(i);
                case Mono.Cecil.Cil.Code.Stelem_I2:
                    return new i_stelem_i2(i);
                case Mono.Cecil.Cil.Code.Stelem_I4:
                    return new i_stelem_i4(i);
                case Mono.Cecil.Cil.Code.Stelem_I8:
                    return new i_stelem_i8(i);
                case Mono.Cecil.Cil.Code.Stelem_I:
                    return new i_stelem_i(i);
                case Mono.Cecil.Cil.Code.Stelem_R4:
                    return new i_stelem_r4(i);
                case Mono.Cecil.Cil.Code.Stelem_R8:
                    return new i_stelem_r8(i);
                case Mono.Cecil.Cil.Code.Stelem_Ref:
                    return new i_stelem_ref(i);
                case Mono.Cecil.Cil.Code.Stfld:
                    return new i_stfld(i);
                case Mono.Cecil.Cil.Code.Stind_I1:
                    return new i_stind_i1(i);
                case Mono.Cecil.Cil.Code.Stind_I2:
                    return new i_stind_i2(i);
                case Mono.Cecil.Cil.Code.Stind_I4:
                    return new i_stind_i4(i);
                case Mono.Cecil.Cil.Code.Stind_I8:
                    return new i_stind_i8(i);
                case Mono.Cecil.Cil.Code.Stind_I:
                    return new i_stind_i(i);
                case Mono.Cecil.Cil.Code.Stind_R4:
                    return new i_stind_r4(i);
                case Mono.Cecil.Cil.Code.Stind_R8:
                    return new i_stind_r8(i);
                case Mono.Cecil.Cil.Code.Stind_Ref:
                    return new i_stind_ref(i);
                case Mono.Cecil.Cil.Code.Stloc:
                    return new i_stloc(i);
                case Mono.Cecil.Cil.Code.Stloc_0:
                    return new i_stloc_0(i);
                case Mono.Cecil.Cil.Code.Stloc_1:
                    return new i_stloc_1(i);
                case Mono.Cecil.Cil.Code.Stloc_2:
                    return new i_stloc_2(i);
                case Mono.Cecil.Cil.Code.Stloc_3:
                    return new i_stloc_3(i);
                case Mono.Cecil.Cil.Code.Stloc_S:
                    return new i_stloc_s(i);
                case Mono.Cecil.Cil.Code.Stobj:
                    return new i_stobj(i);
                case Mono.Cecil.Cil.Code.Stsfld:
                    return new i_stsfld(i);
                case Mono.Cecil.Cil.Code.Sub:
                    return new i_sub(i);
                case Mono.Cecil.Cil.Code.Sub_Ovf:
                    return new i_sub_ovf(i);
                case Mono.Cecil.Cil.Code.Sub_Ovf_Un:
                    return new i_sub_ovf_un(i);
                case Mono.Cecil.Cil.Code.Switch:
                    return new i_switch(i);
                case Mono.Cecil.Cil.Code.Tail:
                    return new i_tail(i);
                case Mono.Cecil.Cil.Code.Throw:
                    return new i_throw(i);
                case Mono.Cecil.Cil.Code.Unaligned:
                    return new i_unaligned(i);
                case Mono.Cecil.Cil.Code.Unbox:
                    return new i_unbox(i);
                case Mono.Cecil.Cil.Code.Unbox_Any:
                    return new i_unbox_any(i);
                case Mono.Cecil.Cil.Code.Volatile:
                    return new i_volatile(i);
                case Mono.Cecil.Cil.Code.Xor:
                    return new i_xor(i);
                default:
                    return null;
            }
        }
    }

    class i_add : Inst
    {
        public i_add(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_add_ovf : Inst
    {
        public i_add_ovf(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_add_ovf_un : Inst
    {
        public i_add_ovf_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_and : Inst
    {
        public i_and(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_arglist : Inst
    {
        public i_arglist(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_beq : Inst
    {
        public i_beq(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_beq_s : Inst
    {
        public i_beq_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_bge : Inst
    {
        public i_bge(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_bge_un : Inst
    {
        public i_bge_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_bge_s : Inst
    {
        public i_bge_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_bgt : Inst
    {
        public i_bgt(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_bgt_s : Inst
    {
        public i_bgt_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_bgt_un : Inst
    {
        public i_bgt_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_ble : Inst
    {
        public i_ble(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_ble_s : Inst
    {
        public i_ble_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_ble_un : Inst
    {
        public i_ble_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_blt : Inst
    {
        public i_blt(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_blt_s : Inst
    {
        public i_blt_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_blt_un : Inst
    {
        public i_blt_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_bne_un : Inst
    {
        public i_bne_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_bne_un_s : Inst
    {
        public i_bne_un_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 2;
        }
    }

    class i_box : Inst
    {
        public i_box(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_br : Inst
    {
        public i_br(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_br_s : Inst
    {
        public i_br_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_brfalse : Inst
    {
        public i_brfalse(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_break : Inst
    {
        public i_break(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Inform debugger of breakpoint.
            // no change.
        }
    }

    class i_brfalse_s : Inst
    {
        public i_brfalse_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_brtrue : Inst
    {
        public i_brtrue(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_brtrue_s : Inst
    {
        public i_brtrue_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_call : Inst
    {
        public i_call(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
            }
            if (args > ret)
                for (int i = 0; i < args - ret; ++i)
                    state._stack.Pop();
            else
                for (int i = 0; i < ret - args; ++i)
                    state._stack.Push(ValueBase.Bottom);
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
            }
            level_after = level_after + ret - args;
        }
    }

    class i_calli : Inst
    {
        public i_calli(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
            }
            if (args > ret)
                for (int i = 0; i < args - ret; ++i)
                    state._stack.Pop();
            else
                for (int i = 0; i < ret - args; ++i)
                    state._stack.Push(ValueBase.Bottom);
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
            }
            level_after = level_after + ret - args;
        }
    }

    class i_callvirt : Inst
    {
        public i_callvirt(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
            }
            if (args > ret)
                for (int i = 0; i < args - ret; ++i)
                    state._stack.Pop();
            else
                for (int i = 0; i < ret - args; ++i)
                    state._stack.Push(ValueBase.Bottom);
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
            }
            level_after = level_after + ret - args;
        }
    }

    class i_castclass : Inst
    {
        public i_castclass(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ceq : Inst
    {
        public i_ceq(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }
    class i_cgt : Inst
    {
        public i_cgt(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_cgt_un : Inst
    {
        public i_cgt_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ckfinite : Inst
    {
        public i_ckfinite(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_clt : Inst
    {
        public i_clt(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_clt_un : Inst
    {
        public i_clt_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_constrained : Inst
    {
        public i_constrained(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_i1 : Inst
    {
        public i_conv_i1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_i2 : Inst
    {
        public i_conv_i2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_i4 : Inst
    {
        public i_conv_i4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_i8 : Inst
    {
        public i_conv_i8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_i : Inst
    {
        public i_conv_i(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i1 : Inst
    {
        public i_conv_ovf_i1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i1_un : Inst
    {
        public i_conv_ovf_i1_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i2 : Inst
    {
        public i_conv_ovf_i2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i2_un : Inst
    {
        public i_conv_ovf_i2_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i4 : Inst
    {
        public i_conv_ovf_i4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i4_un : Inst
    {
        public i_conv_ovf_i4_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i8 : Inst
    {
        public i_conv_ovf_i8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i8_un : Inst
    {
        public i_conv_ovf_i8_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i : Inst
    {
        public i_conv_ovf_i(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_i_un : Inst
    {
        public i_conv_ovf_i_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u1 : Inst
    {
        public i_conv_ovf_u1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u1_un : Inst
    {
        public i_conv_ovf_u1_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u2 : Inst
    {
        public i_conv_ovf_u2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u2_un : Inst
    {
        public i_conv_ovf_u2_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u4 : Inst
    {
        public i_conv_ovf_u4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u4_un : Inst
    {
        public i_conv_ovf_u4_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u8 : Inst
    {
        public i_conv_ovf_u8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u8_un : Inst
    {
        public i_conv_ovf_u8_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u : Inst
    {
        public i_conv_ovf_u(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_ovf_u_un : Inst
    {
        public i_conv_ovf_u_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_r4 : Inst
    {
        public i_conv_r4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_r8 : Inst
    {
        public i_conv_r8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_r_un : Inst
    {
        public i_conv_r_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u1 : Inst
    {
        public i_conv_u1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u2 : Inst
    {
        public i_conv_u2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u4 : Inst
    {
        public i_conv_u4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u8 : Inst
    {
        public i_conv_u8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_conv_u : Inst
    {
        public i_conv_u(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_cpblk : Inst
    {
        public i_cpblk(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_cpobj : Inst
    {
        public i_cpobj(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_div : Inst
    {
        public i_div(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_div_un : Inst
    {
        public i_div_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_dup : Inst
    {
        public i_dup(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.PeekTop();
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_endfilter : Inst
    {
        public i_endfilter(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_endfinally : Inst
    {
        public i_endfinally(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_initblk : Inst
    {
        public i_initblk(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after -= 3;
        }
    }

    class i_initobj : Inst
    {
        public i_initobj(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
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
        public i_isinst(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_jmp : Inst
    {
        public i_jmp(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldarg : Inst
    {
        int _arg;

        public i_ldarg(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int ar = pr.Index;
            _arg = ar;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._arguments[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldarg_0 : Inst
    {
        int _arg = 0;

        public i_ldarg_0(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._arguments[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldarg_1 : Inst
    {
        int _arg = 1;

        public i_ldarg_1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._arguments[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldarg_2 : Inst
    {
        int _arg = 2;

        public i_ldarg_2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._arguments[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldarg_3 : Inst
    {
        int _arg = 3;

        public i_ldarg_3(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._arguments[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldarg_s : Inst
    {
        int _arg;

        public i_ldarg_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int ar = pr.Index;
            _arg = ar;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._arguments[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldarga : Inst
    {
        int _arg;

        public i_ldarga(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new LValue(state._arguments, _arg);
            //state._stack.Push(v);
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldarga_s : Inst
    {
        int _arg;

        public i_ldarga_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new LValue(state._arguments, _arg);
            //state._stack.Push(v);
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4 : Inst
    {
        int _arg;

        public i_ldc_i4(Mono.Cecil.Cil.Instruction i)
            : base(i)
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

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_0 : Inst
    {
        int _arg;

        public i_ldc_i4_0(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 0;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_1 : Inst
    {
        int _arg;

        public i_ldc_i4_1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 1;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_2 : Inst
    {
        int _arg;

        public i_ldc_i4_2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 2;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_3 : Inst
    {
        int _arg;

        public i_ldc_i4_3(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 3;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_4 : Inst
    {
        int _arg;

        public i_ldc_i4_4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 4;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_5 : Inst
    {
        int _arg;

        public i_ldc_i4_5(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 5;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_6 : Inst
    {
        int _arg;

        public i_ldc_i4_6(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 6;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_7 : Inst
    {
        int _arg;

        public i_ldc_i4_7(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 7;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_8 : Inst
    {
        int _arg;

        public i_ldc_i4_8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 8;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_m1 : Inst
    {
        int _arg;

        public i_ldc_i4_m1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = -1;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i4_s : Inst
    {
        int _arg;

        public i_ldc_i4_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
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

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<int>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_i8 : Inst
    {
        Int64 _arg;

        public i_ldc_i8(Mono.Cecil.Cil.Instruction i)
            : base(i)
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

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<Int64>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_r4 : Inst
    {
        Single _arg;

        public i_ldc_r4(Mono.Cecil.Cil.Instruction i)
            : base(i)
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

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<Single>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldc_r8 : Inst
    {
        Double _arg;

        public i_ldc_r8(Mono.Cecil.Cil.Instruction i)
            : base(i)
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

        public override void Execute(ref State state)
        {
            ValueBase v = new RValue<Double>(_arg);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldelem_any : Inst
    {
        public i_ldelem_any(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_i1 : Inst
    {
        public i_ldelem_i1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_i2 : Inst
    {
        public i_ldelem_i2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_i4 : Inst
    {
        public i_ldelem_i4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_i8 : Inst
    {
        public i_ldelem_i8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_i : Inst
    {
        public i_ldelem_i(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_r4 : Inst
    {
        public i_ldelem_r4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_r8 : Inst
    {
        public i_ldelem_r8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_ref : Inst
    {
        public i_ldelem_ref(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_u1 : Inst
    {
        public i_ldelem_u1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldelem_u2 : Inst
    {
        public i_ldelem_u2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelem_u4 : Inst
    {
        public i_ldelem_u4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldelema : Inst
    {
        public i_ldelema(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ldfld : Inst
    {
        public i_ldfld(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldflda : Inst
    {
        public i_ldflda(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldftn : Inst
    {
        public i_ldftn(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            object o = _instruction.Operand;
            Mono.Cecil.MethodReference mr = o as Mono.Cecil.MethodReference;
            RValue<Mono.Cecil.MethodReference> v = new RValue<Mono.Cecil.MethodReference>(mr);
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldind_i1 : Inst
    {
        public i_ldind_i1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // No changes.
        }
    }

    class i_ldind_i2 : Inst
    {
        public i_ldind_i2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldind_i4 : Inst
    {
        public i_ldind_i4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldind_i8 : Inst
    {
        public i_ldind_i8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldind_i : Inst
    {
        public i_ldind_i(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldind_r4 : Inst
    {
        public i_ldind_r4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldind_r8 : Inst
    {
        public i_ldind_r8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldind_ref : Inst
    {
        public i_ldind_ref(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldind_u1 : Inst
    {
        public i_ldind_u1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldind_u2 : Inst
    {
        public i_ldind_u2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldind_u4 : Inst
    {
        public i_ldind_u4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldlen : Inst
    {
        public i_ldlen(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldloc : Inst
    {
        int _arg;

        public i_ldloc(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._locals[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldloc_0 : Inst
    {
        int _arg;

        public i_ldloc_0(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 0;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._locals[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldloc_1 : Inst
    {
        int _arg;

        public i_ldloc_1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 1;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._locals[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldloc_2 : Inst
    {
        int _arg;

        public i_ldloc_2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 2;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._locals[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldloc_3 : Inst
    {
        int _arg;

        public i_ldloc_3(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 3;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._locals[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldloc_s : Inst
    {
        int _arg;

        public i_ldloc_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.Cil.VariableReference pr = i.Operand as Mono.Cecil.Cil.VariableReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._locals[_arg];
            if (v == null) v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldloca : Inst
    {
        int _arg;

        public i_ldloca(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.Cil.VariableDefinition pr = i.Operand as Mono.Cecil.Cil.VariableDefinition;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new LValue(state._locals, _arg);
            //state._stack.Push(v);
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldloca_s : Inst
    {
        int _arg;

        public i_ldloca_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.Cil.VariableDefinition pr = i.Operand as Mono.Cecil.Cil.VariableDefinition;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = new LValue(state._locals, _arg);
            //state._stack.Push(v);
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldnull : Inst
    {
        public i_ldnull(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v = ValueBase.Bottom;
            state._stack.Push(v);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldobj : Inst
    {
        public i_ldobj(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after++;
        }
    }

    class i_ldsfld : Inst
    {
        public i_ldsfld(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldsflda : Inst
    {
        public i_ldsflda(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldstr : Inst
    {
        public i_ldstr(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldtoken : Inst
    {
        public i_ldtoken(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_ldvirtftn : Inst
    {
        public i_ldvirtftn(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_leave : Inst
    {
        public i_leave(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_leave_s : Inst
    {
        public i_leave_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_localloc : Inst
    {
        public i_localloc(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_mkrefany : Inst
    {
        public i_mkrefany(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_mul : Inst
    {
        public i_mul(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_mul_ovf : Inst
    {
        public i_mul_ovf(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_mul_ovf_un : Inst
    {
        public i_mul_ovf_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_neg : Inst
    {
        public i_neg(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }
    }

    class i_newarr : Inst
    {
        public i_newarr(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
                ret++;
            }
            if (args > ret)
                for (int i = 0; i < args - ret; ++i)
                    state._stack.Pop();
            else
                for (int i = 0; i < ret - args; ++i)
                    state._stack.Push(ValueBase.Bottom);
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
                ret++;
            }
            level_after = level_after + ret - args;
        }
    }

    class i_newobj : Inst
    {
        public i_newobj(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Successor is fallthrough.
            int args = 0;
            int ret = 0;
            bool have_delegate = false;
            object method = this.Operand;
            Type t = null;

            if (method as Mono.Cecil.MethodReference != null)
            {
                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;

                // Get owning class.
                Mono.Cecil.TypeReference tr = (Mono.Cecil.TypeReference)mr.DeclaringType;

                // Count args and return.
                have_delegate = true;
                args += mr.Parameters.Count;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference trr = rt.ReturnType;
                    if (!trr.FullName.Equals("System.Void"))
                        ret++;
                }
                ret++;

                for (int i = 0; i < args; ++i)
                    state._stack.Pop();

                // Create it.
                t = Campy.Types.Utils.Utility.CreateBlittableTypeMono(tr, false);

                if (t != null)
                {
                    object obj = Activator.CreateInstance(t);
                    state._stack.Push(new RValue<object>(obj));
                }
                else
                {
                    state._stack.Push(ValueBase.Bottom);
                }
            }
            else
            {
                if (args > ret)
                    for (int i = 0; i < args - ret; ++i)
                        state._stack.Pop();
                else
                    for (int i = 0; i < ret - args; ++i)
                        state._stack.Push(ValueBase.Bottom);
            }
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
                ret++;
            }
            level_after = level_after + ret - args;
        }
    }

    class i_no : Inst
    {
        public i_no(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_nop : Inst
    {
        public i_nop(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_not : Inst
    {
        public i_not(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }
    }

    class i_or : Inst
    {
        public i_or(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_pop : Inst
    {
        public i_pop(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_readonly : Inst
    {
        public i_readonly(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_refanytype : Inst
    {
        public i_refanytype(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_refanyval : Inst
    {
        public i_refanyval(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_rem : Inst
    {
        public i_rem(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_rem_un : Inst
    {
        public i_rem_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            // Pop two values, push new value.
            ValueBase v1 = state._stack.Pop();
            ValueBase v2 = state._stack.Pop();
            // Push bottom -- unknown value.
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_ret : Inst
    {
        public i_ret(Mono.Cecil.Cil.Instruction i)
            : base(i)
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
        public i_rethrow(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_shl : Inst
    {
        public i_shl(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_shr : Inst
    {
        public i_shr(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_shr_un : Inst
    {
        public i_shr_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_sizeof : Inst
    {
        public i_sizeof(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_starg : Inst
    {
        int _arg;

        public i_starg(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.Pop();
            state._arguments[_arg] = v;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_starg_s : Inst
    {
        int _arg;

        public i_starg_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.Pop();
            state._arguments[_arg] = v;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_stelem_any : Inst
    {
        public i_stelem_any(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }
    }

    class i_stelem_i1 : Inst
    {
        public i_stelem_i1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }
    }

    class i_stelem_i2 : Inst
    {
        public i_stelem_i2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }
    }

    class i_stelem_i4 : Inst
    {
        public i_stelem_i4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }
    }

    class i_stelem_i8 : Inst
    {
        public i_stelem_i8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }
    }

    class i_stelem_i : Inst
    {
        public i_stelem_i(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }
    }

    class i_stelem_r4 : Inst
    {
        public i_stelem_r4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }
    }

    class i_stelem_r8 : Inst
    {
        public i_stelem_r8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }
    }

    class i_stelem_ref : Inst
    {
        public i_stelem_ref(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 3;
        }
    }

    class i_stfld : Inst
    {
        public i_stfld(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }
    }

    class i_stind_i1 : Inst
    {
        public i_stind_i1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }
    }

    class i_stind_i2 : Inst
    {
        public i_stind_i2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }
    }

    class i_stind_i4 : Inst
    {
        public i_stind_i4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }
    }

    class i_stind_i8 : Inst
    {
        public i_stind_i8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }
    }

    class i_stind_i : Inst
    {
        public i_stind_i(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }
    }

    class i_stind_r4 : Inst
    {
        public i_stind_r4(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }
    }

    class i_stind_r8 : Inst
    {
        public i_stind_r8(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }
    }

    class i_stind_ref : Inst
    {
        public i_stind_ref(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after = level_after - 2;
        }
    }

    class i_stloc : Inst
    {
        int _arg;

        public i_stloc(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.ParameterReference pr = i.Operand as Mono.Cecil.ParameterReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.Pop();
            state._locals[_arg] = v;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_stloc_0 : Inst
    {
        int _arg;

        public i_stloc_0(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 0;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.Pop();
            state._locals[_arg] = v;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_stloc_1 : Inst
    {
        int _arg;

        public i_stloc_1(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 1;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.Pop();
            state._locals[_arg] = v;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_stloc_2 : Inst
    {
        int _arg;

        public i_stloc_2(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 2;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.Pop();
            state._locals[_arg] = v;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_stloc_3 : Inst
    {
        int _arg;

        public i_stloc_3(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            int arg = 3;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.Pop();
            state._locals[_arg] = v;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_stloc_s : Inst
    {
        int _arg;

        public i_stloc_s(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
            Mono.Cecil.Cil.VariableReference pr = i.Operand as Mono.Cecil.Cil.VariableReference;
            int arg = pr.Index;
            _arg = arg;
        }

        public override void Execute(ref State state)
        {
            ValueBase v = state._stack.Pop();
            state._locals[_arg] = v;
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_stobj : Inst
    {
        public i_stobj(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_stsfld : Inst
    {
        public i_stsfld(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_sub : Inst
    {
        public i_sub(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_sub_ovf : Inst
    {
        public i_sub_ovf(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_sub_ovf_un : Inst
    {
        public i_sub_ovf_un(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }

    class i_switch : Inst
    {
        public i_switch(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_tail : Inst
    {
        public i_tail(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_throw : Inst
    {
        public i_throw(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_unaligned : Inst
    {
        public i_unaligned(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_unbox : Inst
    {
        public i_unbox(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_unbox_any : Inst
    {
        public i_unbox_any(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_volatile : Inst
    {
        public i_volatile(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
        }
    }

    class i_xor : Inst
    {
        public i_xor(Mono.Cecil.Cil.Instruction i)
            : base(i)
        {
        }

        public override void Execute(ref State state)
        {
            state._stack.Pop();
            state._stack.Pop();
            state._stack.Push(ValueBase.Bottom);
        }

        public override void ComputeStackLevel(ref int level_after)
        {
            level_after--;
        }
    }
}
