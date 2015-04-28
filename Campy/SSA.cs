using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campy.Utils;

namespace Campy
{
    public class SSA
    {
        static SSA _ssa;

        SSA()
        {
        }

        public Dictionary<Value, Phi> phi_functions = new Dictionary<Value, Phi>();

        public static SSA Singleton()
        {
            if (_ssa == null)
                _ssa = new SSA();
            return _ssa;
        }

        public enum Operator
        {
            sub, add, mul, div, mod, and, or, xor,
            gt, lt, ge, le, eq, ne,
            shl, shr, shr_un,
            not, neg,
        }

        //============================================================
        //
        // SSA VALUES
        //
        //============================================================

        public class Value
        {
        }

        public class AddressOf : Value
        {
            public Variable v;
            public override string ToString()
            {
                return v.ToString();
            }
        }

        public class Array : Value
        {
            public object _obj;
            public override string ToString()
            {
                return _obj.ToString();
            }
        }

        public class ArrayElement : Variable
        {
            public Variable _array;
            public Variable _index;
            public override string ToString()
            {
                return _array.ToString()
                    + "["
                    + _index.ToString()
                    + "]";
            }
        }

        public class BinaryExpression : Value
        {
            public Value LHS;
            public Value RHS;
            public Operator op;
            public override string ToString()
            {
                return LHS.ToString()
                    + op.ToString()
                    + RHS.ToString();
            }
        }

        public class Block : Value
        {
            public CFG.CFGVertex _block;
            public Block(CFG.CFGVertex block)
            {
                _block = block;
            }
            public override string ToString()
            {
                return "node " + _block.ToString();
            }
        }

        public class DerefOf : Value
        {
            public Variable v;
            public override string ToString()
            {
                return v.ToString();
            }
        }

        public class Field : Value
        {
            public Value obj;
            public Mono.Cecil.FieldReference field;
            public override string ToString()
            {
                return obj.ToString()
                    + "."
                    + field.ToString();
            }
        }

        public class FloatingPoint32 : Value
        {
            public float Value;
            public FloatingPoint32(float value)
            {
                Value = value;
            }
            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class FloatingPoint64 : Value
        {
            public double Value;
            public FloatingPoint64(double value)
            {
                Value = value;
            }
            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class Indirect : Variable
        {
            public Variable variable;
            public override string ToString()
            {
                return "["
                    + variable.ToString()
                    + "]";
            }
        }

        public class Integer32 : Value
        {
            public int Value;
            public Integer32(int value)
            {
                Value = value;
            }
            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class Integer64 : Value
        {
            public long Value;
            public Integer64(long value)
            {
                Value = value;
            }
            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class Obj : Value
        {
            public String type;
            public override string ToString()
            {
                return type.ToString();
            }
        }

        public class Phi : Value
        {
            public Value v;
            public List<Value> merge;
            public override string ToString()
            {
                String result = this.v.ToString() + "("
                    + this.merge.Aggregate(
                            new StringBuilder(),
                            (sb, x) =>
                                sb.Append(x).Append(", "),
                            sb =>
                            {
                                if (0 < sb.Length)
                                    sb.Length -= 2;
                                return sb.ToString();
                            })
                    + ")";
                return result;
            }
        }

        public class Set : Value
        {
            public List<Value> list;
            public void Add(Value v)
            {
                if (list.Contains(v))
                    return;
                list.Add(v);
            }
            public override string ToString()
            {
                return base.ToString();
            }
        }

        public class Structure : Value
        {
            public object _obj;
            public override string ToString()
            {
                return _obj.ToString();
            }
        }

        public class UnaryExpression : Value
        {
            public Value expr;
            public Operator op;
            public override string ToString()
            {
                return op.ToString()
                    + expr.ToString();
            }
        }

        public class UInteger32 : Value
        {
            public uint Value;
            public UInteger32(uint value)
            {
                Value = value;
            }
            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class UInteger64 : Value
        {
            public ulong Value;
            public UInteger64(ulong value)
            {
                Value = value;
            }
            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class Variable : Value
        {
            public String Name;
            static int next;
            public static Variable Generate()
            {
                next++;
                Variable v = new Variable();
                v.Name = "v" + next;
                return v;
            }
            public override string ToString()
            {
                return this.Name;
            }
        }


        //============================================================
        //
        // SSA OPERATIONS
        //
        //============================================================

        public class Operation
        {
        }

        public class Assignment : Operation
        {
            public Value lhs;
            public Value rhs;
        }

        public class Branch : Operation
        {
            public Value expression;
            public Block address_true;
            public Block address_false;
        }

        public class Switch : Operation
        {
            public Value expression;
            public Block[] addrs;
        }

        //public StackQueue<Variable> _stack = new StackQueue<Variable>();

        public MultiMap<Inst, Operation> _operation = new MultiMap<Inst, Operation>();
        
        public MultiMap<Value, Inst> _defined = new MultiMap<Value, Inst>();

        public Assignment inst_assign(Inst inst, Value lhs, Value rhs)
        {
            // Every variable is assigned once, and is uniquely identifiable.
            Assignment a = new Assignment();
            a.lhs = lhs;
            a.rhs = rhs;
            _operation.Add(inst, a);
            _defined.Add(lhs, inst);
            return a;
        }

        public Variable inst_load(Inst inst, Variable lhs, Value rhs)
        {
            inst_assign(inst, lhs, rhs);
            return lhs;
        }

        public void inst_store(Inst inst, Value lhs, Value rhs)
        {
            inst_assign(inst, lhs, rhs);
        }

        public Variable inst_store(Inst inst, ArraySection<Value> arr, int index, Value rhs)
        {
            // Every variable is assigned once, and is uniquely identifiable.
            // We must create a new variable for lhs in order to identify it.
            Variable v = Variable.Generate();
            arr[index] = v;
            inst_assign(inst, v, rhs);
            return v;
        }

        public Value inst_address_of(Inst inst, Value expr)
        {
            AddressOf e = new AddressOf();
            e.v = expr as Variable;
            return e;
        }

        public Value inst_indirect_of(Inst inst, Value expr)
        {
            DerefOf e = new DerefOf();
            e.v = expr as Variable;
            return e;
        }

        public Variable inst_load_element(Inst inst, Value obj, Value index)
        {
            Variable v = Variable.Generate();
            return v;
        }

        public Variable inst_load_field(Inst inst, Value obj, Mono.Cecil.FieldReference field)
        {
            Variable v = Variable.Generate();
            Field f = new Field();
            f.obj = obj;
            f.field = field;
            inst_assign(inst, v, f);
            return v;
        }

        public void inst_store_field(Inst inst, Value obj, Mono.Cecil.FieldReference field, Value val)
        {
            Field f = new Field();
            f.obj = obj;
            f.field = field;
            inst_assign(inst, f, val);
        }

        public void inst_switch(Inst inst, Value expr)
        {
            Switch s = new Switch();
            s.expression = expr;
        }
        
        public Block block(CFG.CFGVertex bb)
        {
            Block b = new Block(bb);
            return b;
        }

        class Data
        {
            public System.Reflection.AssemblyName assemblyName;
            public System.Reflection.Emit.AssemblyBuilder ab;
            public System.Reflection.Emit.ModuleBuilder mb;
            static int v = 1;

            public Data()
            {
                assemblyName = new System.Reflection.AssemblyName("DynamicAssembly" + v++);
                ab = AppDomain.CurrentDomain.DefineDynamicAssembly(
                    assemblyName,
                    System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave);
                mb = ab.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
            }
        }

        static Type CreateStructure(Type hostType, bool declare_parent_chain, bool declare_flatten_structure)
        {
            System.Console.WriteLine("CreateValueBase ht = " + hostType.FullName + " "
                + hostType.Namespace);
            try
            {
                String name;
                System.Reflection.TypeFilter tf;
                Type bbt = null;

                // Declare inheritance types.
                if (declare_parent_chain)
                {
                    // First, declare base type
                    Type bt = hostType.BaseType;
                    if (bt != null && !bt.FullName.Equals("System.Object"))
                    {
                        bbt = CreateStructure(bt, declare_parent_chain, declare_flatten_structure);
                    }
                }

                name = hostType.FullName;
                tf = new System.Reflection.TypeFilter((Type t, object o) =>
                {
                    return t.FullName == name;
                });

                // Find if blittable type for hostType was already performed.
                Data data = new Data();
                Type[] types = data.mb.FindTypes(tf, null);

                // If blittable type was not created, create one with all fields corresponding
                // to that in host, with special attention to arrays.
                if (types.Length == 0)
                {
                    if (hostType.IsArray)
                    {
                        // Recurse
                        Type elementType = CreateStructure(hostType.GetElementType(), declare_parent_chain, declare_flatten_structure);
                        object array_obj = System.Array.CreateInstance(elementType, 0);
                        Type array_type = array_obj.GetType();
                        System.Reflection.Emit.TypeBuilder tb = null;
                        if (bbt != null)
                        {
                            tb = data.mb.DefineType(
                                array_type.Name,
                                System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.SequentialLayout
                                    | System.Reflection.TypeAttributes.Serializable, bbt);
                        }
                        else
                        {
                            tb = data.mb.DefineType(
                                array_type.Name,
                                System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.SequentialLayout
                                    | System.Reflection.TypeAttributes.Serializable);
                        }
                        Type tbc = tb.CreateType();
                        System.Console.WriteLine("CreateValueBase returning = " + tbc.FullName + " "
                            + tbc.Namespace);
                        return tbc;
                    }
                    else if (Campy.Types.Utils.ReflectionCecilInterop.IsStruct(hostType) || hostType.IsClass)
                    {
                        System.Reflection.Emit.TypeBuilder tb = null;
                        if (bbt != null)
                        {
                            tb = data.mb.DefineType(
                                name,
                                System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.SequentialLayout
                                    | System.Reflection.TypeAttributes.Serializable, bbt);
                        }
                        else
                        {
                            tb = data.mb.DefineType(
                                name,
                                System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.SequentialLayout
                                    | System.Reflection.TypeAttributes.Serializable);
                        }
                        Type ht = hostType;
                        while (ht != null)
                        {
                            var fields = ht.GetFields(
                                System.Reflection.BindingFlags.Instance
                                | System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.Public
                                | System.Reflection.BindingFlags.Static);
                            var fields2 = ht.GetFields();
                            foreach (var field in fields)
                            {
                                if (field.FieldType.IsArray)
                                {
                                    tb.DefineField(field.Name, typeof(Variable), System.Reflection.FieldAttributes.Public);
                                }
                                else
                                {
                                    // For non-array type fields, just define the field as is.
                                    tb.DefineField(field.Name, typeof(Variable), System.Reflection.FieldAttributes.Public);
                                }
                            }
                            if (declare_flatten_structure)
                                ht = ht.BaseType;
                            else
                                ht = null;
                        }
                        // Base type will be used.
                        Type tbc = tb.CreateType();
                        System.Console.WriteLine("CreateValueBase returning = " + tbc.FullName + " "
                            + tbc.Namespace);
                        return tbc;
                    }
                    else return null;
                }
                else
                    return types[0];
            }
            catch
            {
                return null;
            }
        }

        public Structure alloc_structure(Mono.Cecil.TypeReference tr)
        {
            Structure result = new Structure();
            //Type srtr = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionType(tr);
            //Type t = CreateStructure(srtr, true, false);
            //object obj = Activator.CreateInstance(t);
            //result._obj = obj;
            return result;
        }

        public Array alloc_array(Mono.Cecil.TypeReference tr)
        {
            Array result = new Array();
            Type srtr = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionType(tr);
            Type t = CreateStructure(srtr, true, false);
            object obj = Activator.CreateInstance(t);
            result._obj = obj;
            return result;
        }

        public Variable integer32(Inst inst, Variable v, int c)
        {
            Integer32 i = new Integer32(c);
            inst_assign(inst, v, i);
            return v;
        }

        public Variable uinteger32(Inst inst, uint c)
        {
            UInteger32 i = new UInteger32(c);
            Variable v = Variable.Generate();
            inst_assign(inst, v, i);
            return v;
        }

        public Variable integer64(Inst inst, long c)
        {
            Integer64 i = new Integer64(c);
            Variable v = Variable.Generate();
            inst_assign(inst, v, i);
            return v;
        }

        public Variable uinteger64(Inst inst, ulong c)
        {
            UInteger64 i = new UInteger64(c);
            Variable v = Variable.Generate();
            inst_assign(inst, v, i);
            return v;
        }

        public Variable floatingpoint32(Inst inst, float c)
        {
            FloatingPoint32 i = new FloatingPoint32(c);
            Variable v = Variable.Generate();
            inst_assign(inst, v, i);
            return v;
        }

        public Variable floatingpoint64(Inst inst, double c)
        {
            FloatingPoint64 i = new FloatingPoint64(c);
            Variable v = Variable.Generate();
            inst_assign(inst, v, i);
            return v;
        }

        public Branch conditional_branch(Inst inst, Value expr, Block address_true, Block address_false)
        {
            // Every variable is assigned once, and is uniquely identifiable.
            Branch b = new Branch();
            b.expression = expr;
            b.address_true = address_true;
            b.address_false = address_false;
            _operation.Add(inst, b);
            return b;
        }

        public Branch unconditional_branch(Inst inst, Block address)
        {
            // Every variable is assigned once, and is uniquely identifiable.
            Branch b = new Branch();
            b.address_true = address;
            _operation.Add(inst, b);
            return b;
        }


        public Variable add(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.add;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable sub(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.sub;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable mul(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.mul;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable div(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.div;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable mod(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.add;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable and(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.and;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable or(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.or;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable xor(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.xor;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable not(Inst inst, Value expr)
        {
            SSA.UnaryExpression e = new SSA.UnaryExpression();
            e.expr = expr;
            e.op = Operator.not;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable neg(Inst inst, Value expr)
        {
            SSA.UnaryExpression e = new SSA.UnaryExpression();
            e.expr = expr;
            e.op = Operator.neg;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable clt(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.lt;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable cle(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.le;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable cgt(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.gt;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable cge(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.ge;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable ceq(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.eq;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable cne(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = Operator.ne;
            Variable v = Variable.Generate();
            inst_assign(inst, v, e);
            return v;
        }

        public Value binary_expression(Operator op, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression();
            e.LHS = lhs;
            e.RHS = rhs;
            e.op = op;
            return e;
        }
    }
}
