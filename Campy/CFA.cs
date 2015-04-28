using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Campy.Utils;

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
        static List<Inst> _call_instructions = new List<Inst>();

        public void AnalyzeCFG()
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
            List<CFG.CFGVertex> entries = new List<CFG.CFGVertex>();
            foreach (CFG.CFGVertex node in _cfg.VertexNodes)
                if (node.IsEntry)
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
            List<CFG.CFGVertex> change_set = _cfg.ChangeSet();
            System.Console.WriteLine("Change set:");
            foreach (CFG.CFGVertex xxxxxx in change_set)
            {
                System.Console.WriteLine(xxxxxx);
            }
            System.Console.WriteLine();

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
                                    System.Console.WriteLine("Warning: Predecessor " + pred
                                        + ", of node " + node
                                        + ", has not been visited.");
                                    continue;
                                }
                                // Warn if predecessor does not concur with another predecessor.
                                if (in_level != -1 && in_level != pred.StackLevelOut)
                                {
                                    System.Console.WriteLine("Warning: Predecessor " + pred
                                        + ", of node " + node
                                        + ", has a stack level different from another predecessor.");
                                }
                                System.Console.WriteLine("Node " + node + " level in set to " + pred.StackLevelOut + " via pred " + pred);
                                node.StackLevelIn = pred.StackLevelOut;
                                in_level = (int)node.StackLevelIn;
                            }
                            // Warn if no predecessors have been visited.
                            if (in_level == -1)
                            {
                                System.Console.WriteLine("Node " + node
                                    + " has no predecessors visited. Cannot process.");
                                continue;
                            }
                        }

                        System.Console.WriteLine();
                        System.Console.WriteLine("Processing block " + node);
                        System.Console.WriteLine("Args " + node.NumberOfArguments + " locs = " + node.NumberOfLocals);
                        System.Console.WriteLine("Level in = " + node.StackLevelIn + " level out = " + node.StackLevelOut);
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
                                System.Console.WriteLine();
                                System.Console.WriteLine("Failed stack level out check for block " + node);
                                _cfg.Dump();
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
                                System.Console.WriteLine("WARNING: level decrease " + succ);
                                continue;
                            }
                            else if (succ.StackLevelIn == level_after)
                            {
                                continue;
                            }
                            System.Console.WriteLine("Update successor " + succ);
                            System.Console.WriteLine("level in = " + level_after + " level out (pre) " + node.StackLevelOut);
                            if (!work.Contains(succ))
                            {
                                work.Add(succ);
                            }
                        }
                        System.Console.WriteLine();
                        foreach (CFG.CFGVertex xx in _cfg.VertexNodes)
                        {
                            System.Console.WriteLine("Node " + xx + " level in " + xx.StackLevelIn + " level out " + xx.StackLevelOut);
                        }
                        System.Console.WriteLine();
                    }
                }
            }

            //******************************************************
            //
            // STEP 7.
            //
            // Convert change set to SSA representation.
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
                            System.Console.WriteLine("Node " + node
                                + " is on queue, but hasn't been processed. Skipping.");
                            continue;
                        }

                        // Note, a new state creates a set of phi
                        // functions to handle edges from all predecessors.
                        // After computing SSA for all predecessors,
                        // we update the phi functions. So, order of processing
                        // nodes for SSA is irrelevant.

                        int level_in = (int)node.StackLevelIn;
                        node.StateIn = new State(node.Method, level_in);
                        State state_after = new State(node.StateIn);
                        State state_pre = new State(state_after);
                        System.Console.WriteLine();
                        System.Console.WriteLine("Node " + node + " in state");
                        int level_after = level_in;
                        int level_pre = level_after;
                        foreach (Inst i in node._instructions)
                        {
                            state_pre = new State(state_after);
                            state_pre.Dump();
                            level_pre = level_after;
                            if (i.Instruction.OpCode.FlowControl == Mono.Cecil.Cil.FlowControl.Call)
                            {
                                i.StateIn = new State(state_pre);
                                _call_instructions.Add(i);
                            }
                            System.Console.WriteLine(i);
                            i.ComputeStackLevel(ref level_after);
                            i.ComputeSSA(ref state_after);
                            Debug.Assert(level_after == state_after._stack.Size());
                            // Save before and after state for call instructions.
                            // This will come in handy for alias analysis.
                            if (i.Instruction.OpCode.FlowControl == Mono.Cecil.Cil.FlowControl.Call)
                                i.StateOut = new State(state_after);
                        }
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
                    System.Console.WriteLine("Compute phi-function for node " + node);
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
                                System.Console.WriteLine(
                                    "Predecessor stack sizes do not agree.");
                                cannot_check = true;
                                break;
                            }
                        }
                    }
                    if (cannot_check)
                        continue;
                    for (int i = 0; i < node.StackLevelIn; ++i)
                    {
                        SSA.Phi phi = new SSA.Phi();
                        List<SSA.Value> list = _cfg.PredecessorNodes(node).Select(
                            (v) =>
                            {
                                return v.StateOut._stack[i];
                            }
                        ).ToList();
                        phi.merge = list;
                        SSA.Value vx = node.StateIn._stack[i];
                        phi.v = vx;
                        ssa.phi_functions.Add(vx, phi);
                    }
                }
            }

            //******************************************************
            //
            // STEP 9.
            //
            // Do alias analysis for interprocedural calls.
            //
            //      For each call function, set up state in phi
            //      function.
            //
            //******************************************************
            {
                foreach (Inst i in _call_instructions)
                {
                    // Find call target in analyzed entries. If found,
                    // create a phi function for each variable on stack
                    // with parameters in caller.
                    Mono.Cecil.Cil.OpCode op = i.OpCode;
                    object operand = i.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    Mono.Cecil.MethodDefinition def = call_to != null ? call_to.Resolve() : null;
                    if (def != null)
                    {
                        foreach (CFG.CFGVertex v in entries)
                        {
                            if (def == v.Method)
                            {
                                System.Console.WriteLine("Found " + v.Method);
                                // Match values pass to method with formal parameters of
                                // the method on the stack.
                                int top = i.StateIn._stack.Count;
                                for (int k = 0; k < v.NumberOfArguments; ++k)
                                {
                                    SSA.Value formal_parameter = v.StateIn._arguments[k];
                                    SSA.Value argument = i.StateIn._stack[top - v.NumberOfArguments + k];
                                    SSA.Phi phi = null;
                                    ssa.phi_functions.TryGetValue(formal_parameter, out phi);
                                    if (phi == null)
                                    {
                                        phi = new SSA.Phi();
                                        ssa.phi_functions.Add(formal_parameter, phi);
                                    }
                                    List<SSA.Value> list = phi.merge;
                                    if (list == null)
                                        list = new List<SSA.Value>();
                                    list.Add(argument);
                                    phi.merge = list;
                                }
                            }
                        }
                        // In following instruction, set up return.
                    }
                }
            }

            System.Console.WriteLine("Final graph:");
            _cfg.Dump();

            // Dump SSA phi functions.
            System.Console.WriteLine("Phi functions");
            foreach (KeyValuePair<SSA.Value, SSA.Phi> p in ssa.phi_functions)
            {
                System.Console.WriteLine(p.Key + " "
                    + p.Value.merge.Aggregate(
                            new StringBuilder(),
                            (sb, x) =>
                                sb.Append(x).Append(", "),
                            sb =>
                            {
                                if (0 < sb.Length)
                                    sb.Length -= 2;
                                return sb.ToString();
                            }));
            }

            // Find all pfe's in graph.
            foreach (CFG.CFGVertex node in _cfg.VertexNodes)
            {
                foreach (Inst i in node._instructions)
                {
                    Mono.Cecil.Cil.OpCode op = i.OpCode;
                    Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                    object operand = i.Operand;
                    Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                    if (fc == Mono.Cecil.Cil.FlowControl.Call && call_to != null && call_to.Name.Equals("Parallel_For_Each"))
                    {
                        System.Console.WriteLine("Found PFE in block " + node.Name);
                        // Get target lambda/delegate by working from this method data flow analysis.
                        FindCallTree(i, entries);
                    }
                }
            }
        }

        SSA.Value Eval(SSA.Value v)
        {
            System.Console.WriteLine("Eval(" + v + ")");

            SSA ssa = SSA.Singleton();

            if (v == null)
            {
                System.Console.WriteLine("v is null. Dead end.");
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
            else if (v.GetType() == typeof(SSA.DerefOf))
            {
                return v;
            }
            else if (v.GetType() == typeof(SSA.Field))
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
                if (ssa.phi_functions.ContainsKey(v))
                {
                    // The value is defined by a phi function.
                    // Evaluate the phi function for a set.
                    // If just one, then return the value, not a set.
                    SSA.Phi phi = ssa.phi_functions[v];
                    System.Console.WriteLine("found phi " + phi);
                    List<SSA.Value> list = phi.merge;
                    if (list.Count == 1)
                        return Eval(list.First());
                    else if (list.Count == 0)
                        return null;
                    else
                    {
                        SSA.Set set_result = new SSA.Set();
                        foreach (SSA.Value l in list)
                        {
                            set_result.Add(Eval(l));
                        }
                        return set_result;
                    }
                }
                else if (ssa._defined.ContainsKey(v))
                {
                    System.Console.WriteLine("No phi for " + v);
                    System.Console.WriteLine("in defined list");

                    // Check for v of newobj instruction for kernel type.
                    foreach (Inst ins in ssa._defined[v])
                    {
                        if (ins.Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Newobj)
                        {
                            object method = ins.Operand;
                            if (method as Mono.Cecil.MethodReference != null)
                            {
                                Mono.Cecil.MethodReference mr = method as Mono.Cecil.MethodReference;
                                if (mr.FullName.Contains("_Kernel_type"))
                                {
                                    // The top of stack for the call instruction
                                    // contains a pointer to the function.
                                    SSA.Value f = ins.StateIn._stack.PeekTop();
                                    return Eval(f);
                                }
                            }
                        }
                        else
                        {
                            foreach (SSA.Operation operation in ssa._operation[ins])
                            {
                                Type t = operation.GetType();
                                if (t == typeof(SSA.Assignment))
                                {
                                    SSA.Assignment assignment = (SSA.Assignment)operation;
                                    return Eval(assignment.rhs);
                                }
                            }
                        }
                    }
                }
                return null;
            }
            else
            {
                System.Console.WriteLine("No phi for " + v);
                return null;
            }
        }

        void FindCallTree(Inst call, List<CFG.CFGVertex> entries)
        {
            Campy.Graphs.TreeLinkedList<SSA.Block> tree = new Graphs.TreeLinkedList<SSA.Block>();
            StackQueue<CFG.CFGVertex> work = new StackQueue<CFG.CFGVertex>();
            List<CFG.CFGVertex> visited = new List<CFG.CFGVertex>();

            // The state for the PFE instruction should have the top of stack
            // containing the delegate object.
            // Evaluate it for an address, and start the transitive
            // closure of the calls.

            SSA.Value the_lambda_value = call.StateIn._stack.PeekTop();
            SSA.Value addr = Eval(the_lambda_value);
            if (addr != null && addr as SSA.Block != null)
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
                            SSA.Value b = Eval(f);
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
