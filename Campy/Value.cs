using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campy.Utils;


namespace Campy
{
   
    public class ValueBase
    {
        static Top _top = new Top();
        static Bottom _bottom = new Bottom();

        public static Top Top
        {
            get { return _top; }
        }

        public static Bottom Bottom
        {
            get { return _bottom; }
        }

        public ValueBase() { }

        public virtual ValueBase Join(ValueBase other)
        {
            if (this.GetType() == typeof(SetValue) && other.GetType() == typeof(SetValue))
            {
                // Add sets.
                SetValue t = (SetValue)this;
                SetValue o = (SetValue)other;
                SetValue result = new SetValue();
                result.Add(t);
                result.Add(o);
                return result;
            }
            else if (this.GetType() == typeof(SetValue))
            {
                SetValue t = (SetValue)this;
                SetValue result = new SetValue();
                result.Add(t);
                result.Add(other);
                return result;
            }
            else if (other.GetType() == typeof(SetValue))
            {
                SetValue o = (SetValue)other;
                SetValue result = new SetValue();
                result.Add(o);
                result.Add(this);
                return result;
            }
            else if (this.GetType() == typeof(Top))
            {
                return other;
            }
            else if (other.GetType() == typeof(Top))
            {
                return this;
            }
            else if (this.GetType() != other.GetType())
                return Bottom;
            else if (this.GetType().Name.Contains("RValue"))
            {
                if (this == other)
                    return this;
                else
                    return Bottom;
            }
            else
                return Bottom;
        }

        virtual public void Dump()
        {
        }


    }

    public class Top : ValueBase
    {
        public override void Dump()
        {
            System.Console.Write(" Top");
        }
    }

    public class Bottom : ValueBase
    {
        public override void Dump()
        {
            System.Console.Write(" Bottom");
        }
    }

    public class RValue<T> : ValueBase
    {
        T _val;

        public T Val
        {
            get { return _val; }
        }

        public RValue(T i)
        {
            _val = i;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(T))
                return _val.Equals((obj as RValue<T>)._val);
            return false;
        }

        public override String ToString()
        {
            return /* typeof(T).Name + */ " " + _val;
        }

        public override void Dump()
        {
            System.Console.Write(" " + this.ToString());
        }
    }

    public class LValue : ValueBase
    {
        ArraySection<ValueBase> _space;
        int _absolute;
        int _relative;

        public LValue(StackQueue<ValueBase> stack)
        {
            _space = stack.Section(1);
            _relative = 0;
            _absolute = _space.Base + _relative;
        }

        public LValue(ArraySection<ValueBase> section, int i)
        {
            _space = section;
            _relative = i;
            _absolute = _space.Base + _relative;
        }

        public ValueBase Value
        {
            get { return _space[_relative]; }
            set { _space[_relative] = value; }
        }

        public override void Dump()
        {
            System.Console.Write(" LValue=" + _space.ToString());
        }
    }

    public class SetValue : ValueBase
    {
        List<ValueBase> _set = new List<ValueBase>();

        public SetValue()
        {
        }

        public SetValue(ValueBase v)
        {
            _set.Add(v);
        }

        public void Add(ValueBase v)
        {
            ValueBase z = _set.Find((ValueBase x) =>
            {
                if (x.Equals(v))
                    return true;
                else
                    return false;
            });
            if (z != null)
                return;
            _set.Add(v);
        }

        public void Add(SetValue v)
        {
            foreach (ValueBase e in v._set)
            {
                ValueBase z = _set.Find((ValueBase x) =>
                {
                    if (x.Equals(v))
                        return true;
                    else
                        return false;
                });
                if (z == null)
                    _set.Add(e);
            }
        }

        public override String ToString()
        {
            return "Set " + _set.Select(n => n.ToString()).Aggregate((a, b) => a + ", " + b);
        }

        public override void Dump()
        {
            System.Console.Write(" "+ ToString());
        }
    }
}
