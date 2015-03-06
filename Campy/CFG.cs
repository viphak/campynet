using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Mono.Cecil;
using Campy.Graphs;
using Campy.Utils;

namespace Campy
{

    public class CFG : GraphLinkedList<object, CFG.CFGVertex, CFG.CFGEdge>
    {
        List<Assembly> _assemblies;
        static int _node_number = 1;

        public CFG()
            : base()
        {
            _assemblies = new List<Assembly>();
        }

        // Add assembly to graph.
        public void AddAssembly(Assembly assembly)
        {
            _assemblies.Add(assembly);
            ExtractBasicBlocks(assembly);
        }

        private void ExtractBasicBlocks(Assembly assembly)
        {
            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = assembly.Location;

            // Use Mono.Cecil to get all instructions, functions, entry points, etc.
            // First, decompile entire module.
            ModuleDefinition md = ModuleDefinition.ReadModule(kernel_assembly_file_name);

            // Examine all types, then all methods of types in order to find all blocks.
            List<Type> types = new List<Type>();
            StackQueue<TypeDefinition> type_definitions = new StackQueue<TypeDefinition>();
            StackQueue<TypeDefinition> type_definitions_closure = new StackQueue<TypeDefinition>();
            foreach (TypeDefinition td in md.Types)
            {
                type_definitions.Push(td);
            }
            while (type_definitions.Count > 0)
            {
                TypeDefinition ty = type_definitions.Pop();
                type_definitions_closure.Push(ty);
                foreach (TypeDefinition ntd in ty.NestedTypes)
                    type_definitions.Push(ntd);
            }
            foreach (TypeDefinition td in type_definitions_closure)
            {
                foreach (MethodDefinition md2 in td.Methods)
                {
                    int count = md2.Body.Instructions.Count;
                    StackQueue<Mono.Cecil.Cil.Instruction> leader_list = new StackQueue<Mono.Cecil.Cil.Instruction>();

                    // Each method is a leader of a block.
                    CFGVertex v = (CFGVertex)this.AddVertex(_node_number++);
                    v.Method = md2;
                    v.HasReturn = md2.IsReuseSlot;
                    v._entry = v;
                    v._ordered_list_of_blocks = new List<CFGVertex>();
                    v._ordered_list_of_blocks.Add(v);
                    for (int j = 0; j < count; ++j)
                    {
                        // accumulate jump to locations since these split blocks.
                        Mono.Cecil.Cil.Instruction mi = md2.Body.Instructions[j];
                        Inst i = Inst.Wrap(mi);
                        Mono.Cecil.Cil.OpCode op = i.OpCode;
                        Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                        v._instructions.Add(i);
                        if (fc == Mono.Cecil.Cil.FlowControl.Next)
                            continue;
                        if (fc == Mono.Cecil.Cil.FlowControl.Branch)
                        {
                            // Save leader target of branch.
                            object o = i.Operand;
                            leader_list.Push(o as Mono.Cecil.Cil.Instruction);
                        }
                    }
                    StackQueue<int> ordered_leader_list = new StackQueue<int>();
                    for (int j = 0; j < count; ++j)
                    {
                        // Order jump targets. These denote locations
                        // where to split blocks. However, it's ordered,
                        // so that splitting is done from last instruction in block
                        // to first instruction in block.
                        Mono.Cecil.Cil.Instruction i = md2.Body.Instructions[j];
                        if (leader_list.Contains(i))
                            ordered_leader_list.Push(j);
                    }
                    // Split block at jump targets in reverse.
                    while (ordered_leader_list.Count > 0)
                    {
                        int i = ordered_leader_list.Pop();
                        CFG.CFGVertex new_node = v.Split(i);
                    }
                }
            }

            this.Dump();

            StackQueue<CFG.CFGVertex> stack = new StackQueue<CFG.CFGVertex>();
            foreach (CFG.CFGVertex node in this.VertexNodes) stack.Push(node);
            while (stack.Count > 0)
            {
                // Split blocks at branches, including calls, with following
                // instruction a leader of new block.
                CFG.CFGVertex node = stack.Pop();
                int count = node._instructions.Count;
                for (int j = 0; j < count; ++j)
                {
                    Inst i = node._instructions[j];
                    Mono.Cecil.Cil.OpCode op = i.OpCode;
                    Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                    if (fc == Mono.Cecil.Cil.FlowControl.Next)
                        continue;
                    if (j + 1 >= count)
                        continue;
                    CFG.CFGVertex new_node = node.Split(j + 1);
                    stack.Push(new_node);
                    break;
                }
            }

            this.Dump();
            stack = new StackQueue<CFG.CFGVertex>();
            foreach (CFG.CFGVertex node in this.VertexNodes) stack.Push(node);
            while (stack.Count > 0)
            {
                // Add in all final non-fallthrough branch edges.
                CFG.CFGVertex node = stack.Pop();
                int count = node._instructions.Count;
                Inst i = node._instructions[count - 1];
                Mono.Cecil.Cil.OpCode op = i.OpCode;
                Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                switch (fc)
                {
                    case Mono.Cecil.Cil.FlowControl.Branch:
                    case Mono.Cecil.Cil.FlowControl.Cond_Branch:
                        {
                            Mono.Cecil.Cil.Instruction target_instruction = i.Operand as Mono.Cecil.Cil.Instruction;
                            CFGVertex target_node = this.VertexNodes.First(
                                (CFGVertex x) =>
                                {
                                    if (!x._instructions.First().Instruction.Equals(target_instruction))
                                        return false;
                                    return true;
                                });
                            System.Console.WriteLine("Create edge a " + node.Name + " to " + target_node.Name);
                            this.AddEdge(node, target_node);
                            break;
                        }
                    case Mono.Cecil.Cil.FlowControl.Break:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Call:
                        {
                            object o = i.Operand;
                            if (o as Mono.Cecil.MethodReference != null)
                            {
                                Mono.Cecil.MethodReference r = o as Mono.Cecil.MethodReference;
                                IEnumerable<CFGVertex> target_node_list = this.VertexNodes.Where(
                                    (CFGVertex x) =>
                                    {
                                        return x.Method.FullName == r.FullName
                                            && x._entry == x;
                                    });
                                int c = target_node_list.Count();
                                if (c == 1)
                                {
                                    // target_node is the entry for a method. Also get the exit.
                                    CFGVertex target_node = target_node_list.First();
                                    CFGVertex exit_node = target_node.Exit;
                                    // check if this is a recursive call. DO NOT BOTHER!!
                                    if (node.Method == target_node.Method)
                                    {
                                    }
                                    else
                                    {
                                        // Create edges from exit to successor blocks of the call.
                                        foreach (CFGEdge e in node._Successors)
                                        {
                                            System.Console.WriteLine("Create edge c " + exit_node.Name + " to " + e.to.Name);
                                            this.AddEdge(exit_node, e.to);
                                        }
                                        // Create edge from node to method entry.
                                        System.Console.WriteLine("Create edge b " + node.Name + " to " + target_node.Name);
                                        this.AddEdge(node, target_node);
                                    }
                                }
                            }
                            break;
                        }
                    case Mono.Cecil.Cil.FlowControl.Meta:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Next:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Phi:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Return:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Throw:
                        break;
                }
            }

            this.Dump();
        }

        bool IsConditional(Mono.Cecil.Cil.Instruction i)
        {
            switch (i.OpCode.FlowControl)
            {
                case Mono.Cecil.Cil.FlowControl.Branch:
                    return false;
                case Mono.Cecil.Cil.FlowControl.Break:
                    return false;
                case Mono.Cecil.Cil.FlowControl.Call:
                    return false;
                case Mono.Cecil.Cil.FlowControl.Cond_Branch:
                    return true;
                case Mono.Cecil.Cil.FlowControl.Meta:
                    return false;
                case Mono.Cecil.Cil.FlowControl.Next:
                    return false;
                case Mono.Cecil.Cil.FlowControl.Phi:
                    return false;
                case Mono.Cecil.Cil.FlowControl.Return:
                    return false;
                case Mono.Cecil.Cil.FlowControl.Throw:
                    return false;
            }
            return false;
        }

        public class CFGVertex
            : GraphLinkedList<object, CFG.CFGVertex, CFG.CFGEdge>.Vertex
        {
            private Mono.Cecil.MethodDefinition _method;

            public Mono.Cecil.MethodDefinition Method
            {
                get
                {
                    return _method;
                }
                set
                {
                    _method = value;
                }
            }
            
            public List<Inst> _instructions = new List<Inst>();

            public List<Inst> Instructions
            {
                get
                {
                    return _instructions;
                }
            }

            protected int _stack_level_in;
            protected int _stack_level_out;
            protected int _stack_pre_last_instruction;

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

            public int StackLevelPreLastInstruction
            {
                get
                {
                    return _stack_pre_last_instruction;
                }
                set
                {
                    _stack_pre_last_instruction = value;
                }
            }

            protected State _state_in;
            protected State _state_out;
            protected State _state_pre_last_instruction;

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

            public State StatePreLastInstruction
            {
                get
                {
                    return _state_pre_last_instruction;
                }
                set
                {
                    _state_pre_last_instruction = value;
                }
            }


            public CFGVertex _entry;

            public bool IsEntry
            {
                get
                {
                    if (_entry == null)
                        return false;
                    if (_entry._ordered_list_of_blocks == null)
                        return false;
                    if (_entry._ordered_list_of_blocks.Count == 0)
                        return false;
                    if (_entry._ordered_list_of_blocks.First() != this)
                        return false;
                    return true;
                }
            }

            public bool IsCall
            {
                get
                {
                    Inst last = _instructions[_instructions.Count - 1];
                    switch (last.OpCode.FlowControl)
                    {
                        case Mono.Cecil.Cil.FlowControl.Call:
                            return true;
                        default:
                            return false;
                    }
                }
            }

            public bool IsNewobj
            {
                get
                {
                    Inst last = _instructions[_instructions.Count - 1];
                    return last.OpCode.Code == Mono.Cecil.Cil.Code.Newobj;
                }
            }

            public bool IsNewarr
            {
                get
                {
                    Inst last = _instructions[_instructions.Count - 1];
                    return last.OpCode.Code == Mono.Cecil.Cil.Code.Newarr;
                }
            }

            public bool IsReturn
            {
                get
                {
                    Inst last = _instructions[_instructions.Count - 1];
                    switch (last.OpCode.FlowControl)
                    {
                        case Mono.Cecil.Cil.FlowControl.Return:
                            return true;
                        default:
                            return false;
                    }
                }
            }

            protected int _number_of_locals;
            protected int _number_of_arguments;

            public int NumberOfLocals
            {
                get
                {
                    return _number_of_locals;
                }
                set
                {
                    _number_of_locals = value;
                }
            }

            public int NumberOfArguments
            {
                get
                {
                    return _number_of_arguments;
                }
                set
                {
                    _number_of_arguments = value;
                }
            }

            bool _has_return;

            public bool HasReturn
            {
                get
                {
                    return _has_return;
                }
                set
                {
                    _has_return = value;
                }
            }

            public CFGVertex Exit
            {
                get
                {
                    List<CFGVertex> list = this._entry._ordered_list_of_blocks;
                    return list[list.Count() - 1];
                }
            }

            public List<CFGVertex> _ordered_list_of_blocks;

            public CFGVertex()
                : base()
            {
            }

            public CFGVertex(CFGVertex o)
                : base(o)
            {
            }

            public CFGVertex Split(int i)
            {
                Debug.Assert(_instructions.Count != 0);
                // Split this node into two nodes, with all instructions after "i" in new node.
                CFG cfg = (CFG)this._Graph;
                CFGVertex result = (CFGVertex)cfg.AddVertex(_node_number++);
                result.Method = this.Method;
                result.HasReturn = this.HasReturn;
                result._entry = this._entry;
                this._entry._ordered_list_of_blocks.Insert(
                    this._entry._ordered_list_of_blocks.IndexOf(this) + 1,
                    result);

                for (int j = i; j < _instructions.Count; ++j)
                    result._instructions.Add(_instructions[j]);
                for (int j = _instructions.Count - 1; j >= i; --j)
                    this._instructions.RemoveAt(j);
                Debug.Assert(this._instructions.Count != 0);
                Debug.Assert(result._instructions.Count != 0);
                Inst last_instruction = this._instructions[
                    this._instructions.Count - 1];
                // Add fall-through branch.
                switch (last_instruction.OpCode.FlowControl)
                {
                    case Mono.Cecil.Cil.FlowControl.Branch:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Break:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Call:
                        cfg.AddEdge(this.Name, result.Name);
                        break;
                    case Mono.Cecil.Cil.FlowControl.Cond_Branch:
                        cfg.AddEdge(this.Name, result.Name);
                        break;
                    case Mono.Cecil.Cil.FlowControl.Meta:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Next:
                        cfg.AddEdge(this.Name, result.Name);
                        break;
                    case Mono.Cecil.Cil.FlowControl.Phi:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Return:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Throw:
                        break;
                }
                return result;
            }

        }

        public class CFGEdge
            : GraphLinkedList<object, CFG.CFGVertex, CFG.CFGEdge>.Edge
        {
        }

        public void Dump()
        {
            System.Console.WriteLine("Graph:");
            System.Console.WriteLine();
            foreach (CFGVertex n in VertexNodes)
            {
                if (n._ordered_list_of_blocks != null)
                {
                    foreach (CFGVertex v in n._ordered_list_of_blocks)
                    {
                        System.Console.WriteLine("Node: " + v.Name + " ");
                        System.Console.WriteLine("Method " + v.Method.FullName);
                        System.Console.WriteLine("Args   " + v.NumberOfArguments);
                        System.Console.WriteLine("Locals " + v.NumberOfLocals);
                        System.Console.WriteLine("Return (reuse) " + v.HasReturn);
                        System.Console.WriteLine("Level in " + v.StackLevelIn);
                        System.Console.WriteLine("Level out " + v.StackLevelOut);
                        System.Console.WriteLine("Instructions:");
                        foreach (Inst i in v._instructions)
                            System.Console.WriteLine(i);
                        System.Console.WriteLine("Edges from:");
                        foreach (object t in this.Predecessors(v.Name))
                        {
                            System.Console.WriteLine(t + " ->");
                        }
                        System.Console.WriteLine("Edges to:");
                        foreach (object t in this.Successors(v.Name))
                        {
                            System.Console.WriteLine("-> " + t);
                        }
                        if (v.StateIn != null)
                        {
                            System.Console.WriteLine("State in");
                            v.StateIn.Dump();
                        }
                        if (v.StatePreLastInstruction != null)
                        {
                            System.Console.WriteLine("State pre last inst");
                            v.StatePreLastInstruction.Dump();
                        }
                        if (v.StateOut != null)
                        {
                            System.Console.WriteLine("State out");
                            v.StateOut.Dump();
                        }
                        System.Console.WriteLine();
                    }
                }
            }
        }
    }

}
