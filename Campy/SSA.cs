using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campy.Utils;
using Campy.Graphs;

namespace Campy
{
    public class SSA : GraphLinkedList<object, SSA.SSAVertex, SSA.SSAEdge>
    {
        static SSA _ssa;
        public MultiMap<SSA.Value, Inst> _defined = new MultiMap<SSA.Value, Inst>(new SSA.ValueCompare());

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

        public class Value : IComparable
        {
            public virtual int CompareTo(object obj)
            {
                throw new ArgumentException("Unimplemented in derived type.");
            }

            public override bool Equals(Object obj)
            {
                throw new ArgumentException("Unimplemented in derived type.");
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                throw new ArgumentException("Unimplemented in derived type.");
            }

            public static bool operator ==(Value a, Value b)
            {
                throw new ArgumentException("Unimplemented in derived type.");
            }

            public static bool operator !=(Value a, Value b)
            {
                throw new ArgumentException("Unimplemented in derived type.");
            }
        }

        public class AddressOf : Value
        {
            public Value _v;

            public AddressOf(Value x)
            {
                _v = x;
            }

            public override string ToString()
            {
                return _v.ToString();
            }
        }

        public class Array : Value
        {
            String _name;
            static int id = 0;

            public Array()
            {
                _name = "a" + id++;
            }

            public override string ToString()
            {
                return _name;
            }
        }

        public class ArrayElement : Value
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

            public BinaryExpression(Operator o, Value l, Value r)
            {
                op = o;
                LHS = l;
                RHS = r;
            }

            public override string ToString()
            {
                return LHS.ToString()
                    + op.ToString()
                    + RHS.ToString();
            }

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.op < this.op)
                    return -1;

                if (p.op > this.op)
                    return 1;

                if (p.LHS < this.LHS)
                    return -1;

                if (p.LHS > this.LHS)
                    return 1;

                if (p.RHS < this.RHS)
                    return -1;

                if (p.RHS > this.RHS)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.op == this.op && p.LHS == this.LHS && p.RHS == this.RHS);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Field));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Field));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.op == p2.op && p1.LHS == p2.LHS && p1.RHS == p2.RHS);
            }

            public static bool operator ==(BinaryExpression p1, BinaryExpression p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.op == p2.op && p1.LHS == p2.LHS && p1.RHS == p2.RHS);
            }

            public static bool operator !=(BinaryExpression p1, BinaryExpression p2)
            {
                return !(p1 == p2);
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
                return "node " + _block;
            }

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p._block.ID < this._block.ID)
                    return -1;

                if (p._block.ID > this._block.ID)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p._block.ID == this._block.ID);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Block));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Block));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1._block.ID == p2._block.ID);
            }

            public static bool operator ==(Block p1, Block p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1._block.ID == p2._block.ID);
            }

            public static bool operator !=(Block p1, Block p2)
            {
                return !(p1 == p2);
            }
        }

        public class Field : Value
        {
            public Value _obj;
            public Mono.Cecil.FieldReference _field;

            public Field(Value o, Mono.Cecil.FieldReference f)
            {
                if (System.Object.ReferenceEquals(o, null))
                    throw new Exception("Constructor of Field has null object.");
                if (System.Object.ReferenceEquals(f, null))
                    throw new Exception("Constructor of field has null field.");
                _obj = o;
                _field = f;
            }

            public override string ToString()
            {
                return
                    "Field "
                    + _field
                    + " of "
                    + _obj;
            }

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p._obj < this._obj)
                    return -1;

                if (p._obj > this._obj)
                    return 1;

                if (p._field < this._field)
                    return -1;

                if (p._field > this._field)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                bool r1 = p._obj.Equals(this._obj);
                bool r2 = this._obj.Equals(p._obj);
                SSA.Field asdf = p as SSA.Field;
                bool r3 = asdf._obj.Equals(this._obj);
                bool r4 = this._obj.Equals(asdf._obj);

                return (p._obj.Equals(this._obj) && p._field == this._field);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Field));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Field));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1._obj == p2._obj && p1._field == p2._field);
            }

            public static bool operator ==(Field p1, Field p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1._obj == p2._obj && p1._field == p2._field);
            }

            public static bool operator !=(Field p1, Field p2)
            {
                return !(p1 == p2);
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

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.Value < this.Value)
                    return -1;

                if (p.Value > this.Value)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.Value == this.Value);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(FloatingPoint32));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(FloatingPoint32));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.Value == p2.Value);
            }

            public static bool operator ==(FloatingPoint32 p1, FloatingPoint32 p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.Value == p2.Value);
            }

            public static bool operator !=(FloatingPoint32 p1, FloatingPoint32 p2)
            {
                return !(p1 == p2);
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

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.Value < this.Value)
                    return -1;

                if (p.Value > this.Value)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.Value == this.Value);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(FloatingPoint64));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(FloatingPoint64));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.Value == p2.Value);
            }

            public static bool operator ==(FloatingPoint64 p1, FloatingPoint64 p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.Value == p2.Value);
            }

            public static bool operator !=(FloatingPoint64 p1, FloatingPoint64 p2)
            {
                return !(p1 == p2);
            }
        }

        public class Indirect : Variable
        {
            public Variable value;

            public Indirect(Variable v)
            {
                value = v;
            }

            public override string ToString()
            {
                return "["
                    + value.ToString()
                    + "]";
            }

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.value < this.value)
                    return -1;

                if (p.value > this.value)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.value == this.value);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Indirect));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Indirect));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.value == p2.value);
            }

            public static bool operator ==(Indirect p1, Indirect p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.value == p2.value);
            }

            public static bool operator !=(Indirect p1, Indirect p2)
            {
                return !(p1 == p2);
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

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.Value < this.Value)
                    return -1;

                if (p.Value > this.Value)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.Value == this.Value);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Integer32));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Integer32));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.Value == p2.Value);
            }

            public static bool operator ==(Integer32 p1, Integer32 p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.Value == p2.Value);
            }

            public static bool operator !=(Integer32 p1, Integer32 p2)
            {
                return !(p1 == p2);
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

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.Value < this.Value)
                    return -1;

                if (p.Value > this.Value)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.Value == this.Value);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Integer64));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Integer64));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.Value == p2.Value);
            }

            public static bool operator ==(Integer64 p1, Integer64 p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.Value == p2.Value);
            }

            public static bool operator !=(Integer64 p1, Integer64 p2)
            {
                return !(p1 == p2);
            }
        }

        public class Obj : Value
        {
            public String type;

            public Obj(String t)
            {
                type = t;
            }

            public override string ToString()
            {
                return type.ToString();
            }
        }

        public class Phi : Value
        {
            public Value _v;
            public List<Value> _merge;
            public CFG.CFGVertex _block;

            public override string ToString()
            {
                String result = this._v.ToString() + "("
                    + this._merge.Aggregate(
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

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.v < this._v)
                    return -1;

                if (p.v > this._v)
                    return 1;

                for (int i = 0; i < p.merge.Count; ++i)
                {
                    if (i >= this._merge.Count)
                        return -1;
                    if (p.merge[i] < this._merge[i])
                        return -1;
                    if (p.merge[i] > this._merge[i])
                        return 1;
                }

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (this.GetType() != o.GetType())
                {
                    return false;
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                if (p.merge.Count != this._merge.Count)
                    return false;

                for (int i = 0; i < p.merge.Count; ++i)
                {
                    if (p.merge[i] != this._merge[i])
                        return false;
                }
                return true;
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Phi));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Phi));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                if (p1.merge.Count != p2.merge.Count)
                    return false;

                for (int i = 0; i < p1.merge.Count; ++i)
                {
                    if (p1.merge[i] != p2.merge[i])
                        return false;
                }
                return true;
            }

            public static bool operator ==(Phi p1, Phi p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                if (p1._merge.Count != p2._merge.Count)
                    return false;

                for (int i = 0; i < p1._merge.Count; ++i)
                {
                    if (p1._merge[i] != p2._merge[i])
                        return false;
                }
                return true;
            }

            public static bool operator !=(Phi p1, Phi p2)
            {
                return !(p1 == p2);
            }
        }

        public class Set : Value
        {
            public List<Value> list;

            public Set()
            {
                list = new List<Value>();
            }

            public void Add(Value v)
            {
                if (list.Contains(v))
                    return;
                bool inserted = false;
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].CompareTo(v) > 0)
                    {
                        list.Insert(i, v);
                        inserted = true;
                        break;
                    }
                }
                if (! inserted)
                    list.Add(v);
            }

            public override string ToString()
            {
                String result = "Set {";
                foreach (SSA.Value v in list)
                {
                    result += " " + v;
                }
                result += " }";
                return result;
            }

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                for (int i = 0; i < p.list.Count; ++i)
                {
                    if (i >= this.list.Count)
                        return -1;
                    if (p.list[i] < this.list[i])
                        return -1;
                    if (p.list[i] > this.list[i])
                        return 1;
                }

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (this.GetType() != o.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                if (p.list.Count != this.list.Count)
                    return false;

                for (int i = 0; i < p.list.Count; ++i)
                {
                    if (p.list[i] != this.list[i])
                        return false;
                }
                return true;
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Set));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Set));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                if (p1.list.Count != p2.list.Count)
                    return false;

                for (int i = 0; i < p1.list.Count; ++i)
                {
                    if (p1.list[i] != p2.list[i])
                        return false;
                }
                return true;
            }

            public static bool operator ==(Set p1, Set p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                if (p1.list.Count != p2.list.Count)
                    return false;

                for (int i = 0; i < p1.list.Count; ++i)
                {
                    if (p1.list[i] != p2.list[i])
                        return false;
                }
                return true;
            }

            public static bool operator !=(Set p1, Set p2)
            {
                return !(p1 == p2);
            }
        }

        public class StaticField : Value
        {
            public Mono.Cecil.FieldReference field;

            public StaticField(Mono.Cecil.FieldReference f)
            {
                field = f;
            }

            public override string ToString()
            {
                return
                    "StaticField "
                    + field;
            }

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.field < this.field)
                    return -1;

                if (p.field > this.field)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.field == this.field);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(StaticField));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(StaticField));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.field == p2.field);
            }

            public static bool operator ==(StaticField p1, StaticField p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.field == p2.field);
            }

            public static bool operator !=(StaticField p1, StaticField p2)
            {
                return !(p1 == p2);
            }
        }

        public class Structure : Value
        {
            public String name;
            public Type type;
            static int id = 0;

            public Structure()
            {
                name = "s" + id++;
            }

            public override string ToString()
            {
                return name;
            }

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (String.Compare(p.name, this.name) < 0)
                    return -1;

                if (String.Compare(p.name, this.name) > 0)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return String.Compare(p.name, this.name) == 0;
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Structure));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Structure));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return String.Compare(p1.name, p2.name) == 0;
            }

            public static bool operator ==(Structure p1, Structure p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return String.Compare(p1.name, p2.name) == 0;
            }

            public static bool operator !=(Structure p1, Structure p2)
            {
                return !(p1 == p2);
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

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.expr < this.expr)
                    return -1;

                if (p.expr > this.expr)
                    return 1;

                if (p.op < this.op)
                    return -1;

                if (p.op > this.op)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.expr == this.expr && p.op == this.op);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(UnaryExpression));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(UnaryExpression));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.expr == p2.expr && p1.op == p2.op);
            }

            public static bool operator ==(UnaryExpression p1, UnaryExpression p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.expr == p2.expr && p1.op == p2.op);
            }

            public static bool operator !=(UnaryExpression p1, UnaryExpression p2)
            {
                return !(p1 == p2);
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

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.Value < this.Value)
                    return -1;

                if (p.Value > this.Value)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.Value == this.Value);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(UInteger32));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(UInteger32));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.Value == p2.Value);
            }

            public static bool operator ==(UInteger32 p1, UInteger32 p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.Value == p2.Value);
            }

            public static bool operator !=(UInteger32 p1, UInteger32 p2)
            {
                return !(p1 == p2);
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

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (p.Value < this.Value)
                    return -1;

                if (p.Value > this.Value)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetType() != this.GetType())
                    return false;

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return (p.Value == this.Value);
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(UInteger64));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(UInteger64));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return (p1.Value == p2.Value);
            }

            public static bool operator ==(UInteger64 p1, UInteger64 p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return (p1.Value == p2.Value);
            }

            public static bool operator !=(UInteger64 p1, UInteger64 p2)
            {
                return !(p1 == p2);
            }
        }

        public class Variable : Value
        {
            public String Name;
            static int next;

            public Variable()
            {
                next++;
                Name = "v" + next;
            }

            public override string ToString()
            {
                return this.Name;
            }

            public override int CompareTo(object o)
            {
                if (o == null)
                {
                    return 1;
                }

                if (this.GetType() != o.GetType())
                {
                    // Order by string name of type.
                    String this_name = this.GetType().FullName;
                    String o_name = o.GetType().FullName;
                    return String.Compare(this_name, o_name);
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return -1;
                }

                if (String.Compare(p.Name, this.Name) < 0)
                    return -1;

                if (String.Compare(p.Name, this.Name) > 0)
                    return 1;

                return 0;
            }

            public override bool Equals(Object o)
            {
                if (o == null)
                {
                    return false;
                }

                dynamic p = Convert.ChangeType(o, this.GetType());
                if ((System.Object)p == null)
                {
                    return false;
                }

                return String.Compare(p.Name, this.Name) == 0;
            }

            static public new bool Equals(Object obj1, Object obj2) /* override */
            {
                if (System.Object.ReferenceEquals(obj1, obj2))
                    return true;

                if (System.Object.ReferenceEquals(obj1, null))
                    return false;

                if (System.Object.ReferenceEquals(obj2, null))
                    return false;

                if (obj1.GetType() != obj2.GetType())
                    return false;

                dynamic p1 = Convert.ChangeType(obj1, typeof(Variable));
                if ((System.Object)p1 == null)
                {
                    return false;
                }

                dynamic p2 = Convert.ChangeType(obj1, typeof(Variable));
                if ((System.Object)p2 == null)
                {
                    return false;
                }

                return String.Compare(p1.Name, p2.Name) == 0;
            }

            public static bool operator ==(Variable p1, Variable p2)
            {
                if (System.Object.ReferenceEquals(p1, p2))
                    return true;

                if (System.Object.ReferenceEquals(p1, null))
                    return false;

                if (System.Object.ReferenceEquals(p2, null))
                    return false;

                if (p1.GetType() != p2.GetType())
                    return false;

                return String.Compare(p1.Name, p2.Name) == 0;
            }

            public static bool operator !=(Variable p1, Variable p2)
            {
                return !(p1 == p2);
            }
        }

        public class ValueCompare : IEqualityComparer<Value>
        {
            public int GetHashCode(Value v)
            {
                return v.GetHashCode();
            }

            public bool Equals(Value a, Value b)
            {
                if (System.Object.ReferenceEquals(a, null))
                {
                    if (System.Object.ReferenceEquals(b, null))
                        return true;
                    return b.Equals(a);
                }
                else
                {
                    return a.Equals(b);
                }
            }

            public static bool Eq(Value a, Value b)
            {
                if (System.Object.ReferenceEquals(a, null))
                {
                    if (System.Object.ReferenceEquals(b, null))
                        return true;
                    return b.Equals(a);
                }
                else
                {
                    return a.Equals(b);
                }
            }
        }

        //============================================================
        //
        // SSA OPERATIONS
        //
        //============================================================

        public class Operation : SSAVertex
        {
        }

        public class Assignment : Operation
        {
            public Value lhs;
            public Value rhs;

            public override string ToString()
            {
                String result = "";
                result += lhs;
                result += " := ";
                result += rhs;
                return result;
            }
        }

        public class Branch : Operation
        {
            public Value expression;
            public Block address_true;
            public Block address_false;

            public override string ToString()
            {
                String result = "";
                result += "If ";
                result += expression;
                if (address_true != null)
                {
                    result += " then branch to ";
                    result += address_true;
                }
                if (address_false != null)
                {
                    result += " else branch to ";
                    result += address_false;
                }
                return result;
            }
        }

        public class Switch : Operation
        {
            public Value expression;
            public Block[] addrs;
        }

        //public StackQueue<Variable> _stack = new StackQueue<Variable>();

        public MultiMap<Inst, Operation> _operation = new MultiMap<Inst, Operation>();

        public Assignment inst_assign(Inst inst, Value lhs, Value rhs)
        {
            // Every variable is assigned once, and is uniquely identifiable.
            Assignment a = new Assignment();
            a.lhs = lhs;
            a.rhs = rhs;
            _operation.Add(inst, a);
            SSA ssa = SSA.Singleton();
            ssa._defined.Add(lhs, inst);
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

        public void inst_store(Inst inst, ArraySection<Value> arr, int index, Value rhs)
        {
            // Every variable is assigned once, and is uniquely identifiable.
            // We must create a new variable for lhs in order to identify it.
            Variable v = new SSA.Variable();
            arr[index] = v;
            inst_assign(inst, v, rhs);
        }

        public Value inst_address_of(Inst inst, Value expr)
        {
            AddressOf e = new AddressOf(expr as Variable);
            return e;
        }

        public Value inst_indirect_of(Inst inst, Value expr)
        {
            Indirect e = new Indirect(expr as Variable);
            return e;
        }

        public Variable inst_load_element(Inst inst, Value obj, Value index)
        {
            Variable v = new SSA.Variable();
            // Go to state and load element.
            return v;
        }

        public void inst_store_element(Inst inst, Value arr, Value index, Value obj)
        {
        }

        public Variable inst_load_field(Inst inst, Value obj, Mono.Cecil.FieldReference field)
        {
            Variable v = new SSA.Variable();
            Field f = new Field(obj, field);
            inst_assign(inst, v, f);
            return v;
        }

        public void inst_store_field(Inst inst, Value obj, Mono.Cecil.FieldReference field, Value val)
        {
            Field f = new Field(obj, field);
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
                                    tb.DefineField(field.Name, typeof(SSA.Value), System.Reflection.FieldAttributes.Public);
                                }
                                else
                                {
                                    // For non-array type fields, just define the field as is.
                                    tb.DefineField(field.Name, typeof(SSA.Value), System.Reflection.FieldAttributes.Public);
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

        public Structure alloc_structure(State state, Mono.Cecil.TypeReference tr)
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
            //Type srtr = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionType(tr);
            //Type t = CreateStructure(srtr, true, false);
            //object obj = Activator.CreateInstance(t);
            //result._obj = obj;
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
            Variable v = new SSA.Variable();
            inst_assign(inst, v, i);
            return v;
        }

        public Variable integer64(Inst inst, long c)
        {
            Integer64 i = new Integer64(c);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, i);
            return v;
        }

        public Variable uinteger64(Inst inst, ulong c)
        {
            UInteger64 i = new UInteger64(c);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, i);
            return v;
        }

        public Variable floatingpoint32(Inst inst, float c)
        {
            FloatingPoint32 i = new FloatingPoint32(c);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, i);
            return v;
        }

        public Variable floatingpoint64(Inst inst, double c)
        {
            FloatingPoint64 i = new FloatingPoint64(c);
            Variable v = new SSA.Variable();
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
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.add, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable sub(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.sub, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable mul(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.mul, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable div(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.div, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable mod(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.add, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable and(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.and, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable or(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.or, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable xor(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.xor, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable not(Inst inst, Value expr)
        {
            SSA.UnaryExpression e = new SSA.UnaryExpression();
            e.expr = expr;
            e.op = Operator.not;
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable neg(Inst inst, Value expr)
        {
            SSA.UnaryExpression e = new SSA.UnaryExpression();
            e.expr = expr;
            e.op = Operator.neg;
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable clt(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.lt, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable cle(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.le, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable cgt(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.gt, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable cge(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.ge, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable ceq(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.eq, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Variable cne(Inst inst, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(Operator.ne, lhs, rhs);
            Variable v = new SSA.Variable();
            inst_assign(inst, v, e);
            return v;
        }

        public Value binary_expression(Operator op, Value lhs, Value rhs)
        {
            SSA.BinaryExpression e = new SSA.BinaryExpression(op, lhs, rhs);
            return e;
        }

        public class SSAVertex
            : GraphLinkedList<object, SSA.SSAVertex, SSA.SSAEdge>.Vertex
        {
            static int kens_id = 0;
            private int _kens_id = kens_id++;

            public int ID
            {
                get { return _kens_id; }
            }

            public SSAVertex()
                : base()
            {
            }

            public SSAVertex(SSAVertex o)
                : base(o)
            {
            }
        }

        public class SSAEdge
            : GraphLinkedList<object, SSA.SSAVertex, SSA.SSAEdge>.Edge
        {
        }
    }
}
