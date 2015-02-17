using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campy
{
    class CFA
    {
        CFG _cfg;

        public CFA(CFG cfg)
        {
            _cfg = cfg;
        }

        public void AnalyzeNodes()
        {
            _cfg.Dump();

            StackQueue<CFG.CFGVertex> stack = new StackQueue<CFG.CFGVertex>();

            // Push on stack nodes that changed.
            foreach (CFG.CFGVertex node in _cfg.VertexNodes)
                stack.Push(node);

            bool done = false;
            while (!done)
            {
                // Perform constant propagation in order to find delegate targets.
                // Push any changed nodes on stack.

                // Analyze blocks to determine new graph.
                while (stack.Count > 0)
                {
                    CFG.CFGVertex node = stack.Pop();
                    int count = node._instructions.Count;
                    for (int j = 0; j < count; ++j)
                    {
                        Mono.Cecil.Cil.Instruction i = node._instructions[j];
                        Mono.Cecil.Cil.OpCode op = i.OpCode;
                        Mono.Cecil.Cil.FlowControl fc = op.FlowControl;
                        if (fc == Mono.Cecil.Cil.FlowControl.Next)
                            continue;
                        if (j+1 >= count)
                            continue;
                        CFG.CFGVertex new_node = node.Split(j+1);
                        stack.Push(new_node);
                        break;
                    }
                }
                break;
            }
            _cfg.Dump();
        }
    }
}
