using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campy.Utils;

namespace Campy
{
    // Call stack handled by pointers to previous
    // call frame, and additional assignments for
    // args/params. When evaluating, the call stack
    // of the current instruction must match call
    // frame for which it is being evaluated.
    public class Nesting
    {
        public Inst _caller;
        public List<SSA.Assignment> _parameter_argument_matching = new List<SSA.Assignment>();
        public Nesting _previous;
    }

    public class State
    {
        public List<Nesting> _bindings;

        // See ECMA 335, page 82.
        public StackQueue<SSA.Value> _stack;

        // _arguments and _locals are objects that actually point
        // to _stack.
        public ArraySection<SSA.Value> _arguments;
        public ArraySection<SSA.Value> _locals;
        
        // _memory contains all objects that are allocated
        // such as classes and arrays.
        public Dictionary<String, SSA.Value> _memory;

        public State()
        {
            _arguments = null;
            _locals = null;
            _memory = new Dictionary<string, SSA.Value>();
            _bindings = new List<Nesting>();
            _bindings.Add(new Nesting());
        }

        public State(Mono.Cecil.MethodDefinition md, int level)
        {
            int args = 0;
            if (md.HasThis) args++;
            args += md.Parameters.Count;
            int locals = md.Body.Variables.Count;

            // Create a stack with variables, which will be
            // bound to phi functions.
            _stack = new StackQueue<SSA.Value>();
            // Allocate parameters, even though we don't what they may be.
            _arguments = _stack.Section(_stack.Count, args);
            for (int i = 0; i < args; ++i)
                _stack.Push(new SSA.Variable());

            // Allocate local variables.
            _locals = _stack.Section(_stack.Count, locals);
            for (int i = 0; i < locals; ++i)
                _stack.Push(new SSA.Variable());

            for (int i = _stack.Size(); i < level; ++i)
                _stack.Push(new SSA.Variable());

            _bindings = new List<Nesting>();
            _bindings.Add(new Nesting());
        }

        public State(State other)
        {
            _stack = new StackQueue<SSA.Value>();
            for (int i = 0; i < other._stack.Count; ++i)
            {
                _stack.Push(other._stack.PeekBottom(i));
            }
            _arguments = _stack.Section(other._arguments.Base, other._arguments.Len);
            _locals = _stack.Section(other._locals.Base, other._locals.Len);
            _bindings = new List<Nesting>();
            _bindings.AddRange(other._bindings);
        }

        public void Dump()
        {
            int args = _arguments.Len;
            int locs = _locals.Len;
            System.Console.Write("[args");
            for (int i = 0; i < args; ++i)
                System.Console.Write(" " + _stack[i]);
            System.Console.Write("]");
            System.Console.Write("[locs");
            for (int i = 0; i < locs; ++i)
                System.Console.Write(" " + _stack[args + i]);
            System.Console.Write("]");
            for (int i = args + locs; i < _stack.Size(); ++i)
                System.Console.Write(" " + _stack[i]);
            System.Console.WriteLine();
        }

        //public static bool operator ==(State a, State b)
        //{
        //    object oa = (object)a;
        //    object ob = (object)b;
        //    if (oa == null && ob == null)
        //        return true;
        //    if (oa == null && ob != null)
        //        return false;
        //    if (oa != null && ob == null)
        //        return false;
        //    if (a._stack.Size() != b._stack.Size())
        //        return false;
        //    for (int i = 0; i < a._stack.Size(); ++i)
        //        if (a._stack[i] != b._stack[i])
        //            return false;
        //    return true;
        //}

        //public static bool operator !=(State a, State b)
        //{
        //    return !(a == b);
        //}
    }
}
