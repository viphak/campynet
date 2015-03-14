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
        static int _node_number = 1;
        List<Mono.Cecil.ModuleDefinition> _modules = new List<ModuleDefinition>();
        StackQueue<Mono.Cecil.MethodDefinition> _to_do = new StackQueue<Mono.Cecil.MethodDefinition>();
        List<Mono.Cecil.MethodDefinition> _done = new List<MethodDefinition>();
        CFA _cfa;

        public CFG()
            : base()
        {
            _cfa = new CFA(this);
        }

        static CFG _singleton = null;

        public static CFG Singleton()
        {
            if (_singleton == null)
                _singleton = new CFG();
            return _singleton;
        }

        public void FindNewBlocks()
        {
            foreach (CFG.CFGVertex node in this.VertexNodes)
            {
                foreach (Inst i in node.Instructions)
                {
                    Mono.Cecil.Cil.OpCode op = i.OpCode;
                    Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                    if (!(fc == Mono.Cecil.Cil.FlowControl.Call))
                        continue;
                    object operand = i.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    // Search for _Kernel_type.
                    if (call_to.FullName.Equals("System.Void Campy.AMP/_Kernel_type::.ctor(System.Object,System.IntPtr)"))
                    {
                        Add(call_to);
                    }
                    // Check if Campy based API call.
                    if (call_to.FullName.Contains(" Campy."))
                        continue;

                    Add(call_to);
                }
            }
        }

        public void HeuristicAdd(Mono.Cecil.ModuleDefinition module)
        {
            // Search module for PFEs and add enclosing method.
            // Find all pfe's in graph.
            foreach (Mono.Cecil.MethodDefinition method in Campy.Types.Utils.ReflectionCecilInterop.GetMethods(module))
            {
                if (method.Body == null)
                    return;
                foreach (Mono.Cecil.Cil.Instruction i in method.Body.Instructions)
                {
                    Mono.Cecil.Cil.OpCode op = i.OpCode;
                    Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                    object operand = i.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    if (fc == Mono.Cecil.Cil.FlowControl.Call && call_to.Name.Equals("Parallel_For_Each"))
                    {
                        Add(method);
                    }
                }
            }
        }

        // Basic API for adding classes and methods to a control flow graph for
        // analysis.

        public void Add(Type type)
        {
            // Add all methods of type.
            foreach (System.Reflection.MethodInfo definition in type.GetMethods())
                Add(definition);
        }

        public void Add(Mono.Cecil.TypeReference type)
        {
            // Add all methods of type.
            Mono.Cecil.TypeDefinition type_defintion = type.Resolve();
            foreach (Mono.Cecil.MethodDefinition definition in type_defintion.Methods)
                Add(definition);
        }

        public void Add(System.Reflection.MethodInfo reference)
        {
            Mono.Cecil.MethodDefinition definition = Campy.Types.Utils.ReflectionCecilInterop.ConvertToMonoCecilMethodDefinition(reference);
            if (_done.Contains(definition))
                return;
            System.Console.WriteLine("Adding method " + definition);
            _to_do.Push(definition);
        }

        public void Add(Mono.Cecil.MethodReference reference)
        {
            Add(reference.Resolve());
        }

        public void Add(Mono.Cecil.MethodDefinition definition)
        {
            if (_done.Contains(definition))
                return;
            System.Console.WriteLine("Adding method " + definition);
            _to_do.Push(definition);
        }

        // Add all methods of assembly to graph.
        public void AddAssembly(String assembly_file_name)
        {
            String full_name = System.IO.Path.GetFullPath(assembly_file_name);
            foreach (Mono.Cecil.ModuleDefinition md in this._modules)
                if (md.FullyQualifiedName.Equals(full_name))
                    return;
            Mono.Cecil.ModuleDefinition module = ModuleDefinition.ReadModule(assembly_file_name);
            _modules.Add(module);
            StackQueue<Mono.Cecil.TypeDefinition> type_definitions = new StackQueue<Mono.Cecil.TypeDefinition>();
            StackQueue<Mono.Cecil.TypeDefinition> type_definitions_closure = new StackQueue<Mono.Cecil.TypeDefinition>();
            foreach (Mono.Cecil.TypeDefinition td in module.Types)
            {
                type_definitions.Push(td);
            }
            while (type_definitions.Count > 0)
            {
                Mono.Cecil.TypeDefinition ty = type_definitions.Pop();
                type_definitions_closure.Push(ty);
                foreach (Mono.Cecil.TypeDefinition ntd in ty.NestedTypes)
                    type_definitions.Push(ntd);
            }
            foreach (Mono.Cecil.TypeDefinition td in type_definitions_closure)
                foreach (Mono.Cecil.MethodDefinition definition in td.Methods)
                    Add(definition);
        }

        public void AddAssembly(Assembly assembly)
        {
            String assembly_file_name = assembly.Location;
            AddAssembly(assembly_file_name);
        }

        public void ExtractBasicBlocks()
        {
            while (_to_do.Count > 0)
            {
                while (_to_do.Count > 0)
                {
                    Mono.Cecil.MethodDefinition definition = _to_do.Pop();
                    ProcessMethod(definition);
                }
                _cfa.AnalyzeCFG();
                FindNewBlocks();
            }
        }

        private void ProcessMethod(MethodDefinition definition)
        {
            _done.Add(definition);
            if (definition.Body == null)
                return;
            int instruction_count = definition.Body.Instructions.Count;
            StackQueue<Mono.Cecil.Cil.Instruction> leader_list = new StackQueue<Mono.Cecil.Cil.Instruction>();

            // Each method is a leader of a block.
            CFGVertex v = (CFGVertex)this.AddVertex(_node_number++);
            v.Method = definition;
            v.HasReturn = definition.IsReuseSlot;
            v._entry = v;
            v._ordered_list_of_blocks = new List<CFGVertex>();
            v._ordered_list_of_blocks.Add(v);
            for (int j = 0; j < instruction_count; ++j)
            {
                // accumulate jump to locations since these split blocks.
                Mono.Cecil.Cil.Instruction mi = definition.Body.Instructions[j];
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
            for (int j = 0; j < instruction_count; ++j)
            {
                // Order jump targets. These denote locations
                // where to split blocks. However, it's ordered,
                // so that splitting is done from last instruction in block
                // to first instruction in block.
                Mono.Cecil.Cil.Instruction i = definition.Body.Instructions[j];
                if (leader_list.Contains(i))
                    ordered_leader_list.Push(j);
            }
            // Split block at jump targets in reverse.
            while (ordered_leader_list.Count > 0)
            {
                int i = ordered_leader_list.Pop();
                CFG.CFGVertex new_node = v.Split(i);
            }

           // this.Dump();

            StackQueue<CFG.CFGVertex> stack = new StackQueue<CFG.CFGVertex>();
            foreach (CFG.CFGVertex node in this.VertexNodes) stack.Push(node);
            while (stack.Count > 0)
            {
                // Split blocks at branches, including calls, with following
                // instruction a leader of new block.
                CFG.CFGVertex node = stack.Pop();
                int node_instruction_count = node._instructions.Count;
                for (int j = 0; j < node_instruction_count; ++j)
                {
                    Inst i = node._instructions[j];
                    Mono.Cecil.Cil.OpCode op = i.OpCode;
                    Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                    if (fc == Mono.Cecil.Cil.FlowControl.Next)
                        continue;
                    if (j + 1 >= node_instruction_count)
                        continue;
                    CFG.CFGVertex new_node = node.Split(j + 1);
                    stack.Push(new_node);
                    break;
                }
            }

         //   this.Dump();
            stack = new StackQueue<CFG.CFGVertex>();
            foreach (CFG.CFGVertex node in this.VertexNodes) stack.Push(node);
            while (stack.Count > 0)
            {
                // Add in all final non-fallthrough branch edges.
                CFG.CFGVertex node = stack.Pop();
                int node_instruction_count = node._instructions.Count;
                Inst i = node._instructions[node_instruction_count - 1];
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
                            //System.Console.WriteLine("Create edge a " + node.Name + " to " + target_node.Name);
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
                                Mono.Cecil.MethodDefinition d = r.Resolve();
                                IEnumerable<CFGVertex> target_node_list = this.VertexNodes.Where(
                                    (CFGVertex x) =>
                                    {
                                        return x.Method.FullName == r.FullName
                                            && x._entry == x;
                                    });
                                int c = target_node_list.Count();
                                if (c >= 1)
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

     //       this.Dump();
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

        class CallEnumerator : IEnumerable<CFG.CFGVertex>
        {
            CFG.CFGVertex _node;

            public CallEnumerator(CFG.CFGVertex node)
            {
                _node = node;
            }

            public IEnumerator<CFG.CFGVertex> GetEnumerator()
            {
                StackQueue<CFG.CFGVertex> stack = new StackQueue<CFG.CFGVertex>();
                stack.Push(_node);
                while (stack.Count > 0)
                {
                    CFG.CFGVertex current = stack.Pop();
                    if (current.IsEntry && current != _node)
                        yield return current;
                    if (current.Method == _node.Method)
                        foreach (CFG.CFGVertex child in current._Graph.SuccessorNodes(current))
                            stack.Push(child);
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEnumerable<CFG.CFGVertex> AllInterproceduralCalls(CFG.CFGVertex node)
        {
            return new CallEnumerator(node);
        }

    }

}
