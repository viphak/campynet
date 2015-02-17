using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Mono.Cecil;
using Campy.Graphs;

namespace Campy
{

    public class CFG : GraphLinkedList<object, CFG.CFGVertex, CFG.CFGEdge>
    {
        Assembly _assembly;
        static int _node_number = 1;

        public CFG(Assembly assembly)
            : base()
        {
            _assembly = assembly;

            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = _assembly.Location;

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
                    // Each method is the start of a block.
                    CFGVertex v = (CFGVertex)this.AddVertex(_node_number++);
                    v._method = md2; 
                    foreach (Mono.Cecil.Cil.Instruction i in md2.Body.Instructions)
                    {
                        v._instructions.Add(i);
                    }
                }
            }
        }

        public class CFGVertex
            : GraphLinkedList<object, CFG.CFGVertex, CFG.CFGEdge>.Vertex
        {
            public List<Mono.Cecil.Cil.Instruction> _instructions = new List<Mono.Cecil.Cil.Instruction>();
            public Mono.Cecil.MethodDefinition _method;

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
                result._method = this._method;
                for (int j = i; j < _instructions.Count; ++j)
                    result._instructions.Add(_instructions[j]);
                for (int j = _instructions.Count - 1; j >= i; --j)
                    this._instructions.RemoveAt(j);
                Debug.Assert(this._instructions.Count != 0);
                Debug.Assert(result._instructions.Count != 0);
                cfg.AddEdge(this.Name, result.Name);
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
            foreach (CFGVertex v in VertexNodes)
            {
                System.Console.WriteLine("Node " + v.Name);
                System.Console.WriteLine("Instructions:");
                foreach (Mono.Cecil.Cil.Instruction i in v._instructions)
                    System.Console.WriteLine(i);
                System.Console.WriteLine("Edges to:");
                foreach (object t in this.Successors(v.Name))
                {
                    System.Console.WriteLine("-> " + t);
                }
                System.Console.WriteLine();
            }
        }
    }

}
