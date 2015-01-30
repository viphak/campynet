using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.PE;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using System.IO;
using System.Diagnostics;
using SR = System.Reflection;
using Campy.Utils;
using NewGraphs;
using GraphAlgorithms;

namespace Campy
{
    class Converter
    {
        String eol = "\r\n";
        Assembly _assembly;
        Dictionary<System.Object, bool> compiled_targets = new Dictionary<object, bool>();
        Dictionary<String, MulticastDelegate> multicastdelegates = new Dictionary<string, MulticastDelegate>();

        public Mono.Cecil.ModuleDefinition GetMonoCecilModuleDefinition(System.Delegate del)
        {
            SR.MethodInfo mi = del.Method;

            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = mi.DeclaringType.Assembly.Location;

            // Decompile entire module.
            ModuleDefinition md = ModuleDefinition.ReadModule(kernel_assembly_file_name);
            return md;
        }

        public Mono.Cecil.MethodDefinition ConvertToMonoCecilType(System.Reflection.MethodInfo mi)
        {
            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = mi.DeclaringType.Assembly.Location;

            // Get directory containing the assembly.
            String full_path = Path.GetFullPath(kernel_assembly_file_name);
            full_path = Path.GetDirectoryName(full_path);

            String x1 = mi.Name;
            String x2 = mi.ReflectedType.Name;
            String x3 = mi.ReflectedType.FullName;
            String x4 = Utility.GetFriendlyTypeName(mi.ReflectedType);

            // Get full name of kernel, including normalization because they cannot be compared directly with Mono.Cecil names.
            String kernel_full_name = string.Format("{0} {1}.{2}({3})", mi.ReturnType.FullName, Utility.RemoveGenericParameters(mi.ReflectedType), mi.Name, string.Join(",", mi.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
            kernel_full_name = Utility.NormalizeSystemReflectionName(kernel_full_name);

            // Decompile entire module.
            ModuleDefinition md = ModuleDefinition.ReadModule(kernel_assembly_file_name);

            // Examine all types, and all methods of types in order to find the lambda in Mono.Cecil.
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
            Mono.Cecil.MethodDefinition lambda_method = null;
            foreach (TypeDefinition td in type_definitions_closure)
            {
                foreach (MethodDefinition md2 in td.Methods)
                {
                    String md2_name = Utility.NormalizeMonoCecilName(md2.FullName);
                    if (md2_name.Contains(kernel_full_name))
                        lambda_method = md2;
                }
            }

            return lambda_method;
        }

        String EmitManagedStruct(Structure structure)
        {
            String result = "";
            result += "ref struct " + structure.Name + eol;
            result += "{" + eol;
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure.target_value);
                String na = fi.Name;
                String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                if (Utility.IsSimpleCampyType(fi.FieldType))
                {
                    result += tys + "^ " + na + ";" + eol;
                }
                else
                {
                    // If it isn't a delegate, or Campy type, then
                    // it's a class.
                    na = Utility.NormalizeSystemReflectionName(na);
                    tys = Utility.NormalizeSystemReflectionName(tys);
                    result += tys + " " + na + ";" + eol;
                }
            }
            result += eol;
            // Add in other structures.
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitManagedStruct(child);
            }
            // Add in function declarations.
            // Actually, for this function, don't emit functions.
            foreach (Tuple<String, SR.MethodInfo> pair in structure.methods)
            {
            }
            result += "} " + structure.Name + ";" + eol;
            return result;
        }

        String EmitAssignmentFromManagedToUnmanagedStruct(Structure structure)
        {
            String result = "";
            result += "{" + eol;
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure.target_value);
                String na = fi.Name;
                String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                String prefix = structure.FullName + ".";
                if (Utility.IsSimpleCampyType(fi.FieldType))
                {
                    result += "(void*)" + prefix + na + "->native()," + eol;
                }
                else
                {
                    // If it isn't a delegate, or Campy type, then
                    // it's a class.
                    na = Utility.NormalizeSystemReflectionName(na);
                    tys = Utility.NormalizeSystemReflectionName(tys);
                    result += prefix + na + "," + eol;
                }
            }
            result += eol;
            // Add in other structures.
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitAssignmentFromManagedToUnmanagedStruct(child);
            }
            // Add in function declarations.
            // Actually, for this function, don't emit functions.
            foreach (Tuple<String, SR.MethodInfo> pair in structure.methods)
            {
            }
            result += "}" + eol;
            return result;
        }

        void GenerateManagedCode(
            System.Delegate del,
            Structure structure,
            String kernel_full_name,
            ModuleDefinition mod_def,
            TypeDefinition td,
            String managed_cpp_file_name,
            String managed_h_file_name,
            String unmanaged_h_file_name)
        {
            String result = "";

            // Get assembly qualifed name.
            String file_name_stem = mod_def.FullyQualifiedName;
            if (Path.HasExtension(file_name_stem) && Utility.IsRecognizedExtension(Path.GetExtension(file_name_stem)))
            {
                String ext = Path.GetExtension(file_name_stem);
                file_name_stem = file_name_stem.Replace(ext, "");
            }

            // Generate CPP code for managed class.
            result += "/* This file, " + managed_cpp_file_name + ", is automatically generated" + eol;
            result += " * via Campy.NET, " + FileVersionInfo.GetVersionInfo(this.GetType().Assembly.Location).FileVersion + "." + eol;
            result += " * The user's assembly, which contains Campy.NET calls, is located at " + eol;
            result += " * " + mod_def.FullyQualifiedName + eol;
            result += " */" + eol;
            result += "#include \"" + unmanaged_h_file_name.Replace("\\", "\\\\") + "\"" + eol;
            result += "#using \"Campy.Types.dll\"" + eol;
            result += "#using \"e:\\Personal\\Work\\Graph\\GraphClassStructures\\NewGraphs\\bin\\Debug\\Newgraphs.dll\"" + eol;
            result += eol;
            result += "using namespace System;" + eol;
            result += "using namespace Campy::Types;" + eol;
            result += "using namespace NewGraphs;" + eol;
            result += eol + eol;
            result += "public ref class " + kernel_full_name + "_managed" + eol;
            result += "{" + eol;

            // Create class member fields to retain the
            // graph of target objects.
            result += "public:" + eol;
            result += "GraphAdjList<Object^>^ graph;" + eol;
            result += "System::Delegate^ del;" + eol;
            result += "Accelerator_View^ accelerator_view;" + eol;
            result += "Extent^ extent;" + eol;
            result += eol;

            result += EmitManagedStruct(structure);

            object ob = del;
            String method_name = (ob as System.Delegate).Method.Name;
            method_name = Utility.NormalizeMonoCecilName(method_name);
            result += "// primary delegate entry point" + eol;
            result += "void " + method_name + "()" + eol;
            result += "{" + eol;
            result += "// Create unmanaged object." + eol;
            result += kernel_full_name + "_unmanaged * unm = new "
                    + kernel_full_name + "_unmanaged" + "();" + eol + eol;
            result += "// Copy data from managed class object into unmanaged class object." + eol;

            result += "unm->s1 = " + eol;
            result += EmitAssignmentFromManagedToUnmanagedStruct(structure);
            result += ";" + eol;

            result += eol;
            result += "unm->native_accelerator_view = accelerator_view->native();" + eol;
            result += "unm->native_extent = extent->native();" + eol;
            //if ("System.Void" != method_definition.MethodReturnType.ReturnType.FullName)
            //    result += "return ";
            result += "unm->" + method_name + "();" + eol;
            result += "}" + eol;
            result += "};" + eol;
            _assembly.managed_cpp_files.Add(managed_cpp_file_name, result);
        }

        String EmitUnmanagedStruct(Structure structure)
        {
            String result = "";
            result += "struct " + structure.Name + eol;
            result += "{" + eol;
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure.target_value);
                String na = fi.Name;
                String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                if (Utility.IsSimpleCampyType(fi.FieldType))
                {
                    result += "void * n_" + na + ";" + eol;
                }
                else
                {
                    na = Utility.NormalizeSystemReflectionName(na);
                    tys = Utility.NormalizeSystemReflectionName(tys);
                    result += tys + " " + na + ";" + eol;
                }
            }
            result += eol;
            // Add in other structures.
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitUnmanagedStruct(child);
            }
            // Add in function declarations.
            // Actually, for this function, don't emit functions.
            foreach (Tuple<String, SR.MethodInfo> pair in structure.methods)
            {
            }
            result += "} " + structure.Name + ";" + eol;
            return result;
        }

        String EmitAssignmentUnmanagedStruct1(Structure structure, ModuleDefinition mod_def)
        {
            String result = "";
            result += "struct " + structure.Name.Replace("s", "a") + eol;
            result += "{" + eol;
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure.target_value);
                String na = fi.Name;
                String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                if (Utility.IsCampyArrayViewType(fi.FieldType))
                {
                    result += "array_view<int, 1> "
                        + na + ";" + eol;
                }
                else if (Utility.IsCampyAcceleratorType(fi.FieldType))
                {
                    result += "accelerator "
                        + na + ";" + eol;
                }
                else if (Utility.IsCampyAcceleratorViewType(fi.FieldType))
                {
                    result += "accelerator_view "
                        + na + ";" + eol;
                }
                else if (Utility.IsCampyIndexType(fi.FieldType))
                {
                    result += "index<1> "
                        + na + ";" + eol;
                }
                else if (Utility.IsCampyExtentType(fi.FieldType))
                {
                    result += "extent<1> "
                        + na + ";" + eol;
                }
                else
                {
                    na = Utility.NormalizeSystemReflectionName(na);
                    tys = Utility.NormalizeSystemReflectionName(tys);
                    result += tys + " " + na + ";" + eol;
                }
            }
            result += eol;
            // Add in other structures.
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitAssignmentUnmanagedStruct1(child, mod_def);
            }
            // Add in function declarations.
            foreach (Tuple<String, SR.MethodInfo> pair in structure.methods)
            {
                String na = pair.Item1;
                SR.MethodInfo dd = pair.Item2;
                String tys = Utility.GetFriendlyTypeName(dd.ReturnType);
                tys = Utility.NormalizeSystemReflectionName(tys);
                result += tys + " " + na;
                // Find method of delegate.
                MethodDefinition md = ConvertToMonoCecilType(dd);
                {
                    Campy.TreeWalker.MethodParametersAstBuilder astBuilder = new Campy.TreeWalker.MethodParametersAstBuilder(
                        new ICSharpCode.Decompiler.DecompilerContext(
                            mod_def) { CurrentType = md.DeclaringType });
                    astBuilder.AddMethod(md);
                    StringWriter output = new StringWriter();
                    astBuilder.GenerateCode(new PlainTextOutput(output));
                    String field_result = output.ToString();
                    result += field_result;
                    output.Dispose();
                }
                result += " const restrict(amp) ";
                {
                    Campy.TreeWalker.MethodBodyAstBuilder astBuilder = new Campy.TreeWalker.MethodBodyAstBuilder(
                        new ICSharpCode.Decompiler.DecompilerContext(
                            mod_def) { CurrentType = md.DeclaringType });
                    astBuilder.AddMethod(md);
                    StringWriter output = new StringWriter();
                    astBuilder.GenerateCode(new PlainTextOutput(output));
                    String field_result = output.ToString();
                    field_result = field_result.Replace("this.", "");
                    result += field_result;
                    output.Dispose();
                }
                result += ";" + eol;
                result += eol;
            }
            result += "} " + structure.Name.Replace("s", "a");
            if (structure.level > 1)
                result += ";" + eol;
            else
                result += eol;
            return result;
        }

        String EmitAssignmentUnmanagedStruct2(Structure structure, ModuleDefinition mod_def)
        {
            String result = "";
            result += "{" + eol;
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure.target_value);
                String na = fi.Name;
                String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                String prefix = structure.FullName + ".";
                if (Utility.IsCampyArrayViewType(fi.FieldType))
                {
                    result += "*(array_view<int, 1>*)"
                        + "(((Campy::Types::Native_Array_View<int, 1> *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (Utility.IsCampyAcceleratorType(fi.FieldType))
                {
                    result += "*(accelerator*)"
                        + "(((Campy::Types::Native_Accelerator *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (Utility.IsCampyAcceleratorViewType(fi.FieldType))
                {
                    result += "*(accelerator_view*)"
                        + "(((Campy::Types::Native_Accelerator_View *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (Utility.IsCampyIndexType(fi.FieldType))
                {
                    result += "*(index<1>*)"
                        + "(((Campy::Types::Native_Index<1> *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (Utility.IsCampyExtentType(fi.FieldType))
                {
                    result += "*(extent<1>*)"
                        + "(((Campy::Types::Native_Extent<1> *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else
                {
                    na = Utility.NormalizeSystemReflectionName(na);
                    tys = Utility.NormalizeSystemReflectionName(tys);
                    result += prefix + na + "," + eol;
                }
            }
            result += eol;
            // Add in other structures.
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitAssignmentUnmanagedStruct2(child, mod_def);
            }
            // Add in function declarations.
            // Actually, for this function, don't emit functions.
            foreach (Tuple<String, SR.MethodInfo> pair in structure.methods)
            {
            }
            result += "}";
            if (structure.level > 1)
                result += "," + eol;
            else
                result += eol;
            return result;
        }


        void GenerateUnmanagedCode(
            System.Delegate del,
            Structure structure,
            String kernel_full_name,
            ModuleDefinition mod_def, TypeDefinition td,
            String unmanaged_cpp_file_name, String unmanaged_h_file_name)
        {
            String result = "";

            {
                // Set up class in header.
                result += "class "
                     + kernel_full_name + "_unmanaged" + eol;
                result += "{" + eol;
                result += "public:" + eol;

                result += EmitUnmanagedStruct(structure);
                result += eol;

                result += "public: void * native_accelerator_view;" + eol;
                result += "public: void * native_extent;" + eol;
                // Output primary delegate method.
                object ob = del;
                String method_name = (ob as System.Delegate).Method.Name;
                method_name = Utility.NormalizeMonoCecilName(method_name);
                result += "// primary delegate entry point" + eol;
                result += "public: void " + method_name + "();" + eol;
                result += "};" + eol;
                _assembly.unmanaged_h_files.Add(unmanaged_h_file_name, result);
            }

            {
                String method_name = (del as System.Delegate).Method.Name;
                method_name = Utility.NormalizeMonoCecilName(method_name);
                object ob = del;

                // Set up unmanaged cpp file.
                result = "";
                result += "#include <amp.h>" + eol;
                result += "#include \"" + unmanaged_h_file_name.Replace("\\", "\\\\") + "\"" + eol;
                result += "#include \"Native_Array_View.h\"" + eol;
                result += "#include \"Native_Extent.h\"" + eol;
                result += "#include \"Native_Accelerator_View.h\"" + eol;
                result += "using namespace concurrency;" + eol + eol;

                // Output entry point of unmanaged delegate.
                result += "void " + kernel_full_name + "_unmanaged::" + method_name + "()" + eol;
                result += "{" + eol;

                result += EmitAssignmentUnmanagedStruct1(structure, mod_def);

                result += "=" + eol;

                result += EmitAssignmentUnmanagedStruct2(structure, mod_def);

                result += ";" + eol;

                result += "extent<1>& _extent"
                    + " = *(extent<1>*)"
                    + "(((Campy::Types::Native_Extent<1> *) native_extent)->native)"
                    + ";" + eol;

                result += "accelerator_view& _accelerator_view"
                    + " = *(accelerator_view*)"
                    + "(((Campy::Types::Native_Accelerator_View *) native_accelerator_view)->native)"
                    + ";" + eol;

                result += eol;
                result += "parallel_for_each(_extent, [=]";
                object ob2 = del;
                MethodDefinition main_md = ConvertToMonoCecilType(del.Method);
                {
                    Campy.TreeWalker.MethodParametersAstBuilder astBuilder = new Campy.TreeWalker.MethodParametersAstBuilder(
                        new ICSharpCode.Decompiler.DecompilerContext(
                            mod_def) { CurrentType = td });
                    astBuilder.AddMethod(main_md);
                    StringWriter output = new StringWriter();
                    astBuilder.GenerateCode(new PlainTextOutput(output));
                    String xxx = output.ToString();
                    xxx = xxx.Replace("Index", "index<1>");
                    result += xxx;
                    output.Dispose();
                }
                result += " restrict(amp)";
                {
                    Campy.TreeWalker.MethodBodyAstBuilder astBuilder = new Campy.TreeWalker.MethodBodyAstBuilder(
                        new ICSharpCode.Decompiler.DecompilerContext(
                            mod_def) { CurrentType = td });
                    astBuilder.AddMethod(main_md);
                    StringWriter output = new StringWriter();
                    astBuilder.GenerateCode(new PlainTextOutput(output));
                    String xxx = output.ToString();

                    // Here's where magic happens. Delegate calls set up the
                    // environment so that the function called is correct.
                    // To do that, "this.function_call(...)" needs to be
                    // replaced with the correct location of the function,
                    // not "this." but off of the a1 nested struct. Search
                    // through the entire nested structures for the function
                    // with the correct method.
                    // Get target of delegate
                    object tar = del.Target;
                    // Get all methods of target.
                    foreach (SR.FieldInfo fi in tar.GetType().GetFields())
                    {
                        object v = fi.GetValue(tar);
                        Delegate d = v as Delegate;
                        if (d == null) continue;
                        String true_method_name = FindMethodName(d.Method, structure);
                        if (true_method_name != null)
                        {
                            String prefix = FindMethodPrefix(d.Method, structure);
                            prefix = prefix.Replace("s", "a");
                            String find = "this." + true_method_name;
                            // Find method name in nested structure.
                            String repl = prefix + "." + true_method_name;
                            // Replace!
                            xxx = xxx.Replace(find, repl);
                        }
                    }

                    // Sometimes data is contained in a class. That
                    // information needs to be passed to the delegate.
                    // Substitute references for class structures.
                    foreach (Structure child in structure.AllChildren)
                    {
                        String prefix = structure.FullName;
                        foreach (String rewrite in child.rewrite_names)
                        {
                            String find = "this." + rewrite + ".";
                            String repl = child.FullName.Replace("s", "a") +".";
                            xxx = xxx.Replace(find, repl);
                        }
                    }

                    // All remaining "this." assume at top level.
                    xxx = xxx.Replace("this.", "a1.");
                    result += xxx;
                    output.Dispose();
                }
                result += ");" + eol;
                result += "}" + eol;
                _assembly.unmanaged_cpp_files.Add(unmanaged_cpp_file_name, result);
            }
        }

        String FindMethodName(SR.MethodInfo mi, Structure structure)
        {
            String result = null;
            foreach (Tuple<String, SR.MethodInfo> tuple in structure.methods)
            {
                if (mi == tuple.Item2)
                    return tuple.Item1;
            }
            foreach (Structure child in structure.nested_structures)
            {
                String child_result = FindMethodName(mi, child);
                if (child_result != null)
                    return child_result;
            }
            return result;
        }

        String FindMethodPrefix(SR.MethodInfo mi, Structure structure)
        {
            String result = null;
            foreach (Tuple<String, SR.MethodInfo> tuple in structure.methods)
            {
                if (mi == tuple.Item2)
                    return structure.FullName;
            }
            foreach (Structure child in structure.nested_structures)
            {
                String child_result = FindMethodPrefix(mi, child);
                if (child_result != null)
                    return child_result;
            }
            return result;
        }

        public Converter(Assembly assembly)
        {
            // Save task list.
            _assembly = assembly;
        }



        public void Convert(System.Delegate del)
        {
            Structure structure = Analysis.FindAllTargets(del);

            // Create a class in C++ CLI which contains the top-level
            // delegate method. This method will need to take the entire
            // closure of list_of_targets, inline the chain of method calls.
            // All data in each target will be enclosed within a struct
            // within the class in order to keep each nice and tidy.
            // The name of the class will be the name of the top-level
            // delegate.

            // Convert multidelegate type to Mono.Cecil type, required to convert to C++ AMP.
            MethodDefinition xxxxx = ConvertToMonoCecilType(del.Method);
            TypeDefinition multidelegate_mc = xxxxx.DeclaringType;
            ModuleDefinition mod_def = multidelegate_mc.Module;

            // Derive name of output files based on the name of the full name.
            // Get full name of kernel, including normalization because they cannot be compared directly with Mono.Cecil names.
            String kernel_full_name = string.Format("{0} {1}.{2}({3})", del.Method.ReturnType.FullName, Utility.RemoveGenericParameters(del.Method.ReflectedType), del.Method.Name, string.Join(",", del.Method.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
            kernel_full_name = Utility.NormalizeSystemReflectionName(kernel_full_name);
            String file_name_stem = kernel_full_name;
            String managed_cpp_file_name = file_name_stem + "_managed.cpp";
            String managed_h_file_name = file_name_stem + "_managed.cpp";
            String unmanaged_cpp_file_name = file_name_stem + "_unmanaged.cpp";
            String unmanaged_h_file_name = file_name_stem + "_unmanaged.h";

            // Generate managed code files.
            GenerateManagedCode(
                del, structure,
                kernel_full_name,
                mod_def, multidelegate_mc,
                managed_cpp_file_name, managed_h_file_name, unmanaged_h_file_name);

            // Generate unmanaced code files.
            GenerateUnmanagedCode(
                del, structure,
                kernel_full_name,
                mod_def, multidelegate_mc,
                unmanaged_cpp_file_name, unmanaged_h_file_name);
        }
    }
}
