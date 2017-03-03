using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Campy.Utils;
using Campy.Types;

namespace Campy
{
    public class PFE_Point
    {
        public CFG.CFGVertex _parallel_for_each_block;
        public List<StackQueue<CFG.CFGVertex>> _chains_from_pfe;
        public List<StackQueue<CFG.CFGVertex>> _chains_to_pfe;
    }

    class CFA
    {
        CFG _cfg;
        List<PFE_Point> _parallel_for_each_list;

        public CFA(CFG cfg)
        {
            _cfg = cfg;
            _parallel_for_each_list = new List<PFE_Point>();
        }

        static int error_count = 0;
        public List<CFG.CFGVertex> entries = new List<CFG.CFGVertex>();

        public void ConvertToSSA()
        {
            SSA ssa = SSA.Singleton();

            //******************************************************
            //
            // STEP 1.
            //
            // Create a list of entries.
            // 
            //      Go through all vertices, determine if it's a
            //      an entry node (i.e., the beginning of the method),
            //      and add it to a list.
            //
            //******************************************************
            foreach (CFG.CFGVertex node in _cfg.VertexNodes)
                if (node.IsEntry && ! entries.Contains(node))
                    entries.Add(node);

            //******************************************************
            //
            // STEP 2.
            //
            // Create a list of all new/changed nodes of the graph.
            //
            //      Query the graph for the list of nodes that have
            //      changed. The change set for the graph is reset
            //      with this call.
            //
            //******************************************************
            List<CFG.CFGVertex> change_set = _cfg.EndChangeSet(_cfg);
            //System.Console.WriteLine("Change set:");
            //foreach (CFG.CFGVertex xxxxxx in change_set)
            //{
            //    System.Console.WriteLine(xxxxxx);
            //}
            //System.Console.WriteLine();

            //******************************************************
            //
            // STEP 3.
            //
            // Set the number of arguments, locals, and the return
            // value count for each of the nodes in the changed set.
            //
            //      Examine the node method (Mono) and using properties
            //      from Mono for the method, compute the attributes
            //      for the nodes.
            //
            //******************************************************
            foreach (CFG.CFGVertex node in change_set)
            {
                int args = 0;
                Mono.Cecil.MethodDefinition md = node.Method;
                Mono.Cecil.MethodReference mr = node.Method;
                if (mr.HasThis) args++;
                args += mr.Parameters.Count;
                node.NumberOfArguments = args;
                int locals = md.Body.Variables.Count;
                node.NumberOfLocals = locals;
                int ret = 0;
                if (mr.MethodReturnType != null)
                {
                    Mono.Cecil.MethodReturnType rt = mr.MethodReturnType;
                    Mono.Cecil.TypeReference tr = rt.ReturnType;
                    // Get type, may contain modifiers.
                    // Note, the return type must be examined in order
                    // to really determine if it returns a value--"void"
                    // means that it doesn't return a value.
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
                node.HasReturnValue = ret > 0;
            }

            //******************************************************
            //
            // STEP 4.
            //
            // Compute list of unreachable nodes. These will be removed
            // from further consideration.
            //
            //******************************************************
            List<CFG.CFGVertex> unreachable = new List<CFG.CFGVertex>();
            {
                // Create DFT order of all nodes.
                IEnumerable<object> objs = entries.Select(x => x.Name);
                if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                    System.Console.WriteLine("Entries " +
                        objs.Aggregate(new StringBuilder(),
                            (sb, v) =>
                                sb.Append(v).Append(", "),
                            sb =>
                                {
                                    if (0 < sb.Length)
                                        sb.Length -= 2;
                                    return sb.ToString();
                                }));
                GraphAlgorithms.DepthFirstPreorderTraversal<object>
                    dfs = new GraphAlgorithms.DepthFirstPreorderTraversal<object>(
                        _cfg,
                        objs
                        );
                List<CFG.CFGVertex> visited = new List<CFG.CFGVertex>();
                foreach (object ob in dfs)
                {
                    CFG.CFGVertex node = _cfg.VertexSpace[_cfg.NameSpace.BijectFromBasetype(ob)];
                    if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                        System.Console.WriteLine("Visiting " + node);
                    visited.Add(node);
                }
                foreach (CFG.CFGVertex v in change_set)
                {
                    if (!visited.Contains(v))
                        unreachable.Add(v);
                }
            }


            //******************************************************
            //
            // STEP 5.
            //
            // Compute list of change set minus unreachable nodes.
            // Most of these nodes are "catch" or "finally" blocks,
            // which we aren't supporting for this CFA.
            //
            //******************************************************
            List<CFG.CFGVertex> change_set_minus_unreachable = new List<CFG.CFGVertex>(change_set);
            foreach (CFG.CFGVertex v in unreachable)
            {
                if (change_set_minus_unreachable.Contains(v))
                {
                    if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                        System.Console.WriteLine("NODE " + v + " IS UNREACHABLE.");
                    change_set_minus_unreachable.Remove(v);
                }
            }

            //******************************************************
            //
            // STEP 6.
            //
            // Compute stack sizes for change set.
            //
            //******************************************************
            {
                List<CFG.CFGVertex> work = new List<CFG.CFGVertex>(change_set_minus_unreachable);
                while (work.Count != 0)
                {
                    // Create DFT order of all nodes.
                    IEnumerable<object> objs = entries.Select(x => x.Name);
                    GraphAlgorithms.DepthFirstPreorderTraversal<object>
                        dfs = new GraphAlgorithms.DepthFirstPreorderTraversal<object>(
                            _cfg,
                            objs
                            );

                    List<CFG.CFGVertex> visited = new List<CFG.CFGVertex>();
                    // Compute stack size for each basic block, processing nodes on work list
                    // in DFT order.
                    foreach (object ob in dfs)
                    {
                        CFG.CFGVertex node = _cfg.VertexSpace[_cfg.NameSpace.BijectFromBasetype(ob)];
                        visited.Add(node);
                        if (!(work.Contains(node)))
                        {
                            continue;
                        }
                        work.Remove(node);

                        // Use predecessor information to get initial stack size.
                        if (node.IsEntry)
                        {
                            node.StackLevelIn = node.NumberOfLocals + node.NumberOfArguments;
                            if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                System.Console.WriteLine("No predecessors for node " + node + ". Setting stack size to args+locals = " + node.StackLevelIn);
                        }
                        else
                        {
                            int in_level = -1;
                            foreach (CFG.CFGVertex pred in _cfg.PredecessorNodes(node))
                            {
                                // Do not consider interprocedural edges when computing stack size.
                                if (pred.Method != node.Method)
                                    continue;
                                // If predecessor has not been visited, warn and do not consider.
                                if (pred.StackLevelOut == null)
                                {
                                    if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                        System.Console.WriteLine("Warning: Predecessor " + pred
                                            + ", of node " + node
                                            + ", has not been visited.");
                                    continue;
                                }
                                // Warn if predecessor does not concur with another predecessor.
                                if (in_level != -1 && in_level != pred.StackLevelOut)
                                {
                                    if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                        System.Console.WriteLine("Warning: Predecessor " + pred
                                            + ", of node " + node
                                            + ", has a stack level different from another predecessor.");
                                }
                                if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                    System.Console.WriteLine("Node " + node + " level in set to " + pred.StackLevelOut + " via pred " + pred);
                                node.StackLevelIn = pred.StackLevelOut;
                                in_level = (int)node.StackLevelIn;
                            }
                            // Warn if no predecessors have been visited.
                            if (in_level == -1)
                            {
                                if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                    System.Console.WriteLine("Node " + node
                                        + " has no predecessors visited. Cannot process.");
                                continue;
                            }
                        }

                        if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                        {
                            System.Console.WriteLine();
                            System.Console.WriteLine("Processing block " + node);
                            System.Console.WriteLine("Args " + node.NumberOfArguments + " locs = " + node.NumberOfLocals);
                            System.Console.WriteLine("Level in = " + node.StackLevelIn + " level out = " +
                                                     node.StackLevelOut);
                        }
                        int level_after = (int)node.StackLevelIn;
                        int level_pre = level_after;
                        foreach (Inst i in node._instructions)
                        {
                            level_pre = level_after;
                            i.ComputeStackLevel(ref level_after);
                            //System.Console.WriteLine("after inst " + i);
                            //System.Console.WriteLine("level = " + level_after);
                            Debug.Assert(level_after >= node.NumberOfLocals + node.NumberOfArguments);
                        }
                        node.StackLevelOut = level_after;
                        // Verify return node that it makes sense.
                        if (node.IsReturn && !unreachable.Contains(node))
                        {
                            if (node.StackLevelOut ==
                                node.NumberOfArguments +
                                node.NumberOfLocals +
                                (node.HasReturnValue ? 1 : 0))
                                ;
                            else
                            {
                                if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                {
                                    System.Console.WriteLine();
                                    System.Console.WriteLine("Failed stack level out check for block " + node);
                                    _cfg.OutputEntireGraph();
                                }
                                throw new Exception("Failed stack level out check");
                            }
                        }
                        node.StackLevelPreLastInstruction = level_pre;
                        foreach (CFG.CFGVertex succ in node._Graph.SuccessorNodes(node))
                        {
                            // If it's an interprocedural edge, nothing to pass on.
                            if (succ.Method != node.Method)
                                continue;
                            // If it's recursive, nothing more to do.
                            if (succ.IsEntry)
                                continue;
                            // If it's a return, nothing more to do also.
                            if (node._instructions.Last() as i_ret != null)
                                continue;
                            // Nothing to update if no change.
                            if (succ.StackLevelIn > level_after)
                            {
                                if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                    System.Console.WriteLine("WARNING: level decrease " + succ);
                                continue;
                            }
                            else if (succ.StackLevelIn == level_after)
                            {
                                continue;
                            }
                            if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                System.Console.WriteLine("Update successor " + succ);
                            if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                System.Console.WriteLine("level in = " + level_after + " level out (pre) " + node.StackLevelOut);
                            if (!work.Contains(succ))
                            {
                                work.Add(succ);
                            }
                        }
                        if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                        {
                            System.Console.WriteLine();
                            foreach (CFG.CFGVertex xx in _cfg.VertexNodes)
                            {
                                System.Console.WriteLine("Node " + xx + " level in " + xx.StackLevelIn + " level out " +
                                                         xx.StackLevelOut);
                            }
                            System.Console.WriteLine();
                        }
                    }
                }
            }

            //******************************************************
            //
            // STEP 7.
            //
            // Convert change set to SSA representation.
            // Each node is converted by itself, without any predecessor
            // information. To do that, each stack must have unique variables.
            //
            //******************************************************
            {
                List<CFG.CFGVertex> work = new List<CFG.CFGVertex>(change_set_minus_unreachable);
                StackQueue<CFG.CFGVertex> worklist = new StackQueue<CFG.CFGVertex>();
                while (work.Count != 0)
                {
                    // Create DFT order of all nodes.
                    IEnumerable<object> objs = entries.Select(x => x.Name);
                    GraphAlgorithms.DepthFirstPreorderTraversal<object>
                        dfs = new GraphAlgorithms.DepthFirstPreorderTraversal<object>(
                            _cfg,
                            objs
                            );

                    List<CFG.CFGVertex> visited = new List<CFG.CFGVertex>();
                    foreach (object ob in dfs)
                    {
                        CFG.CFGVertex node = _cfg.VertexSpace[_cfg.NameSpace.BijectFromBasetype(ob)];
                        visited.Add(node);
                        if (!(work.Contains(node)))
                        {
                            continue;
                        }
                        work.Remove(node);

                        // Check if stack levels computed.
                        if (node.StackLevelIn == null)
                        {
                            if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                System.Console.WriteLine("Node " + node
                                    + " is on queue, but hasn't been processed. Skipping.");
                            continue;
                        }

                        // Note, a new state creates a set of phi
                        // functions to handle edges from all predecessors.
                        // After computing SSA for all predecessors,
                        // we update the phi functions.
                        // However, later on, the defined set of variables
                        // will be required, which is contained in the state.
                        // So, order of processing
                        // nodes for SSA is relevant.

                        int level_in = (int)node.StackLevelIn;
                        // Note, this creates a state with stack of level_in
                        // with all new variables contained in the stack.
                        node.StateIn = new State(node.Method, level_in);
                        State state_after = new State(node.StateIn);
                        State state_pre = new State(state_after);
                        if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                            System.Console.WriteLine();
                        if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                            System.Console.WriteLine("Node " + node + " in state");
                        int level_after = level_in;
                        int level_pre = level_after;
                        foreach (Inst i in node._instructions)
                        {
                            state_pre = new State(state_after);
                            if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                state_pre.Dump();
                            level_pre = level_after;
                            i.StateIn = new State(state_pre);
                            if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                System.Console.WriteLine(i);
                            i.ComputeStackLevel(ref level_after);
                            i.ComputeSSA(ref state_after);
                            Debug.Assert(level_after == state_after._stack.Size());
                            // Save before and after state for call instructions.
                            // This will come in handy for alias analysis.
                            //if (i.Instruction.OpCode.FlowControl == Mono.Cecil.Cil.FlowControl.Call)
                                i.StateOut = new State(state_after);
                        }
                        if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                            state_after.Dump();
                        node.StateOut = state_after;
                    }
                }
            }

            //******************************************************
            //
            // STEP 8.
            //
            // Set up phi functions for change set.
            //
            //******************************************************
            {
                List<CFG.CFGVertex> work = new List<CFG.CFGVertex>(change_set_minus_unreachable);
                while (work.Count != 0)
                {
                    CFG.CFGVertex node = work.First();
                    work.Remove(node);
                    if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                        System.Console.WriteLine("Compute phi-function for node " + node);
                    if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                        System.Console.WriteLine("predecessors " +
                            _cfg.PredecessorNodes(node).Aggregate(
                                new StringBuilder(),
                                (sb, v) =>
                                    sb.Append(v).Append(", "),
                                sb =>
                                {
                                    if (0 < sb.Length)
                                        sb.Length -= 2;
                                    return sb.ToString();
                                }));

                    // Verify all predecessors have identical stack sizes.
                    IEnumerable<int?> levels = _cfg.PredecessorNodes(node).Select(
                        (v) =>
                        {
                            return v.StackLevelOut;
                        }
                    );
                    int? previous = null;
                    bool first = true;
                    bool cannot_check = false;
                    foreach (int? l in levels)
                    {
                        if (first)
                        {
                            first = false;
                            previous = l;
                        }
                        else
                        {
                            if (l != previous)
                            {
                                if (Options.Singleton.Get(Options.OptionType.DisplaySSAComputation))
                                    System.Console.WriteLine(
                                        "Predecessor stack sizes do not agree.");
                                cannot_check = true;
                                break;
                            }
                        }
                    }
                    if (cannot_check)
                        continue;
                    // Every block has a stack that contains variables
                    // defined as phi function with all predecessors.
                    //if (!node.IsEntry)
                    {
                        for (int i = 0; i < node.StackLevelIn; ++i)
                        {
                            SSA.Phi phi = new SSA.Phi();
                            List<SSA.Value> list = _cfg.PredecessorNodes(node).Select(
                                (v) =>
                                {
                                    return v.StateOut._stack[i];
                                }
                            ).ToList();
                            phi._merge = list;
                            SSA.Value vx = node.StateIn._stack[i];
                            phi._v = vx;
                            phi._block = node;
                            ssa.phi_functions.Add(vx, phi);
                        }
                    }
                }
            }

            //System.Console.WriteLine("Final graph:");
            //_cfg.Dump();

            // Dump SSA phi functions.
            //System.Console.WriteLine("Phi functions");
            //foreach (KeyValuePair<SSA.Value, SSA.Phi> p in ssa.phi_functions)
            //{
            //    System.Console.WriteLine(p.Key + " "
            //        + p.Value._merge.Aggregate(
            //                new StringBuilder(),
            //                (sb, x) =>
             //                   sb.Append(x).Append(", "),
             //               sb =>
            //                {
            //                    if (0 < sb.Length)
            //                        sb.Length -= 2;
            //                    return sb.ToString();
            //                }));
            //}

        }

        // This algorithm is based on the one presented in
        // "The Static Single Assignment Book",
        // a book in progress by multiple authors,
        // located at https://gforge.inria.fr/scm/viewvc.php/ssabook/book/
        // The algorithm is presented by F. Brandner and D. Novillo
        // in Chapter 8, Propating Information using SSA, Algorithm 8.1
        // and 8.2. In addition to that presented, this algorithm
        // creates bindings of caller/callee arguments/parameters
        // so that the analysis of the indirect call instructions 
        // points to a specific function.

        private List<CFG.CFGVertex> VisitVertex(CFG.CFGVertex vertex)
        {
            SSA ssa = SSA.Singleton();

            List<CFG.CFGVertex> list = new List<CFG.CFGVertex>();

            // Examine the vertex for calls ...
            foreach (Inst inst in vertex._instructions)
            {
                Mono.Cecil.Cil.OpCode op = inst.OpCode;
                Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                for (; ; )
                {
                    // Examine all blocks for explicit call instructions.
                    // Add method reference to the graph.
                    if (!(fc == Mono.Cecil.Cil.FlowControl.Call))
                        break;
                    if (op.Code == Mono.Cecil.Cil.Code.Newobj)
                        break;
                    object operand = inst.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    if (call_to == null)
                        break;

                    System.Console.WriteLine("Caller/callee matching.");
                    System.Console.WriteLine("Caller: " + inst);
                    System.Console.WriteLine("Callee: " + call_to);

                    // Check if added before.
                    if (_cfg.FindEntry(call_to) == null)
                    {

                        // Add function to graph, convert nodes to SSA,
                        // and add them to the sparse data flow analysis.
                        this._cfg.StartChangeSet(this);
                        this._cfg.Add(call_to);
                        this._cfg.ExtractBasicBlocks();
                        foreach (CFG.CFGVertex v in this._cfg.EndChangeSet(this))
                            list.Add(v);
                    }
                    else
                    {
                        // create bindings parameters to function.
                    }
                    break;
                }

                for (; ; )
                {
                    if (op.Code != Mono.Cecil.Cil.Code.Newobj)
                        break;

                    object operand = inst.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    Mono.Cecil.MethodDefinition call_to_def = call_to != null ? call_to.Resolve() : null;

                    // For newobj, match up with constructor.
                    // For _Kernel_type, match a constructor for MulticastDelegate.
                    // Note, the after state top of stack is the return value,
                    // but it's also the object. Make sure to pass that into the
                    // first argument.
                    bool found = false;
                    CFG.CFGVertex callee = null;
                    if (call_to_def.FullName.Contains("Campy.AMP/_Kernel_type::.ctor"))
                    {
                        System.Console.WriteLine("Multicast delegate construction caller/callee matching.");
                        System.Console.WriteLine("Caller: " + inst);

                        // Match with MulticastDelegate constructor.
                        foreach (CFG.CFGVertex v in entries)
                        {
                            if (v.Method.FullName.Contains("System.MulticastDelegate::CtorClosed"))
                            {
                                System.Console.WriteLine("Callee " + v.Method);
                                found = true;
                                callee = v;
                                break;
                            }
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Newobj caller/callee matching.");
                        System.Console.WriteLine("Caller: " + inst);

                        // Match with constructor.
                        // With constructors, the object is top of stack AFTER the
                        // newobj function.
                        foreach (CFG.CFGVertex v in entries)
                        {
                            if (v.Method == call_to_def)
                            {
                                System.Console.WriteLine("Callee " + v.Method);
                                found = true;
                                callee = v;
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        // Add function to graph, convert nodes to SSA,
                        // and add them to the sparse data flow analysis.
                        this._cfg.StartChangeSet(this);
                        this._cfg.Add(call_to_def);
                        this._cfg.ExtractBasicBlocks();
                        foreach (CFG.CFGVertex v in this._cfg.EndChangeSet(this))
                            list.Add(v);
                        break;
                    }

                    // Match values pass to method with formal parameters of
                    // the method on the stack.
                    foreach (Nesting prev in inst.Block.StateIn._bindings)
                    {
                        Nesting n = new Nesting();
                        n._caller = inst;
                        n._previous = prev;
                        int top = inst.StateIn._stack.Count;
                        for (int k = 0; k < callee.NumberOfArguments; ++k)
                        {
                            SSA.Value argument = null;
                            if (k == 0)
                            {
                                argument = inst.StateOut._stack.PeekTop(0);
                            }
                            else
                            {
                                argument = inst.StateIn._stack.PeekTop(callee.NumberOfArguments - 1 - k);
                            }
                            SSA.Value formal_parameter = callee.StateIn._arguments[k];
                            SSA.Assignment a = new SSA.Assignment();
                            a.lhs = formal_parameter;
                            a.rhs = argument;
                            // Update call stack environment, and associate
                            // it with caller/callee.
                            n._parameter_argument_matching.Add(a);
                        }
                        inst.StateIn._bindings.Add(n);
                        callee.StateIn._bindings.Add(n);
                    }
                    break;
                }

                for (; ; )
                {
                    object operand = inst.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    Mono.Cecil.MethodDefinition call_to_def = call_to != null ? call_to.Resolve() : null;

                    if (call_to_def == null)
                        break;

                    // Some calls are delegate Invoke(). For these,
                    // get the delegate object, the block corresponding to the
                    // function of the delegate, then match args and parameters.

                    if (call_to_def.Name.Equals("Invoke"))
                    {
                        System.Console.WriteLine("Invoke Caller/callee matching.");
                        System.Console.WriteLine("Caller: " + inst);

                        // The first argument to the call is the object.
                        // Get the value.
                        int num_args = call_to_def.Parameters.Count;
                        if (call_to.HasThis) num_args++;
                        SSA.Value f = inst.StateIn._stack.PeekTop(num_args - 1);
                        SSA.Value b = Eval(0, inst.StateIn, f);
                        SSA.Block bb = b as SSA.Block;
                        if (!System.Object.ReferenceEquals(b, null))
                        {
                            CFG.CFGVertex e = (b as SSA.Block)._block;
                            foreach (CFG.CFGVertex v in entries)
                            {
                                if (e.Method == v.Method)
                                {
                                    System.Console.WriteLine("Callee " + v);
                                    break;
                                }
                            }
                        }
                    }
                    break;
                }

                for (; ; )
                {
                    object operand = inst.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    Mono.Cecil.MethodDefinition call_to_def = call_to != null ? call_to.Resolve() : null;

                    if (call_to_def == null)
                        break;

                    // The following is big time special case for Campy calls,
                    // where the function is called indirectly. If it's the parallel.for
                    // call, find out what we're calling.
                    if (call_to_def != null && call_to_def.Name.Equals("For")
                        && call_to_def.DeclaringType != null && call_to_def.DeclaringType.FullName.Equals("Campy.Parallel"))
                    {
                        System.Console.WriteLine("Campy.Parallel::For caller/callee matching.");
                        System.Console.WriteLine("Caller: " + inst);

                        // The state for the PFE instruction should have the top of stack
                        // containing the delegate object.
                        // Evaluate it for an address.

                        SSA.Value the_lambda_value = inst.StateIn._stack.PeekTop();
                        SSA.Value addr = Eval(0, inst.StateIn, the_lambda_value);

                        if (addr as SSA.Set != null)
                        {
                            SSA.Set set = addr as SSA.Set;
                            int count = set.list.Count;
                            if (count == 1)
                                addr = set.list.First();
                        }

                        String asdfasdf = addr.GetType().Name;
                        if (addr as SSA.Structure == null)
                            continue;

                        SSA.Structure s = addr as SSA.Structure;
                        // Define a Field which accesses the "_methodPtr" of
                        // the structure, which we can then evaluate.
                        Parallel._Kernel_type d = (Campy.Types.Index x) => { };
                        Mono.Cecil.TypeDefinition d_mono = Campy.Types.Utils.ReflectionCecilInterop.ConvertToMonoCecilTypeDefinition(d.GetType());
                        IEnumerable<Mono.Cecil.FieldDefinition> fies = d_mono.BaseType.Resolve().BaseType.Resolve().Fields.Where(
                            (f) => { if (f.Name.Equals("_methodPtr")) return true; else return false; });
                        Mono.Cecil.FieldDefinition d_field_mono = d_mono.BaseType.Resolve().BaseType.Resolve().Fields.Where(
                            (f) => { if (f.Name.Equals("_methodPtr")) return true; else return false; }).First();
                        SSA.Field field = new SSA.Field(s, d_field_mono);
                        // Evaluate field.
                        this._cfg.StartChangeSet(this);
                        SSA.Value vv = Eval(0, inst.StateIn, field);
                        if (!System.Object.ReferenceEquals(vv, null)
                            && vv as SSA.Block != null)
                        {
                            System.Console.WriteLine("Callee " + vv);
                        }
                        this._cfg.ExtractBasicBlocks();
                        foreach (CFG.CFGVertex v in this._cfg.EndChangeSet(this))
                            list.Add(v);
                        // else if (!System.Object.ReferenceEquals(vv, null))
                       //     throw new Exception("Unexpected callee");
                    }
                    break;
                }
            
            }
            return list;
        }

        public void SparseDataFlowPropagation(
            List<CFG.CFGVertex> start_nodes)
        {
            SSA ssa = SSA.Singleton();
            Dictionary<CFG.CFGEdge, bool> executable_edge = new Dictionary<CFG.CFGEdge, bool>();
            Dictionary<CFG.CFGVertex, bool> executable_vertex = new Dictionary<CFG.CFGVertex, bool>();
            StackQueue<CFG.CFGVertex> cfg_node_work_list = new StackQueue<CFG.CFGVertex>();
            StackQueue<CFG.CFGEdge> cfg_edge_work_list = new StackQueue<CFG.CFGEdge>();
            StackQueue<CFG.CFGVertex> ssa_work_list = new StackQueue<CFG.CFGVertex>();

            // Clear executable for all nodes and edges in graph.
            foreach (CFG.CFGVertex vertex in _cfg.VertexNodes)
                executable_vertex.Add(vertex, false);

            foreach (CFG.CFGEdge edge in _cfg.Edges)
                executable_edge.Add(edge, false);

            foreach (CFG.CFGVertex vertex in start_nodes)
                cfg_node_work_list.Push(vertex);

            for (; cfg_node_work_list.Count > 0 || cfg_edge_work_list.Count > 0; )
            {
                // Visit CFG nodes in work list.
                while (cfg_node_work_list.Count > 0)
                {
                    CFG.CFGVertex vertex = cfg_node_work_list.Pop();

                    if (executable_vertex[vertex])
                        continue;

                    executable_vertex[vertex] = true;

                    List<CFG.CFGVertex> list = VisitVertex(vertex);

                    foreach (CFG.CFGVertex v in list)
                    {
                        executable_vertex.Add(v, false);
                        foreach (CFG.CFGEdge e in v._Successors)
                            executable_edge.Add(e, false);
                    }

                    // Given vertex, set up successor edges.
                    foreach (CFG.CFGEdge edge in vertex._Successors)
                    {
                        if (! executable_edge[edge])
                            cfg_edge_work_list.Push(edge);
                    }
                }

                // 8.1.3
                while (cfg_edge_work_list.Count > 0)
                {
                    CFG.CFGEdge edge = cfg_edge_work_list.Pop();

                    executable_edge[edge] = true;

                    // Visit CFG node.
                    CFG.CFGVertex vertex = (CFG.CFGVertex)edge.to;

                    cfg_node_work_list.Push(vertex);
                }
            }


            // Get target lambda/delegate by working from this method data flow analysis.
            //FindCallTree(inst);

            
            //foreach (Inst inst in Inst.CallInstructions)
            //{
            //    Mono.Cecil.Cil.OpCode op = inst.OpCode;

            //    if (op.Code == Mono.Cecil.Cil.Code.Newobj)
            //        continue;

            //    object operand = inst.Operand;
            //    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
            //    Mono.Cecil.MethodDefinition call_to_def = call_to != null ? call_to.Resolve() : null;

            //    // Find call target in analyzed entries. If found,
            //    // create a phi function for each variable on stack
            //    // with parameters in caller.

            //    if (call_to_def == null)
            //        continue;

            //    System.Console.WriteLine("Caller/callee matching.");
            //    System.Console.WriteLine("Caller: " + inst);

            //    // Find a straight-forward match of the call to an entry,
            //    // using the method definition. This works much of the time,
            //    // but not always.
            //    foreach (CFG.CFGVertex v in entries)
            //    {
            //        if (call_to_def == v.Method)
            //        {
            //            System.Console.WriteLine("callee " + v);
            //            // Match values pass to method with formal parameters of
            //            // the method on the stack.
            //            int top = inst.StateIn._stack.Count;
            //            for (int k = 0; k < v.NumberOfArguments; ++k)
            //            {
            //                SSA.Value formal_parameter = v.StateIn._arguments[k];
            //                SSA.Value argument = inst.StateIn._stack[top - v.NumberOfArguments + k];
            //                SSA.Phi phi = null;
            //                ssa.phi_functions.TryGetValue(formal_parameter, out phi);
            //                if (phi == null)
            //                {
            //                    phi = new SSA.Phi();
            //                    ssa.phi_functions.Add(formal_parameter, phi);
            //                }
            //                List<SSA.Value> list = phi._merge;
            //                if (list == null)
            //                    list = new List<SSA.Value>();
            //                list.Add(argument);
            //                phi._merge = list;
            //            }
            //            break;
            //        }
            //    }
            //}

            //foreach (Inst inst in Inst.CallInstructions)
            //{
            //    Mono.Cecil.Cil.OpCode op = inst.OpCode;
            //    object operand = inst.Operand;
            //    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
            //    Mono.Cecil.MethodDefinition call_to_def = call_to != null ? call_to.Resolve() : null;

            //    if (call_to_def == null)
            //        continue;

            //    // Some calls are delegate Invoke(). For these,
            //    // get the delegate object, the block corresponding to the
            //    // function of the delegate, then match args and parameters.

            //    if (call_to_def.Name.Equals("Invoke"))
            //    {
            //        System.Console.WriteLine("Invoke Caller/callee matching.");
            //        System.Console.WriteLine("Caller: " + inst);

            //        // The first argument to the call is the object.
            //        // Get the value.
            //        int num_args = call_to_def.Parameters.Count;
            //        if (call_to.HasThis) num_args++;
            //        SSA.Value f = inst.StateIn._stack.PeekTop(num_args - 1);
            //        SSA.Value b = Eval(0, inst.StateIn, f);
            //        SSA.Block bb = b as SSA.Block;
            //        if (!System.Object.ReferenceEquals(b, null))
            //        {
            //            CFG.CFGVertex e = (b as SSA.Block)._block;
            //            foreach (CFG.CFGVertex v in entries)
            //            {
            //                if (e.Method == v.Method)
            //                {
            //                    System.Console.WriteLine("Callee " + v);
            //                    // Match values pass to method with formal parameters of
            //                    // the method on the stack.
            //                    int top = inst.StateIn._stack.Count;
            //                    for (int k = 0; k < v.NumberOfArguments; ++k)
            //                    {
            //                        SSA.Value formal_parameter = v.StateIn._arguments[k];
            //                        SSA.Value argument = inst.StateIn._stack[top - v.NumberOfArguments + k];
            //                        SSA.Phi phi = null;
            //                        ssa.phi_functions.TryGetValue(formal_parameter, out phi);
            //                        if (phi == null)
            //                        {
            //                            phi = new SSA.Phi();
            //                            ssa.phi_functions.Add(formal_parameter, phi);
            //                        }
            //                        List<SSA.Value> list = phi._merge;
            //                        if (list == null)
            //                            list = new List<SSA.Value>();
            //                        list.Add(argument);
            //                        phi._merge = list;
            //                    }
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}

            //foreach (Inst inst in Inst.CallInstructions)
            //{
            //    Mono.Cecil.Cil.OpCode op = inst.OpCode;

            //    if (op.Code != Mono.Cecil.Cil.Code.Newobj)
            //        continue;

            //    object operand = inst.Operand;
            //    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
            //    Mono.Cecil.MethodDefinition call_to_def = call_to != null ? call_to.Resolve() : null;

            //    if (call_to_def == null)
            //        continue;

            //    // For newobj, match up with constructor.
            //    // For _Kernel_type, match a constructor for MulticastDelegate.
            //    // Note, the after state top of stack is the return value,
            //    // but it's also the object. Make sure to pass that into the
            //    // first argument.

            //    if (call_to_def.FullName.Contains("Campy.AMP/_Kernel_type::.ctor"))
            //    {
            //        System.Console.WriteLine("Multicast delegate construction caller/callee matching.");
            //        System.Console.WriteLine("Caller: " + inst);

            //        // Match with MulticastDelegate constructor.
            //        foreach (CFG.CFGVertex v in entries)
            //        {
            //            if (v.Method.FullName.Contains("System.MulticastDelegate::CtorClosed"))
            //            {
            //                System.Console.WriteLine("Callee " + v.Method);
            //                // Match values pass to method with formal parameters of
            //                // the method on the stack.
            //                int top = inst.StateIn._stack.Count;
            //                for (int k = 0; k < v.NumberOfArguments; ++k)
            //                {
            //                    SSA.Value argument = null;
            //                    if (k == 0)
            //                    {
            //                        argument = inst.StateOut._stack.PeekTop(0);
            //                    }
            //                    else
            //                    {
            //                        argument = inst.StateIn._stack.PeekTop(v.NumberOfArguments - 1 - k);
            //                    }
            //                    SSA.Value formal_parameter = v.StateIn._arguments[k];
            //                    SSA.Phi phi = null;
            //                    ssa.phi_functions.TryGetValue(formal_parameter, out phi);
            //                    if (phi == null)
            //                    {
            //                        phi = new SSA.Phi();
            //                        ssa.phi_functions.Add(formal_parameter, phi);
            //                    }
            //                    List<SSA.Value> list = phi._merge;
            //                    if (list == null)
            //                        list = new List<SSA.Value>();
            //                    list.Add(argument);
            //                    phi._merge = list;
            //                }
            //                // Set up side effects on structure.
            //                break;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        System.Console.WriteLine("Newobj caller/callee matching.");
            //        System.Console.WriteLine("Caller: " + inst);

            //        // Match with constructor.
            //        // With constructors, the object is top of stack AFTER the
            //        // newobj function.
            //        foreach (CFG.CFGVertex v in entries)
            //        {
            //            if (v.Method == call_to_def)
            //            {
            //                System.Console.WriteLine("Callee " + v.Method);

            //                // Match values pass to method with formal parameters of
            //                // the method on the stack.

            //                int top = inst.StateIn._stack.Count;
            //                for (int k = 0; k < v.NumberOfArguments; ++k)
            //                {
            //                    SSA.Value argument = null;
            //                    if (k == 0)
            //                    {
            //                        argument = inst.StateOut._stack.PeekTop(0);
            //                    }
            //                    else
            //                    {
            //                        argument = inst.StateIn._stack.PeekTop(v.NumberOfArguments - 1 - k);
            //                    }
            //                    SSA.Value formal_parameter = v.StateIn._arguments[k];
            //                    SSA.Phi phi = null;
            //                    ssa.phi_functions.TryGetValue(formal_parameter, out phi);
            //                    if (phi == null)
            //                    {
            //                        phi = new SSA.Phi();
            //                        ssa.phi_functions.Add(formal_parameter, phi);
            //                    }
            //                    List<SSA.Value> list = phi._merge;
            //                    if (list == null)
            //                        list = new List<SSA.Value>();
            //                    list.Add(argument);
            //                    phi._merge = list;
            //                }
            //                // Set up side effects on structure.
            //                break;
            //            }
            //        }
            //    }
            //}

            //foreach (Inst inst in Inst.CallInstructions)
            //{
            //    Mono.Cecil.Cil.OpCode op = inst.OpCode;
            //    object operand = inst.Operand;
            //    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
            //    Mono.Cecil.MethodDefinition call_to_def = call_to != null ? call_to.Resolve() : null;

            //    if (call_to_def == null)
            //        continue;

            //    // Match Parallel_For_Each call with delegate method callee.
            //    // Note, the index parameter is undefined, but the instance for
            //    // the delegate can be set.

            //    if (call_to_def.FullName.Contains("Campy.AMP::Parallel_For_Each"))
            //    {
            //        System.Console.WriteLine("Parallel_For_Each caller/callee matching.");
            //        System.Console.WriteLine("Caller: " + inst);

            //        // The state for the PFE instruction should have the top of stack
            //        // containing the delegate object.
            //        // Evaluate it for an address.

            //        SSA.Value the_lambda_value = inst.StateIn._stack.PeekTop();
            //        SSA.Value addr = Eval(0, inst.StateIn, the_lambda_value);

            //        if (addr as SSA.Set != null)
            //        {
            //            SSA.Set set = addr as SSA.Set;
            //            int count = set.list.Count;
            //            if (count == 1)
            //                addr = set.list.First();
            //        }

            //        String asdfasdf = addr.GetType().Name;
            //        if (addr as SSA.Structure == null)
            //            continue;

            //        SSA.Structure s = addr as SSA.Structure;
            //        // Define a Field which accesses the "_methodPtr" of
            //        // the structure, which we can then evaluate.
            //        AMP._Kernel_type d = (Campy.Types.Index x) => { };
            //        Mono.Cecil.TypeDefinition d_mono = Campy.Types.Utils.ReflectionCecilInterop.ConvertToMonoCecilTypeDefinition(d.GetType());
            //        IEnumerable<Mono.Cecil.FieldDefinition> fies = d_mono.BaseType.Resolve().BaseType.Resolve().Fields.Where(
            //            (f) => { if (f.Name.Equals("_methodPtr")) return true; else return false; });
            //        Mono.Cecil.FieldDefinition d_field_mono = d_mono.BaseType.Resolve().BaseType.Resolve().Fields.Where(
            //            (f) => { if (f.Name.Equals("_methodPtr")) return true; else return false; }).First();
            //        SSA.Field field = new SSA.Field(s, d_field_mono);
            //        // Evaluate field.
            //        SSA.Value vv = Eval(0, inst.StateIn, field);
            //        System.Console.WriteLine("Eval of field _methodPtr is " + vv);

            //        //            if (!System.Object.ReferenceEquals(addr, null) && addr as SSA.Block != null)

            //        CFG.CFGVertex v = null;

            //        System.Console.WriteLine("Callee " + v);

            //        // Match argument/parameter for target only, i.e., arg[0].
            //        // The arg[1] cannot be matched because it is an index
            //        // set up at runtime.

            //        int top = inst.StateIn._stack.Count;
            //        int k = 0;
            //        SSA.Value formal_parameter = v.StateIn._arguments[k];
            //        SSA.Value argument = inst.StateIn._stack[top - v.NumberOfArguments + k];
            //        SSA.Phi phi = null;
            //        ssa.phi_functions.TryGetValue(formal_parameter, out phi);
            //        if (phi == null)
            //        {
            //            phi = new SSA.Phi();
            //            ssa.phi_functions.Add(formal_parameter, phi);
            //        }
            //        List<SSA.Value> list = phi._merge;
            //        if (list == null)
            //            list = new List<SSA.Value>();
            //        list.Add(argument);
            //        phi._merge = list;
            //        break;
            //    }
            //}
        }

        List<Nesting> GetBindings(State state, SSA.Value v, SSA.Value resolve_to_value)
        {
            SSA ssa = SSA.Singleton();

            if (System.Object.ReferenceEquals(v, null))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.AddressOf))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Array))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.ArrayElement))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.BinaryExpression))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Block))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Field))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Indirect))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.FloatingPoint32))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.FloatingPoint64))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Indirect))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Integer32))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Integer64))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Obj))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Phi))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Set))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Structure))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.UnaryExpression))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.UInteger32))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.UInteger64))
            {
                return new List<Nesting>();
            }
            else if (v.GetType() == typeof(SSA.Variable))
            {
                // If variable is phi function (_merge), ...
                if (ssa.phi_functions.ContainsKey(v))
                {
                    // The value is defined by a phi function.
                    // Evaluate the phi function for a set.
                    // If just one, then return the value, not a set.
                    SSA.Phi phi = ssa.phi_functions[v];
                    if (phi._block.IsEntry)
                    {
                        // Get block v defined in.
                        CFG.CFGVertex n = phi._block;

                        List<Nesting> result = new List<Nesting>();

                        // look up stack for args.
                        foreach (Nesting env in n.StateIn._bindings)
                        {
                            for (Nesting e = env; e != null; e = e._previous)
                            {
                                // Check each assignment to see if it will resolve into target.
                                foreach (SSA.Assignment ass in e._parameter_argument_matching)
                                {
                                    if (ass.lhs.Equals(v))
                                    {
                                        SSA.Value t = Eval(0, state, ass.rhs);
                                        if (!Object.ReferenceEquals(t, null) && t.Equals(resolve_to_value))
                                        {
                                            result.Add(env);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    }

                    {
                        List<Nesting> result = new List<Nesting>();
                        List<SSA.Value> list = phi._merge;
                        if (list.Count == 1)
                        {
                            return GetBindings(state, list.First(), resolve_to_value);
                        }
                        else if (list.Count == 0)
                            return result;
                        else
                        {
                            foreach (SSA.Value l in list)
                            {
                                List<Nesting> xx = GetBindings(state, l, resolve_to_value);
                                result.AddRange(xx);
                            }
                            return result;
                        }
                    }
                }
                else if (ssa._defined.ContainsKey(v))
                {
                    foreach (Inst ins in ssa._defined[v])
                    {
                        IEnumerable<SSA.Operation> operation_list = ssa._operation[ins];
                        int count_of_operation_list = operation_list.Count();
                        foreach (SSA.Operation operation in operation_list)
                        {
                            Type t = operation.GetType();
                            if (t == typeof(SSA.Assignment))
                            {
                                SSA.Assignment assignment = (SSA.Assignment)operation;
                                return GetBindings(state, assignment.rhs, resolve_to_value);
                            }
                            else
                                throw new Exception("Cannot determine semantics of Eval with non-assignment operation.");
                        }
                    }
                }
                else
                {
                }
                return new List<Nesting>();
            }
            else
            {
                System.Console.WriteLine("No phi for " + v);
                return new List<Nesting>();
            }
        }

        SSA.Value Eval(int level, State state, SSA.Value v, List<Nesting> bindings = null)
        {
            System.Console.WriteLine("".PadLeft(level * 3) + "Eval(" + v + ")");

            SSA ssa = SSA.Singleton();

            if (System.Object.ReferenceEquals(v, null))
            {
                System.Console.WriteLine("".PadLeft(level * 3) + "v is null. Dead end.");
                return null;
            }
            else if (v.GetType() == typeof(SSA.AddressOf))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Array))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.ArrayElement))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.BinaryExpression))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Block))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Field))
            {
                if (ssa.phi_functions.ContainsKey(v))
                {
                    // The value is defined by a phi function.
                    // Evaluate the phi function for a set.
                    // If just one, then return the value, not a set.
                    SSA.Phi phi = ssa.phi_functions[v];
                    System.Console.WriteLine("".PadLeft(level * 3) + "found phi " + phi);
                    List<SSA.Value> list = phi._merge;
                    if (list.Count == 1)
                    {
                        System.Console.WriteLine("".PadLeft(level * 3) + "--> Eval(" + list.First() + ")");
                        return Eval(level + 1, state, list.First(), bindings);
                    }
                    else if (list.Count == 0)
                        return null;
                    else
                    {
                        SSA.Set set_result = new SSA.Set();
                        foreach (SSA.Value l in list)
                        {
                            System.Console.WriteLine("".PadLeft(level * 3) + "--> Eval(" + l + ")");
                            SSA.Value xx = Eval(level + 1, state, l, bindings);
                            set_result.Add(xx);
                        }
                        return set_result;
                    }
                }
                if (ssa._defined.ContainsKey(v))
                {
                    System.Console.WriteLine("".PadLeft(level * 3) + v + " in defined list.");
                    foreach (Inst ins in ssa._defined[v])
                    {
                        System.Console.WriteLine("".PadLeft(level * 3) + ins);
                        IEnumerable<SSA.Operation> operation_list = ssa._operation[ins];
                        int count_of_operation_list = operation_list.Count();
                        foreach (SSA.Operation operation in operation_list)
                        {
                            Type t = operation.GetType();
                            if (t == typeof(SSA.Assignment))
                            {
                                SSA.Assignment assignment = (SSA.Assignment)operation;
                                System.Console.WriteLine(assignment);
                                System.Console.WriteLine("".PadLeft(level * 3) + "--> Eval(" + assignment.rhs + ")");
                                return Eval(level + 1, state, assignment.rhs, bindings);
                            }
                            else
                                throw new Exception("Cannot determine semantics of Eval with non-assignment operation.");
                        }
                    }
                }
                SSA.Field v_field = v as SSA.Field;

                // Search entire defined for field set value that resolves into this
                // field fetch value. The key is that the name of the field
                // must match.

                StackQueue<SSA.Field> to_do = new StackQueue<SSA.Field>();
                StackQueue<SSA.Field> setters = new StackQueue<SSA.Field>();
                Mono.Cecil.TypeDefinition td2 = v_field._field.DeclaringType.Resolve();

                foreach (KeyValuePair<SSA.Value, List<Inst>> va in ssa._defined)
                {
                    if (va.Key as SSA.Field != null)
                    {
                        SSA.Field f = va.Key as SSA.Field;
                        System.Console.WriteLine(f + " " + v_field);
                        bool eq = f._field == v_field._field;
                        bool eq2 = f._field.FullName.Equals(v_field._field.FullName);
                        Mono.Cecil.TypeDefinition td = f._field.DeclaringType.Resolve();
                        bool eq3 = td.FullName.Equals(td2.FullName);
                        if (eq || (eq2 && eq3))
                            to_do.Push(f);
                    }
                }

                // Work list "to_do" contains all setters for an object with the field.
                // Check each matched setter of the field to see if it matches
                // the object. Worse case is to just allow everything, but that
                // causes problems later on.
                // Resolve object of Field, and test if it's the object requested.

                SSA.Value v_obj_eval = Eval(level + 1, state, v_field._obj, bindings);
                while (to_do.Count > 0)
                {
                    SSA.Field f = to_do.Pop();
                    SSA.Value f_obj_eval = Eval(level + 1, state, f._obj, bindings);
                    if (SSA.ValueCompare.Eq(f_obj_eval, v_obj_eval))
                        setters.Push(f);
                    else if (f_obj_eval as SSA.Set != null)
                    {
                        SSA.Set set = f_obj_eval as SSA.Set;
                        if ((!System.Object.Equals(v_obj_eval, null)) && set.list.Contains(v_obj_eval))
                            setters.Push(f);
                    }
                }

                // Each setter get value.
                if (setters.Count == 0)
                    return null;
                SSA.Set result = new SSA.Set();
                foreach (SSA.Field setter in setters)
                {
                    // Derive context so that obj resolves into
                    // specific object.
                    List<Nesting> bind = GetBindings(state, setter._obj, v_obj_eval);
                    SSA.Value results = Eval(level + 1, state, setter, bind);
                    result.Add(results);
                }
                return result;
            }
            else if (v.GetType() == typeof(SSA.Indirect))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.FloatingPoint32))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.FloatingPoint64))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Indirect))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Integer32))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Integer64))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Obj))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Phi))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Set))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Structure))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.UnaryExpression))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.UInteger32))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.UInteger64))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Variable))
            {
                // If variable is phi function (_merge), ...
                if (ssa.phi_functions.ContainsKey(v))
                {
                    // The value is defined by a phi function.
                    // Evaluate the phi function for a set.
                    // If just one, then return the value, not a set.
                    SSA.Phi phi = ssa.phi_functions[v];
                    if (phi._block.IsEntry)
                    {
                        // Get block v defined in.
                        CFG.CFGVertex n = phi._block;

                        SSA.Set set = new SSA.Set();

                        if (bindings == null)
                            bindings = n.StateIn._bindings;

                        {
                            // look up stack for args.
                            foreach (Nesting env in bindings)
                            {
                                for (Nesting e = env; e != null; e = e._previous)
                                {
                                    foreach (SSA.Assignment ass in e._parameter_argument_matching)
                                    {
                                        if (ass.lhs.Equals(v))
                                        {
                                            System.Console.WriteLine("".PadLeft(level * 3) + "Arg match " + v + " = " + ass.rhs);

                                            // Found lhs.
                                            SSA.Value value = Eval(level + 1, state, ass.rhs, bindings);
                                            set.Add(value);
                                        }
                                    }
                                }
                            }
                            System.Console.WriteLine("".PadLeft(level * 3) + "set ret " + set);
                            return set;
                        }
                        return null;
                    }
                    else
                    {
                        System.Console.WriteLine("".PadLeft(level * 3) + "found phi " + phi);
                        List<SSA.Value> list = phi._merge;
                        if (list.Count == 1)
                        {
                            System.Console.WriteLine("".PadLeft(level * 3) + "--> Eval(" + list.First() + ")");
                            return Eval(level + 1, state, list.First(), bindings);
                        }
                        else if (list.Count == 0)
                            return null;
                        else
                        {
                            SSA.Set set_result = new SSA.Set();
                            foreach (SSA.Value l in list)
                            {
                                System.Console.WriteLine("".PadLeft(level * 3) + "--> Eval(" + l + ")");
                                SSA.Value xx = Eval(level + 1, state, l, bindings);
                                set_result.Add(xx);
                            }
                            return set_result;
                        }
                    }
                }
                else if (ssa._defined.ContainsKey(v))
                {
                    System.Console.WriteLine("".PadLeft(level * 3) + v + " in defined list.");
                    foreach (Inst ins in ssa._defined[v])
                    {
                        System.Console.WriteLine("".PadLeft(level * 3) + ins);
                        IEnumerable<SSA.Operation> operation_list = ssa._operation[ins];
                        int count_of_operation_list = operation_list.Count();
                        foreach (SSA.Operation operation in operation_list)
                        {
                            Type t = operation.GetType();
                            if (t == typeof(SSA.Assignment))
                            {
                                SSA.Assignment assignment = (SSA.Assignment)operation;
                                System.Console.WriteLine(assignment);
                                System.Console.WriteLine("".PadLeft(level * 3) + "--> Eval(" + assignment.rhs + ")");
                                return Eval(level + 1, state, assignment.rhs, bindings);
                            }
                            else
                                throw new Exception("Cannot determine semantics of Eval with non-assignment operation.");
                        }
                    }
                }
                else
                {
                }
                return null;
            }
            else
            {
                System.Console.WriteLine("No phi for " + v);
                return null;
            }
        }

        public void FindCallTree(Inst call)
        {
            List<CFG.CFGVertex> entries = _cfg.Entries;
            State state = call.StateIn;
            Campy.Graphs.TreeLinkedList<SSA.Block> tree = new Graphs.TreeLinkedList<SSA.Block>();
            StackQueue<CFG.CFGVertex> work = new StackQueue<CFG.CFGVertex>();
            List<CFG.CFGVertex> visited = new List<CFG.CFGVertex>();

            // The state for the PFE instruction should have the top of stack
            // containing the delegate object.
            // Evaluate it for an address, and start the transitive
            // closure of the calls.

            SSA.Value the_delegate_structure = call.StateIn._stack.PeekTop();
            SSA.Value addr = Eval(0, state, the_delegate_structure);
            Type ttttttttt = addr.GetType();
            System.Console.WriteLine(ttttttttt.FullName);
            if (! ttttttttt.Name.Equals("Structure"))
            {
                System.Console.WriteLine("Delegate is not a structure. Cannot find PFE call tree.");
                return;
            }
            // Get field System.Delegate::_methodPtr of delegate.
            Parallel._Kernel_type d = (Campy.Types.Index x) => {  };
            Mono.Cecil.TypeDefinition d_mono = Campy.Types.Utils.ReflectionCecilInterop.ConvertToMonoCecilTypeDefinition(d.GetType());
            foreach (Mono.Cecil.FieldDefinition fd in d_mono.BaseType.Resolve().BaseType.Resolve().Fields)
            {
                System.Console.WriteLine(fd.Name);
            }
            IEnumerable<Mono.Cecil.FieldDefinition> fies = d_mono.BaseType.Resolve().BaseType.Resolve().Fields.Where(
                (f) => { if (f.Name.Equals("_methodPtr")) return true; else return false; });
            foreach (Mono.Cecil.FieldDefinition fd in fies)
            {
                System.Console.WriteLine(fd.Name);
            }
            Mono.Cecil.FieldDefinition d_field_mono = d_mono.BaseType.Resolve().BaseType.Resolve().Fields.Where(
                (f) => { if (f.Name.Equals("_methodPtr")) return true; else return false; }).First();
            SSA.Field field = new SSA.Field(addr, d_field_mono);
            // Evaluate field.
            SSA.Value vv = Eval(0, state, field);

            if (!System.Object.ReferenceEquals(addr, null) && addr as SSA.Block != null)
            {
                SSA.Block b = addr as SSA.Block;
                work.Push(b._block);
            }
            while (work.Count > 0)
            {
                CFG.CFGVertex block = work.DequeueBottom();
                if (visited.Contains(block))
                    continue;
                visited.Add(block);
                foreach (CFG.CFGVertex e in _cfg.SuccessorNodes(block))
                {
                    work.Push(e);
                }
                foreach (Inst inst in block._instructions)
                {
                    Mono.Cecil.Cil.OpCode op = inst.OpCode;
                    Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                    object operand = inst.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    if (fc == Mono.Cecil.Cil.FlowControl.Call && call_to != null)
                    {
                        System.Console.WriteLine("Found call " + call_to.FullName);
                        Mono.Cecil.MethodDefinition md = call_to.Resolve();
                        if (md.Body == null && md.Name.Equals("Invoke"))
                        {
                            // The first argument to the call is the object.
                            // Get the value.
                            int num_args = md.Parameters.Count;
                            if (call_to.HasThis) num_args++;
                            SSA.Value f = inst.StateIn._stack.PeekTop(num_args - 1);
                            SSA.Value b = Eval(0, state, f);
                        }
                        CFG.CFGVertex n = _cfg.FindEntry(call_to);
                        if (n != null)
                            work.Push(n);
                    }
                }
            }
        }

        List<StackQueue<CFG.CFGVertex>> FollowPredecessors(CFG.CFGVertex node, StackQueue<CFG.CFGVertex> chain)
        {
            List<StackQueue<CFG.CFGVertex>> result = new List<StackQueue<CFG.CFGVertex>>();

            if (node.IsEntry)
            {
                result.Add(new StackQueue<CFG.CFGVertex>(chain));
            }

            // Visit predecessors.
            foreach (CFG.CFGVertex pred in node._Graph.PredecessorNodes(node))
            {
                // Stop if visited before.
                if (chain.Contains(pred))
                    continue;

                // Stop if return edge.
                if (pred.IsReturn)
                    continue;

                // Visit succ.
                chain.Push(pred);
                List<StackQueue<CFG.CFGVertex>> chains = FollowPredecessors(pred, chain);
                chain.Pop();

                // Union two sets with maximal chains.
                result = UnionChainSets(result, chains);
            }
            return result;
        }

        List<StackQueue<CFG.CFGVertex>> FollowSuccessors(CFG.CFGVertex node, StackQueue<CFG.CFGVertex> chain)
        {
            List<StackQueue<CFG.CFGVertex>> result = new List<StackQueue<CFG.CFGVertex>>();

            // Stop if return edge.
            if (node.IsReturn)
            {
                result.Add(new StackQueue<CFG.CFGVertex>(chain));
                return result;
            }

            // Visit children.
            foreach (CFG.CFGVertex succ in node._Graph.SuccessorNodes(node))
            {
                // Stop if visited before.
                if (chain.Contains(succ))
                    continue;

                // Visit succ.
                chain.Push(succ);
                List<StackQueue<CFG.CFGVertex>> chains = FollowSuccessors(succ, chain);
                chain.Pop();

                // Union two sets with maximal chains.
                result = UnionChainSets(result, chains);
            }
            return result;
        }

        List<StackQueue<CFG.CFGVertex>> UnionChainSets(List<StackQueue<CFG.CFGVertex>> a, List<StackQueue<CFG.CFGVertex>> b)
        {
            List<StackQueue<CFG.CFGVertex>> result = new List<StackQueue<CFG.CFGVertex>>(a);
            foreach (StackQueue<CFG.CFGVertex> p in b)
            {
                StackQueue<CFG.CFGVertex> chain_of_entry_p = ChainOfEntry(p);
                bool dup = false;
                // Check if result list.
                for (int i = 0; i < result.Count; ++i)
                {
                    StackQueue<CFG.CFGVertex> r = result[i];
                    StackQueue<CFG.CFGVertex> chain_of_entry_r = ChainOfEntry(r);
                    if (IsPrefix(chain_of_entry_r, chain_of_entry_p))
                    {
                        result[i] = p;
                        dup = true;
                        break;
                    }
                    else if (IsPrefix(chain_of_entry_p, chain_of_entry_r))
                    {
                        dup = true;
                        break;
                    }
                }
                if (!dup)
                {
                    result.Add(p);
                }
            }
            return result;
        }

        StackQueue<CFG.CFGVertex> ChainOfEntry(StackQueue<CFG.CFGVertex> o)
        {
            StackQueue<CFG.CFGVertex> result = new StackQueue<CFG.CFGVertex>();
            for (int i = 0; i < o.Size(); ++i)
            {
                CFG.CFGVertex v = o[i];
                if (v.IsEntry)
                    result.Push(v);
            }
            return result;
        }

        bool IsPrefix(StackQueue<CFG.CFGVertex> a, StackQueue<CFG.CFGVertex> b)
        {
            if (a.Size() > b.Size())
                return false;
            for (int i = 0; i < a.Size(); ++i)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }
    }
}
