using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Campy.Utils;
using Campy.Types;
using Campy.Graphs;

namespace Campy
{
    /// <summary>
    /// C++ AMP cannot represent pointers, but can represent a structure containing nested
    /// C++ structs. "Structure" is an intermediate representation in the form of a tree
    /// which the GPU uses. However, C++ AMP does not allow any value in the struct to be
    /// altered.
    /// </summary>
    class Structure
    {
        // A class instance in the call chain.
        public object _class_instance;

        // The main method, if any.
        public MethodInfo _main_method;

        // A list of fields in the class instance.
        List<FieldInfo> _simple_fields = new List<FieldInfo>();

        // A list of method in the class instance.
        List<Tuple<String, MethodInfo>> _methods = new List<Tuple<string, MethodInfo>>();

        // A list of next instances in the call chain.
        List<Structure> _nested_structures = new List<Structure>();

        public List<Structure> nested_structures { get { return _nested_structures; } }
        public List<Tuple<String, MethodInfo>> methods { get { return _methods; } }
        public String Name { get; set; }
        public String FullName { get; set; }
        public int level { get; set; }
        public List<String> rewrite_names = new List<string>();
        public List<FieldInfo> simple_fields { get { return _simple_fields; } }

        private Structure(Structure parent, object target)
        {
            parent._nested_structures.Add(this);
            this._class_instance = target;
        }

        class StructureEnumerator : IEnumerable<Structure>
        {
            Structure top_level_structure;

            public StructureEnumerator(Structure vs)
            {
                top_level_structure = vs;
            }

            public IEnumerator<Structure> GetEnumerator()
            {
                StackQueue<Structure> stack = new StackQueue<Structure>();
                stack.Push(top_level_structure);
                while (stack.Count > 0)
                {
                    Structure current = stack.Pop();
                    yield return current;
                    foreach (Structure child in current._nested_structures)
                        stack.Push(child);
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEnumerable<Structure> AllChildren
        {
            get
            {
                return new StructureEnumerator(this);
            }
        }

        public Structure()
        {
        }

        // Create an intermediate representation of data_graph and control_flow_grpah
        // that contains the nodes in the graphs as Structures, and edges between
        // nodes represented by nesting of Structures. This representation is
        // needed for translation to C++ AMP.
        public void Initialize(GraphAdjList<object> data_graph, CFG control_flow_graph)
        {
            List<Tuple<object, String>> target_field_name_map = new List<Tuple<object, string>>();

            object del = data_graph.Vertices.First();
            System.Delegate start_delegate = del as System.Delegate;
            this._class_instance = start_delegate.Target;
            this._main_method = start_delegate.Method;

            int last_depth = 1;
            String last_prefix = "";

            this.Name = "s" + last_depth;
            this.FullName = last_prefix + this.Name;
            this.level = last_depth;

            List<object> targets = new List<object>();
            StackQueue<String> stack_of_prefix = new StackQueue<string>();
            StackQueue<object> stack_of_nodes = new StackQueue<object>();
            StackQueue<Structure> stack_of_structures = new StackQueue<Structure>();
            stack_of_prefix.Push(last_prefix);
            stack_of_nodes.Push(del);
            Structure top_level_structure = this;
            stack_of_structures.Push(top_level_structure);

            Dictionary<object, List<Tuple<Delegate, String>>> target_to_delegate_fieldname = new Dictionary<object, List<Tuple<Delegate, string>>>();
            Dictionary<object, Structure> map_target_to_structure = new Dictionary<object, Structure>();

            Dictionary<object, int> depth = new Dictionary<object, int>();
            depth.Add(del, last_depth);
            while (stack_of_nodes.Count > 0)
            {
                object current_node = stack_of_nodes.Pop();
                String current_prefix = stack_of_prefix.Pop();
                int current_depth = depth[current_node];
                if (targets.Contains(current_node))
                    continue;
                targets.Add(current_node);

                if (current_node as System.Delegate != null)
                {
                    System.Delegate d = current_node as System.Delegate;
                    if (d.Target != null)
                    {
                        object next = d.Target;
                        if (targets.Contains(next))
                            continue;
                        stack_of_nodes.Push(next);
                        depth.Add(next, current_depth);
                        stack_of_prefix.Push(current_prefix);
                        continue;
                    }
                }

                object target = current_node;
//                result += "// Target " + Utility.GetFriendlyTypeName(target.GetType()) + eol;
                foreach (object next in data_graph.Successors(current_node))
                {
                    stack_of_nodes.Push(next);
                    depth.Add(next, current_depth + 1);
                    stack_of_prefix.Push(current_prefix + "s" + current_depth + ".");
                }
                if (current_depth > last_depth)
                {
                    Structure p = stack_of_structures.PeekTop();
                    stack_of_structures.Push(new Structure(p, target));
                    Structure c = stack_of_structures.PeekTop();
                    c.Name = "s" + current_depth;
                    c.FullName = current_prefix + c.Name;
                    c.level = current_depth;
                }
                else if (current_depth < last_depth)
                {
                    stack_of_structures.Pop();
                }
                Structure current_structure = stack_of_structures.PeekTop();
                last_depth = current_depth;
                last_prefix = current_prefix;
                map_target_to_structure.Add(target, current_structure);
                Type target_type = target.GetType();
                foreach (FieldInfo fi in target_type.GetFields())
                {
                    // Add field if it's a simple value type or campy type.
                    // If it's a class, we'll be converting it into a struct.
                    object field_value = fi.GetValue(target);
                    if (field_value as System.Delegate == null
                        && (fi.FieldType.IsValueType ||
                            TypesUtility.IsSimpleCampyType(fi.FieldType)))
                        current_structure.AddField(fi);
                    else if (field_value != null && field_value as System.Delegate == null)
                    {
                        // It's a class. Note rewrite here.
                        target_field_name_map.Add(new Tuple<object, String>(field_value, fi.Name));
                    }

                    Type field_type = fi.FieldType;
                    if (field_value != null && TypesUtility.IsBaseType(field_type, typeof(Delegate)))
                    {
                        Delegate d = field_value as Delegate;
                        String na = fi.Name;
                        object true_target = d.Target;
                        if (true_target != null)
                        {
                            List<Tuple<Delegate, String>> dele;
                            if (target_to_delegate_fieldname.TryGetValue(true_target, out dele))
                                dele.Add(new Tuple<Delegate, String>(d, na));
                            else
                                target_to_delegate_fieldname.Add(true_target, new List<Tuple<Delegate, String>>() { new Tuple<Delegate, String>(d, na) });
                        }
                        else
                        {
                            // stick method on current target.
                            List<Tuple<Delegate, String>> dele;
                            if (target_to_delegate_fieldname.TryGetValue(target, out dele))
                                dele.Add(new Tuple<Delegate, String>(d, na));
                            else
                                target_to_delegate_fieldname.Add(target, new List<Tuple<Delegate, String>>() { new Tuple<Delegate, String>(d, na) });
                        }
                    }
                }
            }

            foreach (KeyValuePair<object, List<Tuple<Delegate, String>>> pair in target_to_delegate_fieldname)
            {
                Structure current_structure = null;
                map_target_to_structure.TryGetValue(pair.Key, out current_structure);
                foreach (Tuple<Delegate, String> tuple in pair.Value)
                {
                    Delegate dd = tuple.Item1;
                    String na = tuple.Item2;
                    current_structure.AddMethod(dd.Method, na);
                }
            }

            foreach (object node in data_graph.Vertices)
            {
                Structure current_structure = null;
                map_target_to_structure.TryGetValue(node, out current_structure);
                if (current_structure != null)
                {
                    foreach (Tuple<object, string> pair in target_field_name_map)
                    {
                        if (pair.Item1 == node)
                            current_structure.rewrite_names.Add(pair.Item2);
                    }
                }
            }

            // Add methods from control flow graph.
            foreach (CFG.CFGVertex node in control_flow_graph.VertexNodes)
            {
                if (node.IsEntry)
                {
                    // Scan structure and see if the instance contains the method.
                    foreach (KeyValuePair<object, Structure> pair in map_target_to_structure)
                    {
                        object o = pair.Key;
                        Structure s = pair.Value;
                        Type t = o.GetType();
                        Mono.Cecil.TypeDefinition td = node.Method.DeclaringType;
                        Type tdt = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionType(td);
                        if (tdt == t)
                        {
                            // Add method to structure if this method isn't the top level method.
                            MethodInfo mi = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionMethodInfo(node.Method);
                            if (mi != _main_method)
                            {
                                s.AddMethod(mi, mi.Name);
                            }
                            // Get calls.
                            foreach (CFG.CFGVertex c in control_flow_graph.AllInterproceduralCalls(node))
                            {
                                MethodInfo mic = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionMethodInfo(c.Method);
                                s.AddMethod(mic, mic.Name);
                            }
                        }
                    }
                }
            }

        }
        
        public void AddField(FieldInfo field)
        {
            _simple_fields.Add(field);
        }

        public void AddMethod(MethodInfo method, String true_name)
        {
            Tuple<String, MethodInfo> p = new Tuple<String, MethodInfo>(true_name, method);
            _methods.Add(p);
        }

        public void Dump()
        {
            StackQueue<Structure> stack = new StackQueue<Structure>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                Structure structure = stack.Pop();
                System.Console.WriteLine("Struct");
                System.Console.WriteLine("Name " + structure.Name);
                System.Console.WriteLine("Fullname " + structure.FullName);
                System.Console.WriteLine("Rewrite Names " + rewrite_names);
                System.Console.WriteLine("level " + structure.level);
                System.Console.WriteLine("instance of " + structure._class_instance);
                System.Console.WriteLine("Fields:");
                foreach (FieldInfo fi in structure._simple_fields)
                {
                    System.Console.WriteLine(fi);
                }
                foreach (Tuple<String, MethodInfo> pair in structure._methods)
                {
                    System.Console.WriteLine(pair.Item1 + " " + pair.Item2);
                }
                foreach (Structure child in structure._nested_structures)
                {
                    stack.Push(child);
                }
            }
        }
    }

    class Analysis
    {
        public static Structure FindAllTargets(object obj)
        {
            // Construct control flow graph from the method of the delegate.
            CFG control_flow_graph = CFG.Singleton();
            Delegate dddd = (Delegate)obj;
            control_flow_graph.Add(Campy.Types.Utils.ReflectionCecilInterop.ConvertToMonoCecilMethodDefinition(dddd.Method));
            control_flow_graph.ExtractBasicBlocks();

            // Construct graph containing delegates used in program.
            StackQueue<object> stack = new StackQueue<object>();
            stack.Push(obj);
            Campy.Graphs.GraphLinkedList<object> preliminary_data_graph = new GraphLinkedList<object>();
            while (stack.Count > 0)
            {
                object node = stack.Pop();
                if (preliminary_data_graph.Vertices.Contains(node))
                    continue;
                preliminary_data_graph.AddVertex(node);

                // Case 1: object is multicast delegate.
                System.MulticastDelegate md = node as System.MulticastDelegate;
                if (md != null)
                {
                    foreach (System.Delegate node2 in md.GetInvocationList())
                    {
                        stack.Push(node2);
                    }
                }

                // Case 2: object is plain delegate. Note fall through from previous case.
                System.Delegate del = node as System.Delegate;
                if (del != null)
                {
                    object target = del.Target;
                    if (target == null)
                    {
                        // If target is null, then the delegate is a function that
                        // uses either static data, or does not require any additional
                        // data.
                    }
                    else
                    {
                        stack.Push(target);
                    }
                }
                else
                {
                    // Case 3: object is a class, and potentially could point to delegate.
                    // Examine all fields, looking for list_of_targets.
                    Type target_type = node.GetType();

                    FieldInfo[] target_type_fieldinfo = target_type.GetFields();
                    foreach (var field in target_type_fieldinfo)
                    {
                        var value = field.GetValue(node);
                        if (value != null)
                        {
                            if (field.FieldType.IsValueType)
                                continue;
                            if (TypesUtility.IsCampyArrayType(field.FieldType))
                                continue;
                            if (TypesUtility.IsSimpleCampyType(field.FieldType))
                                continue;
                            // chase pointer type.
                            stack.Push(value);
                        }
                    }
                }
            }

            if (true)
            foreach (object node in preliminary_data_graph.Vertices)
            {
                System.Console.WriteLine("Node "
                    + node.GetHashCode()
                    + " "
                    + node.ToString());
                System.Console.WriteLine("Node "
                    + (((node as Delegate) != null) ? "is " : "is not ")
                    + "a delegate.");
                System.Console.WriteLine();
            }

            foreach (object node in preliminary_data_graph.Vertices)
            {
                object target = node;

                // Case 1: object is multicast delegate.
                System.MulticastDelegate md = target as System.MulticastDelegate;
                if (md != null)
                {
                    foreach (System.Delegate node2 in md.GetInvocationList())
                    {
                        if (target != node2)
                        {
                            //System.Console.WriteLine("Node "
                            //    + node.GetHashCode() + " " + node.ToString());
                            //System.Console.WriteLine("-> "
                            //        + node2.GetHashCode() + " " + node2.ToString());
                            preliminary_data_graph.AddEdge(target, node2);
                        }
                    }
                }

                // Case 2: object is plain delegate. Note fall through from previous case.
                System.Delegate del = node as System.Delegate;
                if (del != null)
                {
                    object check = del.Target;
                    if (check == null)
                    {
                        // If target is null, then the delegate is a function that
                        // uses either static data, or does not require any additional
                        // data.
                    }
                    else
                    {
                        target = check;
                        preliminary_data_graph.AddEdge(node, target);
                    }
                }
                else
                {
                    // Case 3: object is a class, and potentially could point to delegate.
                    // Examine all fields, looking for list_of_targets.
                    Type target_type = target.GetType();

                    FieldInfo[] target_type_fieldinfo = target_type.GetFields();
                    foreach (var field in target_type_fieldinfo)
                    {
                        var value = field.GetValue(target);
                        if (value != null && preliminary_data_graph.Vertices.Contains(value))
                        {
                            //System.Console.WriteLine("Node "
                            //    + node.GetHashCode() + " " + node.ToString());
                            //System.Console.WriteLine("-> "
                            //        + value.GetHashCode() + " " + value.ToString());
                            // Chase down the field.
                            preliminary_data_graph.AddEdge(target, value);
                        }
                    }
                }
            }

            if (true)
            {
                System.Console.WriteLine("Full graph of lambda closure.");
                foreach (object node in preliminary_data_graph.Vertices)
                {
                    System.Console.WriteLine("Node "
                        + node.GetHashCode()
                        + " "
                        + node.ToString());
                    foreach (object succ in preliminary_data_graph.Successors(node))
                        System.Console.WriteLine("-> "
                            + succ.GetHashCode() + " " + succ.ToString());
                }
                System.Console.WriteLine();
            }

            // Perform final depth first traversal of grpah, computing
            // new graph with those objects in chain of delegates.
            List<object> delegates = new List<object>();
            List<object> subset = new List<object>();
            foreach (object node in preliminary_data_graph.Vertices)
               // if (chained_to_delegate.Contains(node))
                    subset.Add(node);

            GraphAdjList<object> data_graph = new GraphAdjList<object>();
            data_graph.SetNameSpace(subset);
            foreach (object node in subset)
                data_graph.AddVertex(node);
            Campy.GraphAlgorithms.DepthFirstPreorderTraversal<object> dfpt = new Campy.GraphAlgorithms.DepthFirstPreorderTraversal<object>(preliminary_data_graph, new object[] { obj });
            foreach (object node in dfpt)
            {
                if (subset.Contains(node))
                {
                    IEnumerable<object> list = preliminary_data_graph.Predecessors(node);
                    if (list.Count() > 0)
                    {
                        // Assume graph is actually a tree.
                        object pred = list.First();
                        data_graph.AddEdge(pred, node);
                    }
                }
            }

            if (true)
            {
                System.Console.WriteLine("Full graph of lambda closure.");
                foreach (object node in data_graph.Vertices)
                {
                    System.Console.WriteLine("Node "
                        + node.GetHashCode()
                        + " "
                        + node.ToString());
                    foreach (object succ in data_graph.Successors(node))
                        System.Console.WriteLine("-> "
                            + succ.GetHashCode() + " " + succ.ToString());
                }
                System.Console.WriteLine();
            }

            Structure res = new Structure();
            res.Initialize(data_graph, control_flow_graph);
            res.Dump();

            return res;
        }
    }
}
