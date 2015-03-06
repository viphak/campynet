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

        public void AnalyzeCFG()
        {
            // Find all blocks marked as entries. These should just
            // be the first block of each method.
            List<CFG.CFGVertex> entries = new List<CFG.CFGVertex>();
            foreach (CFG.CFGVertex node in _cfg.VertexNodes)
                if (node.IsEntry)
                    entries.Add(node);

            // Compute number of args, locals, and return for each method block.
            foreach (CFG.CFGVertex node in _cfg.VertexNodes)
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
                    if (!tr.FullName.Equals("System.Void"))
                        ret++;
                }
                node.HasReturn = ret > 0;
            }

            List<CFG.CFGVertex> visited = new List<CFG.CFGVertex>();

            // Compute stack size for each basic block.
            // In theory, we don't need this, but it functions to double check
            // unimplemented instructions.
            StackQueue<CFG.CFGVertex> worklist = new StackQueue<CFG.CFGVertex>();
            foreach (CFG.CFGVertex entry in entries)
            {
                worklist = new StackQueue<CFG.CFGVertex>();
                entry.StackLevelIn = entry.NumberOfArguments + entry.NumberOfLocals;
                worklist.Push(entry);
                while (worklist.Size() > 0)
                {
                    CFG.CFGVertex node = worklist.Pop();
                    // Keep two different counters:
                    // level, the level before and after all instructions
                    // except calls.
                    // level_with_call, the level during a call.
                    System.Console.WriteLine();
                    System.Console.WriteLine("Processing block " + node);
                    System.Console.WriteLine("Args + locs = " + (entry.NumberOfArguments + entry.NumberOfLocals));
                    System.Console.WriteLine("Level in = " + node.StackLevelIn);
                    int level_after = node.StackLevelIn;
                    int level_pre = level_after;
                    foreach (Inst i in node._instructions)
                    {
                        level_pre = level_after;
                        i.ComputeStackLevel(ref level_after);
                        System.Console.WriteLine("after inst " + i);
                        System.Console.WriteLine("level = " + level_after);
                        Debug.Assert(level_after >= node.NumberOfLocals + node.NumberOfArguments);
                    }
                    node.StackLevelOut = level_after;
                    node.StackLevelPreLastInstruction = level_pre;
                    foreach (CFG.CFGVertex succ in node._Graph.SuccessorNodes(node))
                    {
                        // If it's an interprocedural edge, nothing to pass on.
                        if (succ.Method != node.Method)
                            continue;
                        // If it's recursive, nothing more to do.
                        if (succ == entry)
                            continue;
                        // If it's a return, nothing more to do also.
                        if (node._instructions.Last() as i_ret != null)
                            continue;
                        System.Console.WriteLine("Update successor " + succ);
                        System.Console.WriteLine("level in = " + level_after);
                        succ.StackLevelIn = level_after;
                        Debug.Assert(level_after >= succ.NumberOfLocals + succ.NumberOfArguments);
                        if (!visited.Contains(succ))
                        {
                            worklist.Push(succ);
                            visited.Add(succ);
                        }
                    }
                }
            }

            _cfg.Dump();

            // Verify any functions have +1 of args and locs left on stack for exit block.
            foreach (CFG.CFGVertex node in _cfg.VertexNodes)
            {
                if (node.IsReturn)
                {
                    Debug.Assert(node.StackLevelOut ==
                        node.NumberOfArguments +
                        node.NumberOfLocals +
                        (node.HasReturn ? 1 : 0));
                }
            }

            // Perform initial constant propagation for all nodes, each in isolation.
            worklist = new StackQueue<CFG.CFGVertex>();
            foreach (CFG.CFGVertex node in _cfg.VertexNodes)
            {
                node.StateIn = new State(node.Method, node.StackLevelIn);
                State state_after = new State(node.StateIn);
                State state_pre = new State(state_after);
                System.Console.WriteLine();
                System.Console.WriteLine("Node " + node + " in state");
                node.StateIn.Dump();
                int level_after = node.StackLevelIn;
                int level_pre = level_after;
                foreach (Inst i in node._instructions)
                {
                    state_pre = new State(state_after);
                    level_pre = level_after;
                    i.ComputeStackLevel(ref level_after);
                    i.Execute(ref state_after);
                    System.Console.WriteLine("after inst " + i);
                    Debug.Assert(level_after == state_after._stack.Size());
                    state_after.Dump();
                }
                System.Console.WriteLine("Node " + node + " out state");
                state_after.Dump();
                System.Console.WriteLine();
                node.StateOut = state_after;
                node.StatePreLastInstruction = state_pre;
                worklist.Push(node);
            }

            // Now, perform constant propagation with predecessor information.
            while (worklist.Count > 0)
            {
                CFG.CFGVertex node = worklist.DequeueBottom();
                State initial = new State(node.Method, node.StackLevelIn);

                System.Console.WriteLine();
                System.Console.WriteLine("Node " + node + " in state");
                node.StateIn.Dump();
                System.Console.WriteLine("Node " + node + " out state");
                node.StateOut.Dump();

                // Add in predecessor states.
                foreach (CFG.CFGVertex pred in _cfg.PredecessorNodes(node))
                {
                    System.Console.WriteLine("predecessor " + pred + " out state ");
                    pred.StateOut.Dump();
                    if (pred.IsCall && pred.Method != node.Method)
                    {
                        // Predecessor contains a call; this block is the entry for the callee.
                        // Copy call args to node state.
                        initial.UnionCall(pred);
                        System.Console.WriteLine("pred " + pred + " update");
                        initial.Dump();
                    }
                    else if (pred.IsReturn && pred.Method != node.Method)
                    {
                        // Predecessor contains a return instruction.
                        // This block is an interprocedural return.
                        // Push return value, if any.
                        initial.UnionReturn(pred);
                        System.Console.WriteLine("pred " + pred + " update");
                        initial.Dump();
                    }
                    else
                    {
                        // Non-interprocedural edge. Just add in state changes from
                        // predecessor.
                        initial.Union(pred);
                        System.Console.WriteLine("pred " + pred + " update");
                        initial.Dump();
                    }
                }
                System.Console.WriteLine("Node " + node + " in state after merging predecessors");
                initial.Dump();
                if (initial != node.StateIn)
                {
                    System.Console.WriteLine("initial state does not equal state in.");
                    node.StateIn.Dump();
                    // Perform data flow.
                    node.StateIn = initial;
                    State state_after = new State(initial);
                    State state_pre = new State(initial);
                    int level_after = node.StackLevelIn;
                    int level_pre = level_after;
                    foreach (Inst i in node._instructions)
                    {
                        level_pre = level_after;
                        state_pre = new State(state_after);
                        i.ComputeStackLevel(ref level_after);
                        System.Console.WriteLine("inst update before " + i);
                        state_after.Dump();
                        i.Execute(ref state_after);
                        System.Console.WriteLine("inst update after " + i);
                        state_after.Dump();
                        Debug.Assert(level_after == state_after._stack.Size());
                    }
                    node.StateOut = state_after;
                    node.StatePreLastInstruction = state_pre;
                    foreach (CFG.CFGVertex succ in _cfg.SuccessorNodes(node))
                    {
                        worklist.Push(succ);
                    }
                }
            }

            System.Console.WriteLine("Final graph:");
            _cfg.Dump();

            // Find all pfe's in graph.
            foreach (CFG.CFGVertex node in _cfg.VertexNodes)
            {
                Inst i = node._instructions[node._instructions.Count - 1];
                Mono.Cecil.Cil.OpCode op = i.OpCode;
                Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                object operand = i.Operand;
                Mono.Cecil.MethodReference call_to = operand as Mono.Cecil.MethodReference;
                if (fc == Mono.Cecil.Cil.FlowControl.Call && call_to.Name.Equals("Parallel_For_Each"))
                {
                    System.Console.WriteLine("Found PFE in block " + node.Name);
                    // Get target lambda/delegate by working from this method data flow analysis.
                    CFG.CFGVertex pfe = null;
                    pfe = node;
                    FindChains(pfe, entries);
                }
            }
        }

        void FindChains(CFG.CFGVertex pfe, List<CFG.CFGVertex> entries)
        {
            // Find all call chains to PFE.
            StackQueue<CFG.CFGVertex> p = new StackQueue<CFG.CFGVertex>();
            p.Push(pfe);
            List<StackQueue<CFG.CFGVertex>> chains_to_pfe = FollowPredecessors(pfe, p);
            List<StackQueue<CFG.CFGVertex>> chains_from_pfe = new List<StackQueue<CFG.CFGVertex>>();

            // Find all chains from PFE lambda.
            ValueBase the_lambda_value = pfe.StatePreLastInstruction._stack.PeekTop();
            CFG.CFGVertex f = null;
            if (the_lambda_value.GetType().FullName.Contains("Mono.Cecil.MethodReference"))
            {
                RValue<Tuple<object, RValue<Mono.Cecil.MethodReference>>> rv = (RValue<Tuple<object, RValue<Mono.Cecil.MethodReference>>>)the_lambda_value;
                Tuple<object, RValue<Mono.Cecil.MethodReference>> t = (Tuple<object, RValue<Mono.Cecil.MethodReference>>)rv.Val;
                RValue<Mono.Cecil.MethodReference> rv2 = t.Item2;
                Mono.Cecil.MethodReference mr = rv2.Val;
                // Find entry for mr.
                foreach (CFG.CFGVertex v in entries)
                {
                    if (mr == v.Method)
                    {
                        f = v;
                        break;
                    }
                }
            }
            if (f != null)
            {
                StackQueue<CFG.CFGVertex> s = new StackQueue<CFG.CFGVertex>();
                s.Push(f);
                chains_from_pfe = FollowSuccessors(f, s);
            }

            System.Console.WriteLine("Chains in");
            foreach (StackQueue<CFG.CFGVertex> chain in chains_to_pfe)
            {
                bool first = true;
                foreach (CFG.CFGVertex v in chain)
                {
                    if (!first)
                        System.Console.Write(" -> ");
                    System.Console.Write(v);
                    first = false;
                }
                System.Console.WriteLine();
            }

            System.Console.WriteLine("Chains out from " + f);
            foreach (StackQueue<CFG.CFGVertex> chain in chains_from_pfe)
            {
                bool first = true;
                for (int i = 0; i < chain.Size(); ++i)
                {
                    CFG.CFGVertex v = chain[i];
                    if (!first)
                        System.Console.Write(" -> ");
                    System.Console.Write(v);
                    first = false;
                }
                System.Console.WriteLine();
            }

            PFE_Point pfe_point = new PFE_Point();
            pfe_point._parallel_for_each_block = pfe;
            pfe_point._chains_to_pfe = chains_to_pfe;
            pfe_point._chains_from_pfe = chains_from_pfe;
            this._parallel_for_each_list.Add(pfe_point);
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
