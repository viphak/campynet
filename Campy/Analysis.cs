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

        // A list of fields that refer to a class.
        // Here, we actually note the class instance because we're going
        // to have to convert it into a qualified struct.
        public List<Tuple<System.Reflection.FieldInfo, object>> _class_fields;

        // A list of fields that refer to a delegate.
        // Here, we actually note the delegate instance because we're going
        // to have to convert it into a qualified struct/method call.
        public List<Tuple<System.Reflection.FieldInfo, Delegate>> _delegate_fields;

        // A list of methods owned by the struct.
        List<MethodInfo> _methods = new List<MethodInfo>();

        // A list of next instances in the call chain.
        List<Structure> _nested_structures = new List<Structure>();

        public List<Structure> nested_structures { get { return _nested_structures; } }
        public List<MethodInfo> methods { get { return _methods; } }
        public String Name { get; set; }
        public String FullName { get; set; }
        public int level { get; set; }
        public List<String> rewrite_names = new List<string>();
        public List<FieldInfo> simple_fields { get { return _simple_fields; } }
        public static Dictionary<object, Structure> map_target_to_structure = new Dictionary<object, Structure>();

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
        public static Structure Initialize(Dictionary<Delegate, object> delegate_to_instance, System.Reflection.MethodInfo main_method, GraphLinkedList<object> data_graph, CFG control_flow_graph)
        {
            map_target_to_structure = new Dictionary<object, Structure>();
            Structure top_level_structure = new Structure();
            object dd = data_graph.Vertices.First();
            top_level_structure._class_instance = dd;
            top_level_structure._main_method = main_method;
            int last_depth = 1;
            String last_prefix = "";
            top_level_structure.Name = "s" + last_depth;
            top_level_structure.FullName = last_prefix + top_level_structure.Name;
            top_level_structure.level = last_depth;
            List<object> targets = new List<object>();
            StackQueue<String> stack_of_prefix = new StackQueue<string>();
            StackQueue<object> stack_of_nodes = new StackQueue<object>();
            StackQueue<Structure> stack_of_structures = new StackQueue<Structure>();
            stack_of_prefix.Push(last_prefix);
            stack_of_nodes.Push(dd);
            stack_of_structures.Push(top_level_structure);
            Dictionary<object, int> depth = new Dictionary<object, int>();
            depth.Add(dd, last_depth);
            while (stack_of_nodes.Count > 0)
            {
                object current_node = stack_of_nodes.Pop();
                String current_prefix = stack_of_prefix.Pop();
                int current_depth = depth[current_node];
                if (targets.Contains(current_node))
                    continue;

                object target = current_node;
                targets.Add(target);
                System.Console.WriteLine("Target " + Utility.GetFriendlyTypeName(target.GetType()));

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
                current_structure._delegate_fields = new List<Tuple<System.Reflection.FieldInfo, Delegate>>();
                current_structure._class_fields = new List<Tuple<System.Reflection.FieldInfo, object>>();

                last_depth = current_depth;
                last_prefix = current_prefix;
                Structure try_structure = null;
                map_target_to_structure.TryGetValue(target, out try_structure);
                if (try_structure != null)
                {
                    Debug.Assert(try_structure == current_structure);
                }
                else
                {
                    map_target_to_structure.Add(target, current_structure);
                }
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
                        current_structure._class_fields.Add(new Tuple<System.Reflection.FieldInfo, object>(fi, field_value));
                    }

                    // When field of the target/class is a delegate, the intent in the code
                    // is to call the delegate via the field. Since pointers to anything
                    // are not allowed in C++ AMP, we need to actually either rewrite the
                    // structure so that the field is a method (not preferred),
                    // rewrite the name to the true name of the method called, or
                    // rewrite the method to be the name of the field (not preferred).
                    // In any case, note the association of the field with
                    // the delegate.
                    Type field_type = fi.FieldType;
                    if (field_value != null && TypesUtility.IsBaseType(field_type, typeof(Delegate)))
                    {
                        Delegate d = field_value as Delegate;
                        current_structure._delegate_fields.Add(new Tuple<System.Reflection.FieldInfo, Delegate>(fi, d));
                    }
                }
            }

            //foreach (object node in data_graph.Vertices)
            //{
            //    Structure current_structure = null;
            //    map_target_to_structure.TryGetValue(node, out current_structure);
            //    if (current_structure != null)
            //    {
            //        foreach (Tuple<object, string> pair in _class_fields)
            //        {
            //            if (pair.Item1 == node)
            //                current_structure.rewrite_names.Add(pair.Item2);
            //        }
            //    }
            //}

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
                            // Add method to structure.
                            MethodInfo mi = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionMethodInfo(node.Method);
                            s.AddMethod(mi);
                            // Get calls.
                            foreach (CFG.CFGVertex c in control_flow_graph.AllInterproceduralCalls(node))
                            {
                                MethodInfo mic = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionMethodInfo(c.Method);
                                s.AddMethod(mic);
                            }
                        }
                    }
                }
            }

            
            // For each field that is a delegate, find the target structure corresponding to the delegate,
            // and add the delegate method to the structure.
            foreach (KeyValuePair<Delegate, object> pair in delegate_to_instance)
            {
                Delegate k = pair.Key;
                object v = pair.Value;
                Structure target_structure = null;
                map_target_to_structure.TryGetValue(v, out target_structure);
                Debug.Assert(target_structure != null);
                target_structure.AddMethod(k.Method);
            }
                
            //stack_of_structures = new StackQueue<Structure>();
            //stack_of_structures.Push(top_level_structure);
            //while (stack_of_structures.Count > 0)
            //{
            //    Structure cur_structure = stack_of_structures.Pop();
            //    foreach (Tuple<System.Reflection.FieldInfo, Delegate> tuple in cur_structure._delegate_fields)
            //    {
            //        if (tuple.Item2.Target != null)
            //        {
            //            Structure target_structure = null;
            //            map_target_to_structure.TryGetValue(tuple.Item2.Target, out target_structure);
            //            Debug.Assert(target_structure != null);
            //            target_structure.AddMethod(tuple.Item2.Method, tuple.Item2.Method.Name);
            //        }
            //        else
            //        {
            //            // Target empty. Map delegate to target.

            //        }
            //    }
            //    foreach (Structure child in cur_structure.nested_structures)
            //    {
            //        stack_of_structures.Push(child);
            //    }
            //}
            
            return top_level_structure;
        }
        
        public void AddField(FieldInfo field)
        {
            _simple_fields.Add(field);
        }

        public void AddMethod(MethodInfo method)
        {
            if (!_methods.Contains(method))
                _methods.Add(method);
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
                System.Console.WriteLine("Rewrite Names " + (rewrite_names.Count() > 0 ? rewrite_names.Aggregate((i, j) => i + " " + j) : ""));
                System.Console.WriteLine("level " + structure.level);
                System.Console.WriteLine("instance of " + structure._class_instance);
                System.Console.WriteLine("Simple Fields:");
                foreach (FieldInfo fi in structure._simple_fields)
                {
                    System.Console.WriteLine(fi);
                }
                System.Console.WriteLine("Class Fields:");
                foreach (Tuple<FieldInfo, object> pair in structure._class_fields)
                {
                    System.Console.WriteLine(pair.Item1.Name + " " + pair.Item2);
                }
                System.Console.WriteLine("Delegate Fields:");
                foreach (Tuple<FieldInfo, Delegate> pair in structure._delegate_fields)
                {
                    System.Console.WriteLine(pair.Item1.Name + " " + pair.Item2);
                }
                foreach (MethodInfo met in structure._methods)
                {
                    System.Console.WriteLine(met);
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

        public static String MyToString(object obj)
        {
            if (TypesUtility.IsBaseType(obj.GetType(), typeof(Delegate)))
            {
                String result = "";
                Delegate d = obj as Delegate;
                result += "Method " + d.Method.ToString();
                result += " Object " + d.Target;
                return result;
            }
            else
                return obj.ToString();
        }

        public static Structure FindAllTargets(object obj)
        {
            Dictionary<Delegate, object> delegate_to_instance = new Dictionary<Delegate, object>();
            // Construct control flow graph from lambda delegate method.
            CFG control_flow_graph = CFG.Singleton();
            Delegate lambda_delegate = (Delegate)obj;
            control_flow_graph.Add(Campy.Types.Utils.ReflectionCecilInterop.ConvertToMonoCecilMethodDefinition(lambda_delegate.Method));
            control_flow_graph.ExtractBasicBlocks();

            // Construct graph containing all objects used in lambda.
            StackQueue<object> stack = new StackQueue<object>();
            stack.Push(lambda_delegate);
            Campy.Graphs.GraphLinkedList<object> data_graph = new GraphLinkedList<object>();
            while (stack.Count > 0)
            {
                object node = stack.Pop();

                // Case 1: object is multicast delegate.
                System.MulticastDelegate md = node as System.MulticastDelegate;
                if (md != null)
                {
                    foreach (System.Delegate node2 in md.GetInvocationList())
                    {
                        if ((object)node2 != (object)node)
                        {
                            System.Console.WriteLine("Pushing2 " + MyToString(node2));
                            stack.Push(node2);
                        }
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
                        target = Activator.CreateInstance(del.Method.DeclaringType);
                        if (data_graph.Vertices.Contains(target))
                            continue;
                        if (!delegate_to_instance.ContainsKey(del))
                        {
                            Debug.Assert(!TypesUtility.IsBaseType(target.GetType(), typeof(Delegate)));
                            data_graph.AddVertex(target);
                            delegate_to_instance.Add(del, target);
                            stack.Push(target);
                        }
                    }
                    else
                    {
                        // Target isn't null for delegate. Most likely, the method
                        // is part of the target, so let's assert that.
                        bool found = false;
                        foreach (System.Reflection.MethodInfo mi in target.GetType().GetMethods())
                        {
                            if (mi == del.Method)
                            {
                                found = true;
                                break;
                            }
                        }
                        Debug.Assert(found);
                        System.Console.WriteLine("Pushing " + MyToString(target));
                        if (delegate_to_instance.ContainsKey(del))
                        {
                            Debug.Assert(delegate_to_instance[del] == target);
                        }
                        else
                        {
                            delegate_to_instance.Add(del, target);
                        }
                        stack.Push(target);
                    }
                    continue;
                }

                if (data_graph.Vertices.Contains(node))
                    continue;

                Debug.Assert(! TypesUtility.IsBaseType(node.GetType(), typeof(Delegate)));
                data_graph.AddVertex(node);

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
                        System.Console.WriteLine("Pushingf " + MyToString(value));
                        stack.Push(value);
                    }
                }
            }

            if (true)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Dump of nodes.");
                foreach (object node in data_graph.Vertices)
                {
                    System.Console.WriteLine("Node "
                        + MyToString(node));
                    System.Console.WriteLine("typeof node = "
                        + node.GetType());
                    System.Console.WriteLine("Node "
                        + (((node as Delegate) != null) ? "is " : "is not ")
                        + "a delegate.");
                    System.Console.WriteLine();
                }
            }

            // Add edges.
            foreach (object node in data_graph.Vertices)
            {
                Type node_type = node.GetType();

                FieldInfo[] node_type_fieldinfo = node_type.GetFields();
                foreach (var field in node_type_fieldinfo)
                {
                    if (field.FieldType.IsValueType)
                        continue;
                    if (TypesUtility.IsCampyArrayType(field.FieldType))
                        continue;
                    if (TypesUtility.IsSimpleCampyType(field.FieldType))
                        continue;
                    var value = field.GetValue(node);
                    if (value == null)
                    {
                    }
                    else if (TypesUtility.IsBaseType(value.GetType(), typeof(Delegate)))
                    {
                        Delegate del = value as Delegate;
                        object value_target = del.Target;
                        if (value_target == node)
                            ;
                        else if (value_target != null)
                        {
                            Debug.Assert(data_graph.Vertices.Contains(node));
                            Debug.Assert(data_graph.Vertices.Contains(value_target));
                            data_graph.AddEdge(node, value_target);
                        }
                        else
                        {
                            value_target = delegate_to_instance[del];
                            if (value_target != node)
                            {
                                Debug.Assert(data_graph.Vertices.Contains(node));
                                Debug.Assert(data_graph.Vertices.Contains(value_target));
                                data_graph.AddEdge(node, value_target);
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(data_graph.Vertices.Contains(node));
                        Debug.Assert(data_graph.Vertices.Contains(value));
                        data_graph.AddEdge(node, value);
                    }
                }
            }

            if (true)
            {
                System.Console.WriteLine("Full graph of lambda closure.");
                foreach (object node in data_graph.Vertices)
                {
                    System.Console.WriteLine("Node "
                        + MyToString(node));
                    System.Console.WriteLine("typeof node = "
                        + node.GetType());
                    System.Console.WriteLine("Node "
                        + (((node as Delegate) != null) ? "is " : "is not ")
                        + "a delegate.");
                    foreach (object succ in data_graph.Successors(node))
                        System.Console.WriteLine("-> "
                            + succ.GetHashCode() + " " + MyToString(succ));
                    System.Console.WriteLine();
                }
                System.Console.WriteLine();
            }

            Structure res = Structure.Initialize(delegate_to_instance, lambda_delegate.Method, data_graph, control_flow_graph);
            res.Dump();

            return res;
        }
    }
}
