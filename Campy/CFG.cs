using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Campy.Graphs;
using Campy.Utils;

namespace Campy
{

    public class CFG : GraphLinkedList<object, CFG.CFGVertex, CFG.CFGEdge>
    {
        static int _node_number = 1;
        GraphLinkedList<object, CFG.CFGVertex, CFG.CFGEdge> _interprocedure_graph = new GraphLinkedList<object, CFGVertex, CFGEdge>();
        List<Mono.Cecil.ModuleDefinition> _loaded_modules = new List<ModuleDefinition>();
        List<Mono.Cecil.ModuleDefinition> _analyzed_modules = new List<ModuleDefinition>();
        StackQueue<Mono.Cecil.MethodDefinition> _to_do = new StackQueue<Mono.Cecil.MethodDefinition>();
        List<Mono.Cecil.MethodDefinition> _done = new List<MethodDefinition>();
        List<CFG.CFGVertex> _entries = new List<CFGVertex>();
        CFA _cfa;
        Analysis _analysis;

        CFG(Analysis analysis)
            : base()
        {
            _cfa = new CFA(this);
            _analysis = analysis;
        }

        public List<CFG.CFGVertex> Entries
        {
            get { return _entries; }
        }

        static CFG _singleton = null;

        internal static CFG Singleton(Analysis analysis)
        {
            if (_singleton == null)
                _singleton = new CFG(analysis);
            return _singleton;
        }

        public void FindNewBlocks(Assembly assembly)
        {
            // Do not analyze Campy modules generally...
            if (Options.Singleton.Get(Options.OptionType.DoNotAnalyzeCampyAssemblies))
                return;

            // Starting from all blocks in this assembly,
            // find all PFE's, then compute control-flow/
            // data-flow analysis in order to compute call
            // structure and new blocks to analyze.

            List<Inst> pfe_list = new List<Inst>();
            List<CFG.CFGVertex> pfe_entries = new List<CFGVertex>();

            // Find all pfe's all nodes in assembly.
            foreach (CFG.CFGVertex node in this.VertexNodes)
            {
                foreach (Inst inst in node._instructions)
                {
                    Mono.Cecil.Cil.OpCode op = inst.OpCode;
                    Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                    object operand = inst.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    if (fc == Mono.Cecil.Cil.FlowControl.Call)
                    {
                        if (call_to != null && call_to.Name.Equals("For")
                            && call_to.DeclaringType != null && call_to.DeclaringType.FullName.Equals("Campy.Parallel"))
                        {
                            System.Console.WriteLine("Found PFE in block " + node.Name);
                            pfe_list.Add(inst);
                        }
                    }
                }
            }

            // Convert PFE instructions into a list of entries.
            foreach (Inst inst in pfe_list)
            {
                CFG.CFGVertex entry = FindEntry(inst);
                if (!pfe_entries.Contains(entry))
                    pfe_entries.Add(entry);
            }

            // Perform sparse data flow propagation in order to
            // get all indirect calls. Add those blocks to the graph.
            this._cfa.SparseDataFlowPropagation(pfe_entries);
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
                    if (fc == Mono.Cecil.Cil.FlowControl.Call)
                    {
                        if (call_to != null && call_to.Name.Equals("For")
                            && call_to.DeclaringType != null && call_to.DeclaringType.FullName.Equals("Campy.Parallel"))
                        {
                            Add(method);
                        }
                    }
                }
            }
        }

        // Basic API for adding classes and methods to a control flow graph for
        // analysis.

        public void Add(Type type)
        {
            // Add all methods of type.
            BindingFlags findFlags = BindingFlags.NonPublic |
                                                BindingFlags.Public |
                                                BindingFlags.Static |
                                                BindingFlags.Instance |
                                                BindingFlags.InvokeMethod |
                                                BindingFlags.OptionalParamBinding |
                                                BindingFlags.DeclaredOnly;
            foreach (System.Reflection.MethodInfo definition in type.GetMethods(findFlags))
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
            Add(definition);
        }

        public void Add(Mono.Cecil.MethodReference reference)
        {
            Add(reference.Resolve());
        }

        public void Add(Mono.Cecil.MethodDefinition definition)
        {
            // Do not analyze Campy modules generally...
            if (Options.Singleton.Get(Options.OptionType.DoNotAnalyzeCampyAssemblies))
            {
                if (definition.Module != null && Analysis.IsCampyModuleName(definition.Module.Name))
                    return;
            }

            if (_done.Contains(definition))
                return;
            if (_to_do.Contains(definition))
                return;

            bool ignore = false;

            foreach (KeyValuePair<String, String> pair in _analysis._filter)
            {
                String pat = pair.Key;
                String ty = pair.Value;

                // Match based on type "ty".
                if (ty.Equals("-method"))
                {
                    // match on name/parameters.
                    Regex reg = new Regex(@"^" + pat + @"$");
                    String def = definition.ToString();
                    Match m = reg.Match(def);
                    int ind = m.Index;
                    int l = m.Length;
                    if (!(ind >= 0 && l > 0))
                        continue;
                    ignore = true;
                    break;
                }
                else if (ty.Equals("+method"))
                {
                    // match on name/parameters.
                    Regex reg = new Regex(@"^" + pat + @"$");
                    String def = definition.ToString();
                    Match m = reg.Match(def);
                    int ind = m.Index;
                    int l = m.Length;
                    if (!(ind >= 0 && l > 0))
                        continue;
                    ignore = false;
                    break;
                }
                else if (ty.Equals("-namespace"))
                {
                    // match on name/parameters.
                    Regex reg = new Regex(@"^" + pat + @"$");
                    String def = definition.DeclaringType.Namespace.ToString();
                    Match m = reg.Match(def);
                    int ind = m.Index;
                    int l = m.Length;
                    if (!(ind >= 0 && l > 0))
                        continue;
                    ignore = true;
                    break;
                }
                else if (ty.Equals("+namespace"))
                {
                    // match on name/parameters.
                    Regex reg = new Regex(@"^" + pat + @"$");
                    String def = definition.DeclaringType.Namespace.ToString();
                    Match m = reg.Match(def);
                    int ind = m.Index;
                    int l = m.Length;
                    if (!(ind >= 0 && l > 0))
                        continue;
                    ignore = false;
                    break;
                }
                else if (ty.Equals("-assembly"))
                {
                    // match on name/parameters.
                    Regex reg = new Regex(@"^" + pat + @"$");
                    String def = definition.Module.Assembly.ToString();
                    Match m = reg.Match(def);
                    int ind = m.Index;
                    int l = m.Length;
                    if (!(ind >= 0 && l > 0))
                        continue;
                    ignore = true;
                    break;
                }
                else if (ty.Equals("+assembly"))
                {
                    // match on name/parameters.
                    Regex reg = new Regex(@"^" + pat + @"$");
                    String def = definition.Module.Assembly.ToString();
                    Match m = reg.Match(def);
                    int ind = m.Index;
                    int l = m.Length;
                    if (!(ind >= 0 && l > 0))
                        continue;
                    ignore = false;
                    break;
                }
            }

            if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                System.Console.WriteLine((ignore ? "Ignoring " : "Adding ") + definition);
            if (ignore)
                return;
            _to_do.Push(definition);
        }

        public Mono.Cecil.ModuleDefinition LoadAssembly(String assembly_file_name)
        {
            String full_name = System.IO.Path.GetFullPath(assembly_file_name);
            foreach (Mono.Cecil.ModuleDefinition md in this._loaded_modules)
                if (md.FullyQualifiedName.Equals(full_name))
                    return md;
            Mono.Cecil.ModuleDefinition module = ModuleDefinition.ReadModule(assembly_file_name);
            _loaded_modules.Add(module);
            return module;
        }

        // Add all methods of assembly to graph.
        public void AddAssembly(String assembly_file_name)
        {
            Mono.Cecil.ModuleDefinition module = LoadAssembly(assembly_file_name);
            String full_name = System.IO.Path.GetFullPath(assembly_file_name);
            foreach (Mono.Cecil.ModuleDefinition md in this._analyzed_modules)
                if (md.FullyQualifiedName.Equals(full_name))
                    return;
            _analyzed_modules.Add(module);
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

        static MethodInfo GetMethodInfo(Action a)
        {
            return a.Method;
        }
        
        public void ExtractBasicBlocks()
        {
            this.StartChangeSet(this);
            while (_to_do.Count > 0)
            {
                Mono.Cecil.MethodDefinition definition = _to_do.Pop();
                ExtractBasicBlocksOfMethod(definition);
            }
            _cfa.ConvertToSSA();
        }

        Dictionary<object, List<CFG.CFGVertex>> _change_set = new Dictionary<object, List<CFGVertex>>();

        public void StartChangeSet(object observer)
        {
            if (_change_set.ContainsKey(observer))
            {
                _change_set[observer] = new List<CFGVertex>();
            }
            else
            {
                _change_set.Add(observer, new List<CFGVertex>());
            }
        }

        public List<CFG.CFGVertex> EndChangeSet(object observer)
        {
            if (_change_set.ContainsKey(observer))
            {
                List<CFG.CFGVertex> list = _change_set[observer];
                _change_set.Remove(observer);
                return list;
            }
            else
                return null;
        }

        public override GraphLinkedList<object,CFG.CFGVertex,CFG.CFGEdge>.Vertex AddVertex(object v)
        {
            foreach (CFG.CFGVertex vertex in this.VertexNodes)
            {
                if (vertex.Name == v)
                    return vertex;
            }
            if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                System.Console.WriteLine("adding vertex " + v);
            CFG.CFGVertex x = (CFGVertex)base.AddVertex(v);
            foreach (KeyValuePair<object, List<CFG.CFGVertex>> pair in this._change_set)
            {
                pair.Value.Add(x);
                Debug.Assert(_change_set[pair.Key].Contains(x));
            }
            return x;
        }

        internal Dictionary<Mono.Cecil.Cil.Instruction, CFG.CFGVertex> partition_of_instructions = new Dictionary<Mono.Cecil.Cil.Instruction, CFGVertex>();

        private void ExtractBasicBlocksOfMethod(MethodDefinition definition)
        {
            _done.Add(definition);
            // Make sure definition assembly is loaded.
            String full_name = definition.Module.FullyQualifiedName;
            LoadAssembly(full_name);
            if (definition.Body == null)
            {
                System.Console.WriteLine("WARNING: METHOD BODY NULL! " + definition);
                return;
            }
            int instruction_count = definition.Body.Instructions.Count;
            StackQueue<Mono.Cecil.Cil.Instruction> leader_list = new StackQueue<Mono.Cecil.Cil.Instruction>();

            // Each method is a leader of a block.
            CFGVertex v = (CFGVertex)this.AddVertex(_node_number++);
            v.Method = definition;
            v.HasReturnValue = definition.IsReuseSlot;
            v._entry = v;
            this._entries.Add(v);
            v._ordered_list_of_blocks = new List<CFGVertex>();
            v._ordered_list_of_blocks.Add(v);
            for (int j = 0; j < instruction_count; ++j)
            {
                // accumulate jump to locations since these split blocks.
                Mono.Cecil.Cil.Instruction mi = definition.Body.Instructions[j];
                //System.Console.WriteLine(mi);
                Inst i = Inst.Wrap(mi, this);
                Mono.Cecil.Cil.OpCode op = i.OpCode;
                Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                
                v._instructions.Add(i);

                // Verify that mi not owned already.
                CFG.CFGVertex asdfasdf;
                Debug.Assert(! partition_of_instructions.TryGetValue(mi, out asdfasdf));
                // Update ownership.
                partition_of_instructions.Add(mi, v);

                if (fc == Mono.Cecil.Cil.FlowControl.Next)
                    continue;
                if (fc == Mono.Cecil.Cil.FlowControl.Branch
                    || fc == Mono.Cecil.Cil.FlowControl.Cond_Branch)
                {
                    // Save leader target of branch.
                    object o = i.Operand;
                    // Two cases that I know of: operand is just and instruction,
                    // or operand is an array of instructions (via a switch instruction).

                    Mono.Cecil.Cil.Instruction oo = o as Mono.Cecil.Cil.Instruction;
                    Mono.Cecil.Cil.Instruction[] ooa = o as Mono.Cecil.Cil.Instruction[];
                    if (oo != null)
                    {
                        leader_list.Push(oo);
                    }
                    else if (ooa != null)
                    {
                        foreach (Mono.Cecil.Cil.Instruction ins in ooa)
                        {
                            Debug.Assert(ins != null);
                            leader_list.Push(ins);
                        }
                    }
                    else
                        throw new Exception("Unknown operand type for basic block partitioning.");

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
                //System.Console.WriteLine("Looking for " + i);
                if (leader_list.Contains(i))
                    ordered_leader_list.Push(j);
            }
            // Split block at jump targets in reverse.
            while (ordered_leader_list.Count > 0)
            {
                int i = ordered_leader_list.Pop();
                CFG.CFGVertex new_node = v.Split(i);
            }

            //this.Dump();

            StackQueue<CFG.CFGVertex> stack = new StackQueue<CFG.CFGVertex>();
            foreach (CFG.CFGVertex node in this.VertexNodes) stack.Push(node);
            while (stack.Count > 0)
            {
                // Split blocks at branches, not including calls, with following
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
                    if (fc == Mono.Cecil.Cil.FlowControl.Call)
                        continue;
                    if (fc == Mono.Cecil.Cil.FlowControl.Meta)
                        continue;
                    if (fc == Mono.Cecil.Cil.FlowControl.Phi)
                        continue;
                    if (j + 1 >= node_instruction_count)
                        continue;
                    CFG.CFGVertex new_node = node.Split(j + 1);
                    stack.Push(new_node);
                    break;
                }
            }

            //this.Dump();
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
                            // Two cases: i.Operand is a single instruction, or an array of instructions.
                            if (i.Operand as Mono.Cecil.Cil.Instruction != null)
                            {
                                Mono.Cecil.Cil.Instruction target_instruction = i.Operand as Mono.Cecil.Cil.Instruction;
                                CFGVertex target_node = this.VertexNodes.First(
                                    (CFGVertex x) =>
                                    {
                                        if (!x._instructions.First().Instruction.Equals(target_instruction))
                                            return false;
                                        return true;
                                    });
                                if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                    System.Console.WriteLine("Create edge a " + node.Name + " to " + target_node.Name);
                                this.AddEdge(node, target_node);
                            }
                            else if (i.Operand as Mono.Cecil.Cil.Instruction[] != null)
                            {
                                foreach (Mono.Cecil.Cil.Instruction target_instruction in (i.Operand as Mono.Cecil.Cil.Instruction[]))
                                {
                                    CFGVertex target_node = this.VertexNodes.First(
                                        (CFGVertex x) =>
                                        {
                                            if (!x._instructions.First().Instruction.Equals(target_instruction))
                                                return false;
                                            return true;
                                        });
                                    System.Console.WriteLine("Create edge a " + node.Name + " to " + target_node.Name);
                                    this.AddEdge(node, target_node);
                                }
                            }
                            else
                                throw new Exception("Unknown operand type for conditional branch.");
                            break;
                        }
                    case Mono.Cecil.Cil.FlowControl.Break:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Call:
                        {
                            // We no longer split at calls. Splitting causes
                            // problems because interprocedural edges are
                            // produced. That's not good because it makes
                            // code too "messy".
                            break;

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
                                            this._interprocedure_graph.AddEdge(exit_node, e.to);
                                        }
                                        // Create edge from node to method entry.
                                        System.Console.WriteLine("Create edge b " + node.Name + " to " + target_node.Name);
                                        this._interprocedure_graph.AddEdge(node, target_node);
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

            //this.Dump();
        }

        /// <summary>
        /// Return block corresponding to the instruction.
        /// </summary>
        /// <param name="inst"></param>
        /// <returns></returns>
        public CFGVertex FindEntry(Inst inst)
        {
            CFGVertex result = null;

            // Find owning block.
            result = inst.Block;

            // Return entry block for method.
            return result._entry;
        }

        public CFGVertex FindEntry(Mono.Cecil.Cil.Instruction inst)
        {
            CFGVertex result = null;
            foreach (CFG.CFGVertex node in this.VertexNodes)
                if (node._instructions.First().Instruction == inst)
                    return node;
            return result;
        }

        public CFGVertex FindEntry(Mono.Cecil.MethodReference mr)
        {
            foreach (CFG.CFGVertex node in this.VertexNodes)
                if (node.Method == mr)
                    return node;
            return null;
        }

        static int kens_id = 1;

        public class CFGVertex
            : GraphLinkedList<object, CFG.CFGVertex, CFG.CFGEdge>.Vertex
        {
            private int _kens_id = kens_id++;

            public int ID
            {
                get { return _kens_id; }
            }

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

            protected int? _stack_level_in;
            protected int? _stack_level_out;
            protected int _stack_pre_last_instruction;

            public int? StackLevelIn
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

            public int? StackLevelOut
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

            public bool HasReturnValue
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
                StackLevelOut = null;
                StackLevelIn = null;
            }

            public CFGVertex(CFGVertex o)
                : base(o)
            {
                StackLevelOut = null;
                StackLevelIn = null;
            }

            public void OutputEntireNode()
            {
                CFG.CFGVertex v = this;
                System.Console.WriteLine();
                System.Console.WriteLine("Node: " + v.Name + " ");
                System.Console.WriteLine(new String(' ', 4) + "Method " + v.Method.FullName);
                System.Console.WriteLine(new String(' ', 4) + "Args   " + v.NumberOfArguments);
                System.Console.WriteLine(new String(' ', 4) + "Locals " + v.NumberOfLocals);
                System.Console.WriteLine(new String(' ', 4) + "Return (reuse) " + v.HasReturnValue);
                System.Console.WriteLine(new String(' ', 4) + "Stack level in " + v.StackLevelIn);
                System.Console.WriteLine(new String(' ', 4) + "Stack level out " + v.StackLevelOut);
                if (this._Graph.Predecessors(v.Name).Any())
                {
                    System.Console.Write(new String(' ', 4) + "Edges from:");
                    foreach (object t in this._Graph.Predecessors(v.Name))
                    {
                        System.Console.Write(" " + t);
                    }
                    System.Console.WriteLine();
                }
                if (this._Graph.Successors(v.Name).Any())
                {
                    System.Console.Write(new String(' ', 4) + "Edges to:");
                    foreach (object t in this._Graph.Successors(v.Name))
                    {
                        System.Console.Write(" " + t);
                    }
                    System.Console.WriteLine();
                }
                System.Console.WriteLine(new String(' ', 4) + "Instructions:");
                SSA ssa = SSA.Singleton();
                //if (v.StateIn != null)
                //{
                //    System.Console.WriteLine("State in");
                //    v.StateIn.Dump();
                //}
                foreach (Inst i in v._instructions)
                {
                    //if (i.StateIn != null)
                    //    i.StateIn.Dump();

                    System.Console.Write(new String(' ', 8) + i + new String(' ', 4));

                    if (ssa._operation.ContainsKey(i))
                    {
                        foreach (SSA.Operation o in ssa._operation[i])
                        {
                            System.Console.Write(" [" + o + "]");
                        }
                    }

                    System.Console.WriteLine();
                }

                //if (v.StateOut != null)
                //{
                //    System.Console.WriteLine("State out");
                //    v.StateOut.Dump();
                //}
                System.Console.WriteLine();
            }

            public CFGVertex Split(int i)
            {
                if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                    System.Console.WriteLine("Split at " + i + " " + _instructions[i]);
                Debug.Assert(_instructions.Count != 0);
                // Split this node into two nodes, with all instructions after "i" in new node.
                CFG cfg = (CFG)this._Graph;
                CFGVertex result = (CFGVertex)cfg.AddVertex(_node_number++);
                result.Method = this.Method;
                result.HasReturnValue = this.HasReturnValue;
                result._entry = this._entry;

                // Insert new block after this block.
                this._entry._ordered_list_of_blocks.Insert(
                    this._entry._ordered_list_of_blocks.IndexOf(this) + 1,
                    result);

                int count = _instructions.Count;

                // Add instructions from split point to new block.
                for (int j = i; j < count; ++j)
                {
                    result._instructions.Add(_instructions[j]);

                    // Verify instruction ownership.
                    Debug.Assert(cfg.partition_of_instructions[_instructions[j].Instruction] == this);
                    // Update ownership.
                    cfg.partition_of_instructions.Remove(_instructions[j].Instruction);
                    cfg.partition_of_instructions.Add(_instructions[j].Instruction, result);
                }

                // Remove instructions from previous block.
                for (int j = i; j < count; ++j)
                {
                    this._instructions.RemoveAt(i);
                }

                Debug.Assert(this._instructions.Count != 0);
                Debug.Assert(result._instructions.Count != 0);
                Debug.Assert(this._instructions.Count + result._instructions.Count == count);

                Inst last_instruction = this._instructions[
                    this._instructions.Count - 1];
 
                // Transfer any out edges to pred block to new block.
                while (cfg.SuccessorNodes(this).Count() > 0)
                {
                    CFG.CFGVertex succ = cfg.SuccessorNodes(this).First();
                    cfg.DeleteEdge(this, succ);
                    cfg.AddEdge(result, succ);
                }

                // Add fall-through branch from pred to succ block.
                switch (last_instruction.OpCode.FlowControl)
                {
                    case Mono.Cecil.Cil.FlowControl.Branch:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Break:
                        break;
                    case Mono.Cecil.Cil.FlowControl.Call:
                        cfg._interprocedure_graph.AddEdge(this.Name, result.Name);
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

                //System.Console.WriteLine("After split");
                //cfg.Dump();
                //System.Console.WriteLine("-----------");
                return result;
            }

        }

        public class CFGEdge
            : GraphLinkedList<object, CFG.CFGVertex, CFG.CFGEdge>.Edge
        {
            public bool IsInterprocedural()
            {
                CFG.CFGVertex f = (CFG.CFGVertex)this.from;
                CFG.CFGVertex t = (CFG.CFGVertex)this.to;
                if (f.Method != t.Method)
                    return true;
                return false;
            }
        }

        public void OutputEntireGraph()
        {
            System.Console.WriteLine("Graph:");
            System.Console.WriteLine();
            System.Console.WriteLine("List of entries blocks:");
            System.Console.WriteLine(new String(' ', 4) + "Node" + new string(' ', 4) + "Method");
            foreach (CFGVertex n in this._entries)
            {
                System.Console.Write("{0,8}", n);
                System.Console.Write(new string(' ', 4));
                System.Console.WriteLine(n.Method.FullName);
            }
            System.Console.WriteLine();
            System.Console.WriteLine("List of callers:");
            System.Console.WriteLine(new String(' ', 4) + "Node" + new string(' ', 4) + "Instruction");
            foreach (Inst caller in Inst.CallInstructions)
            {
                CFGVertex n = caller.Block;
                System.Console.Write("{0,8}", n);
                System.Console.Write(new string(' ', 4));
                System.Console.WriteLine(caller);
            }
            if (this._entries.Any())
            {
                System.Console.WriteLine();
                System.Console.WriteLine("List of orphan blocks:");
                System.Console.WriteLine(new String(' ', 4) + "Node" + new string(' ', 4) + "Method");
                foreach (CFGVertex n in this._entries)
                {
                    if (n.StateIn != null &&
                        n.StateIn._bindings != null &&
                        n.StateIn._bindings.Count == 1)
                    {
                        System.Console.Write("{0,8}", n);
                        System.Console.Write(new string(' ', 4));
                        if (n.StateIn._bindings.First()._caller != null)
                            System.Console.WriteLine("hmm?? ");
                        System.Console.WriteLine(n.Method.FullName);
                    }
                }
                System.Console.WriteLine();
            }

            foreach (CFGVertex n in VertexNodes)
            {
                if (n._ordered_list_of_blocks != null)
                {
                    foreach (CFGVertex v in n._ordered_list_of_blocks)
                    {
                        v.OutputEntireNode();
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
                foreach (CFG.CFGVertex current in _node._ordered_list_of_blocks)
                {
                    foreach (CFG.CFGVertex next in _node._Graph.SuccessorNodes(current))
                    {
                        if (next.IsEntry && next.Method != _node.Method)
                            yield return next;
                    }
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
