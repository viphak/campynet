using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campy.Utils;

namespace Campy
{
    public class State
    {
        // See ECMA 335, page 82.
        public StackQueue<ValueBase> _stack;
        public ArraySection<ValueBase> _arguments;
        public ArraySection<ValueBase> _locals;
        public System.Reflection.MethodInfo _method_info;
        public Dictionary<String, ValueBase> _memory;
        public Dictionary<String, ValueBase> _registers;

        public State()
        {
            _stack = new StackQueue<ValueBase>();
            _arguments = null;
            _locals = null;
            _method_info = null;
            _memory = new Dictionary<string, ValueBase>();
            _registers = new Dictionary<string, ValueBase>();
        }

        public State(Mono.Cecil.MethodDefinition md, int level)
        {
            int args = 0;
            if (md.HasThis) args++;
            args += md.Parameters.Count;
            int locals = md.Body.Variables.Count;

            // Create an empty stack.
            _stack = new StackQueue<ValueBase>();
            // Allocate parameters, even though we don't what they may be.
            _arguments = _stack.Section(_stack.Count, args);
            for (int i = 0; i < args; ++i)
                _stack.Push(ValueBase.Top);

            // Allocate local variables.
            _locals = _stack.Section(_stack.Count, locals);
            for (int i = 0; i < locals; ++i)
                _stack.Push(ValueBase.Top);

            for (int i = _stack.Size(); i < level; ++i)
                _stack.Push(ValueBase.Top);
        }

        public State(State other)
        {
            _stack = new StackQueue<ValueBase>();
            for (int i = 0; i < other._stack.Count; ++i)
            {
                _stack.Push(other._stack.PeekBottom(i));
            }
            _arguments = _stack.Section(other._arguments.Base, other._arguments.Len);
            _locals = _stack.Section(other._locals.Base, other._locals.Len);
        }

        public void Union(CFG.CFGVertex pred)
        {
            State other = pred.StateOut;
            // All stack elements are joined with
            // with other stack elements.
            for (int i = 0; i < this._stack.Size(); ++i)
            {
                this._stack[i] = this._stack[i].Join(other._stack[i]);
            }
        }

        public void UnionCall(CFG.CFGVertex pred)
        {
            // Caller exists in one block of different method.
            // This block is entry of callee.
            State caller = pred.StatePreLastInstruction;
            // This state is callee. A separate state is provided,
            // so caller arguments must be copyied to the callee.
            for (int i = 0; i < _arguments.Len; ++i)
                _arguments[i] = caller._stack[_arguments.Len - i - 1];
        }

        public void UnionReturn(CFG.CFGVertex pred)
        {
            State callee = pred.StateOut;
            // This block is return from another method.
            // Args were already popped.
            int ret = 0;
            object method = pred.Instructions.Last().Operand;
            if (method as Mono.Cecil.MethodReference != null)
            {
                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
            }

            // Push result, if any.
            if (ret == 1)
                this._stack.Push(callee._stack.PeekTop());
        }

        public void Dump()
        {
            int args = _arguments.Len;
            int locs = _locals.Len;
            System.Console.Write("[args");
            for (int i = 0; i < args; ++i)
                _stack[i].Dump();
            System.Console.Write("]");
            System.Console.Write("[locs");
            for (int i = 0; i < locs; ++i)
                _stack[args + i].Dump();
            System.Console.Write("]");
            for (int i = args + locs; i < _stack.Size(); ++i)
                _stack[i].Dump();
            System.Console.WriteLine();
        }

        public static bool operator ==(State a, State b)
        {
            object oa = (object)a;
            object ob = (object)b;
            if (oa == null && ob == null)
                return true;
            if (oa == null && ob != null)
                return false;
            if (oa != null && ob == null)
                return false;
            if (a._stack.Size() != b._stack.Size())
                return false;
            for (int i = 0; i < a._stack.Size(); ++i)
                if (a._stack[i] != b._stack[i])
                    return false;
            return true;
        }

        public static bool operator !=(State a, State b)
        {
            return !(a == b);
        }
    }
}
