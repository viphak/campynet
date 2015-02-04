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
using NewGraphs;

namespace Campy
{
    class Structure
    {
        public object target_value;
        List<FieldInfo> _simple_fields = new List<FieldInfo>();
        public List<FieldInfo> simple_fields { get { return _simple_fields; } }
        List<Structure> _nested_structures = new List<Structure>();
        public List<Structure> nested_structures { get { return _nested_structures; } }
        List<Tuple<String, MethodInfo>> _methods = new List<Tuple<string,MethodInfo>>();
        public List<Tuple<String, MethodInfo>> methods { get { return _methods; } }
        public String Name { get; set; }
        public String FullName { get; set; }
        public int level { get; set; }
        public List<String> rewrite_names = new List<string>();

        public Structure(Structure parent, object target)
        {
            parent._nested_structures.Add(this);
            this.target_value = target;
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

        public Structure(GraphAdjList<object> list_of_targets)
        {
            List<Tuple<object, String>> target_field_name_map = new List<Tuple<object, string>>();
            object del = list_of_targets.Vertices.First();
            int last_depth = 1;
            String last_prefix = "";

            this.Name = "s" + last_depth;
            this.FullName = last_prefix + this.Name;
            this.level = last_depth;
            System.Delegate start = del as System.Delegate;
            this.target_value = start.Target;

            // Create a type which is a nested struct that mirrors the graph.
            // It isn't necessary, but just helps reinforces the runtime model
            // on the unmanaged side.
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
                foreach (object next in list_of_targets.Successors(current_node))
                {
                    stack_of_nodes.Push(next);
                    depth.Add(next, current_depth + 1);
                    stack_of_prefix.Push(current_prefix + "s" + current_depth + ".");
                }
                if (current_depth > last_depth)
                {
                    Structure p = stack_of_structures.Top();
                    stack_of_structures.Push(new Structure(p, target));
                    Structure c = stack_of_structures.Top();
                    c.Name = "s" + current_depth;
                    c.FullName = current_prefix + c.Name;
                    c.level = current_depth;
                }
                else if (current_depth < last_depth)
                {
                    stack_of_structures.Pop();
                }
                Structure current_structure = stack_of_structures.Top();
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

            foreach (object node in list_of_targets.Vertices)
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
    }

    class Analysis
    {
        public static Structure FindAllTargets(object obj)
        {
            // To do this properly, a complete control flow graph with constant
            // propagation would have to be contructed. With that information,
            // we could determine with reasonable certainty what targets would
            // require translation to C++ AMP. We already use ILSpy for the
            // representation of the program, or we could go back to System.Reflection.
            //
            // For now, perform a transitive closure of the fields of the delegate.
            // This does a pretty reason job. C++ AMP adds a constraint in that
            // it cannot access data outside of auto variables (variables declared
            // in the lexically enclosing block). As a result, the call to the
            // delegate must be inlined to the top level delegate.

            StackQueue<object> stack = new StackQueue<object>();
            stack.Push(obj);
            // Build a graph of the closure of obj across all fields.
            NewGraphs.GraphLinkedList<object> graph = new GraphLinkedList<object>();
            while (stack.Count > 0)
            {
                object node = stack.Pop();
                if (graph.Vertices.Contains(node))
                    continue;
                graph.AddVertex(node);

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
                else if (TypesUtility.IsCampyArrayViewType(node.GetType()))
                {
                    // recurse into type of array object.
                    Type t = node.GetType();
                    Type[] args = t.GenericTypeArguments;
                    if (args[0].IsValueType)
                        continue;
                    // get first element of array. Let this define
                    // array view element.
                    // Find _data field of Array_View.
                    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Static | BindingFlags.Instance |
                         BindingFlags.DeclaredOnly;
                    FieldInfo[] sfi = t.GetFields(flags);
                    FieldInfo sn = sfi.Where(f => "_data" == f.Name).FirstOrDefault();
                    if (sn == null)
                        throw new ArgumentException("Field not found.");
                    object obj_array = sn.GetValue(node);
                    if (obj_array == null)
                        throw new ArgumentException("Value not found.");
                    Array array = obj_array as Array;
                    object element = array.GetValue(0);
                    stack.Push(element);
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
                            // Chase down the field.
                            if (field.FieldType.IsValueType)
                                continue;
                            if (TypesUtility.IsCampyArrayViewType(field.FieldType))
                            {
                                // chase type.
                                stack.Push(value);
                                continue;
                            }
                            if (!TypesUtility.IsSimpleCampyType(field.FieldType))
                            {
                                // chase type.
                                stack.Push(value);
                                continue;
                            }
                        }
                    }
                }
            }

            if (false)
            foreach (object node in graph.Vertices)
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

            foreach (object node in graph.Vertices)
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
                            graph.AddEdge(target, node2);
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
                        graph.AddEdge(node, target);
                    }
                }
                else if (TypesUtility.IsCampyArrayViewType(node.GetType()))
                {
                    graph.AddEdge(node, target);
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
                        if (value != null && graph.Vertices.Contains(value))
                        {
                            //System.Console.WriteLine("Node "
                            //    + node.GetHashCode() + " " + node.ToString());
                            //System.Console.WriteLine("-> "
                            //        + value.GetHashCode() + " " + value.ToString());
                            // Chase down the field.
                            graph.AddEdge(target, value);
                        }
                    }
                }
            }

            if (false)
            {
                System.Console.WriteLine("Full graph of lambda closure.");
                foreach (object node in graph.Vertices)
                {
                    System.Console.WriteLine("Node "
                        + node.GetHashCode()
                        + " "
                        + node.ToString());
                    foreach (object succ in graph.Successors(node))
                        System.Console.WriteLine("-> "
                            + succ.GetHashCode() + " " + succ.ToString());
                }
                System.Console.WriteLine();
            }
            // Perform final depth first traversal of objects and create new graph with those objects in chain of delegates.
            List<object> delegates = new List<object>();
            List<object> subset = new List<object>();
            foreach (object node in graph.Vertices)
               // if (chained_to_delegate.Contains(node))
                    subset.Add(node);

            GraphAdjList<object> closure_graph = new GraphAdjList<object>();
            closure_graph.SetNameSpace(subset);
            foreach (object node in subset)
                closure_graph.AddVertex(node);
            GraphAlgorithms.DepthFirstPreorderTraversal<object> dfpt = new GraphAlgorithms.DepthFirstPreorderTraversal<object>(graph, new object[] { obj });
            foreach (object node in dfpt)
            {
                if (subset.Contains(node))
                {
                    IEnumerable<object> list = graph.Predecessors(node);
                    if (list.Count() > 0)
                    {
                        // Assume graph is actually a tree.
                        object pred = list.First();
                        closure_graph.AddEdge(pred, node);
                    }
                }
            }

            if (false)
            {
                System.Console.WriteLine("Full graph of lambda closure.");
                foreach (object node in closure_graph.Vertices)
                {
                    System.Console.WriteLine("Node "
                        + node.GetHashCode()
                        + " "
                        + node.ToString());
                    foreach (object succ in closure_graph.Successors(node))
                        System.Console.WriteLine("-> "
                            + succ.GetHashCode() + " " + succ.ToString());
                }
                System.Console.WriteLine();
            }
            Structure res = new Structure(closure_graph);

            return res;
        }
    }
}
