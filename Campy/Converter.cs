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

namespace Campy
{
    class Converter
    {
        String eol = "\r\n";
        Assembly _assembly;
        Dictionary<System.Object, bool> compiled_targets = new Dictionary<object, bool>();
        Dictionary<String, MulticastDelegate> multicastdelegates = new Dictionary<string, MulticastDelegate>();

        public Mono.Cecil.MethodDefinition ConvertToMonoCecilType(System.Delegate del)
        {
            SR.MethodInfo mi = del.Method;

            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = mi.DeclaringType.Assembly.Location;

            // Get directory containing the assembly.
            String full_path = Path.GetFullPath(kernel_assembly_file_name);
            full_path = Path.GetDirectoryName(full_path);

            // Get full name of kernel, including normalization because they cannot be compared directly with Mono.Cecil names.
            String kernel_full_name = string.Format("{0} {1}.{2}({3})", mi.ReturnType.FullName, mi.ReflectedType.FullName, mi.Name, string.Join(",", mi.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
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

        void GenerateManagedCode(
            String kernel_full_name,
            List<System.Delegate> delegates,
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
            result += "#include \"" + unmanaged_h_file_name.Replace("\\", "\\\\") + "\"" + eol;
            result += "#using \"Campy.Types.dll\"" + eol;
            result += eol;
            result += "using namespace System;" + eol;
            result += "using namespace Campy::Types;" + eol;
            result += eol + eol;
            result += "public ref class " + kernel_full_name + "_managed" + eol;
            result += "{" + eol;

            // Loop through every delegate and output each field to capture.
            result += "// Inline delegate target field." + eol;
            List<object> targets = new List<object>();
            foreach (System.Delegate del in delegates)
            {
                if (targets.Contains(del.Target))
                    continue;
                if (del.Target != null)
                {
                    targets.Add(del.Target);
                    result += "// Delegate target " + Utility.GetFriendlyTypeName(del.Target.GetType()) + eol;
                    object del_target = del.Target;
                    Type del_target_type = del.Target.GetType();
                    foreach (SR.FieldInfo fi in del_target_type.GetFields())
                    {
                        // Here, we do not output fields which are delegates.
                        // Each delegate target is output, and the method handled
                        // as an auto in the C++ AMP code.
                        object field_value = fi.GetValue(del.Target);
                        if (field_value as System.Delegate != null)
                            continue;
                        String na = fi.Name;
                        String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                        // Also, we really can only handle Campy or value types.
                        // Check and complain.
                        if (!(field_value.GetType().IsValueType
                            || Utility.IsSimpleCampyType(field_value)))
                            throw new Exception("Can only compile value types, or Campy Types. "
                                + tys + " is not one of those.");
                        // In C++ CLI, classes are reference pointer type.
                        if (Utility.IsSimpleCampyType(field_value))
                            tys += '^';
                        result += "public: " + tys + " " + na + ";" + eol;
                    }
                }
                result += eol;
            }
            result += "public: Accelerator_View^ accelerator_view;" + eol;
            result += "public: Extent^ extent;" + eol;
            result += eol;
            String method_name = delegates.First().Method.Name;
            method_name = Utility.NormalizeMonoCecilName(method_name);
            result += "// primary delegate entry point" + eol;
            result += "void " + method_name + "()" + eol;
            result += "{" + eol;
            result += "// Create unmanaged object." + eol;
            result += kernel_full_name + "_unmanaged * unm = new "
                    + kernel_full_name + "_unmanaged" + "();" + eol + eol;
            result += "// Copy data from managed class object into unmanaged class object." + eol;
            // Loop through every delegate and output each field to capture.
            foreach (System.Delegate del in delegates)
            {
                if (del.Target != null)
                {
                    result += "// Delegate target " + Utility.GetFriendlyTypeName(del.Target.GetType()) + eol;
                    object del_target = del.Target;
                    Type del_target_type = del.Target.GetType();
                    foreach (SR.FieldInfo fi in del_target_type.GetFields())
                    {
                        // Here, we do not output fields which are delegates.
                        // Each delegate target is output, and the method handled
                        // as an auto in the C++ AMP code.
                        object field_value = fi.GetValue(del.Target);
                        if (field_value as System.Delegate != null)
                            continue;
                        String na = fi.Name;
                        String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                        // Also, we really can only handle Campy or value types.
                        // Check and complain.
                        if (!(field_value.GetType().IsValueType
                            || Utility.IsSimpleCampyType(field_value)))
                            throw new Exception("Can only compile value types, or Campy Types. "
                                + tys + " is not one of those.");
                        // In C++ CLI, classes are reference pointer type.
                        if (Utility.IsSimpleCampyType(field_value))
                            tys += '^';
                        if (fi.FieldType.Name.Contains("Array_View"))
                        {
                            result += "unm->nav_" + fi.Name + " = (void *)" + fi.Name + "->nav();" + eol;
                        }
                        else
                        {
                            result += "unm->" + fi.Name + " = " + fi.Name + ";" + eol;
                        }
                    }
                }
            }
            result += eol;
            result += "unm->native_accelerator_view = accelerator_view->nav();" + eol;
            result += "unm->native_extent = extent->ne();" + eol;
            //if ("System.Void" != method_definition.MethodReturnType.ReturnType.FullName)
            //    result += "return ";
            result += "unm->" + method_name + "();" + eol;
            result += "}" + eol;
            result += "};" + eol;
            _assembly.managed_cpp_files.Add(managed_cpp_file_name, result);
        }

        void GenerateUnmanagedCode(
            String kernel_full_name,
            List<System.Delegate> delegates,
            ModuleDefinition mod_def, TypeDefinition td,
            String unmanaged_cpp_file_name, String unmanaged_h_file_name)
        {
            String result = "";

            // Set up class in header.
            result += "class "
                 + kernel_full_name + "_unmanaged" + eol;
            result += "{" + eol;
            // Loop through every targets of every delegate, and output each field to capture.
            {
                List<object> targets = new List<object>();
                result += "// Capture targets of all delegates." + eol;
                foreach (System.Delegate del in delegates)
                {
                    if (targets.Contains(del.Target))
                        continue;
                    if (del.Target != null)
                    {
                        object del_target = del.Target;
                        targets.Add(del_target);
                        Type del_target_type = del.Target.GetType();
                        foreach (SR.FieldInfo fi in del_target_type.GetFields())
                        {
                            // Here, we do not output fields which are delegates.
                            // Each delegate target is output, and the method handled
                            // as an auto in the C++ AMP code.
                            object field_value = fi.GetValue(del.Target);
                            if (field_value as System.Delegate != null)
                                continue;
                            // Special case for Campy types: remove the
                            // Campy prefix, and convert to pointers.
                            if (fi.FieldType.Name.Contains("Array_View"))
                            {
                                result += "public: void * nav_" + fi.Name + ";" + eol;
                            }
                            else
                            {
                                String na = fi.Name;
                                String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                                result += tys + " " + na + ";" + eol;
                            }
                        }
                    }
                    result += eol;
                }
            }
            result += "void * native_accelerator_view;" + eol;
            result += "void * native_extent;" + eol;
            // Output primary delegate method.
            String method_name = delegates.First().Method.Name;
            method_name = Utility.NormalizeMonoCecilName(method_name);
            result += "// primary delegate entry point" + eol;
            result += "void " + method_name + "();" + eol;
            result += "};" + eol;
            _assembly.unmanaged_h_files.Add(unmanaged_h_file_name, result);

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
            int suffix = 0;
            {
                List<object> targets = new List<object>();
                foreach (System.Delegate del in delegates)
                {
                    if (targets.Contains(del.Target))
                        continue;
                    if (del.Target != null)
                    {
                        object del_target = del.Target;
                        targets.Add(del_target);
                        Type del_target_type = del.Target.GetType();
                        foreach (SR.FieldInfo fi in del_target_type.GetFields())
                        {
                            // Here, we do not output fields which are delegates.
                            // Each delegate target is output, and the method handled
                            // as an auto in the C++ AMP code.
                            object field_value = fi.GetValue(del.Target);
                            if (field_value as System.Delegate != null)
                                continue;
                            String na = fi.Name;
                            if (fi.FieldType.Name.Contains("Array_View"))
                            {
                                result += "Campy::Types::Native_Array_View<int, 1> * data" + suffix + eol;
                                result += "   = (Campy::Types::Native_Array_View<int, 1> *)this->nav_"
                                    + na + ";" + eol;
                                result += "array_view<int, 1>& "
                                    + na + " = *(array_view<int, 1>*)data"
                                    + suffix + "->ar;" + eol;
                                suffix++;
                            }
                            else
                            {
                                String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                                result += tys + " " + na;
                                result += " = this->" + na + ";";
                                result += eol;
                            }
                        }
                    }
                }
            }
            result += "Campy::Types::Native_Extent<1> * data" + suffix
                + " = (Campy::Types::Native_Extent<1> *)this->native_extent;" + eol;
            result += "extent<1>& e"
                + " = *(extent<1>*)data" + suffix + "->ne;" + eol;
            suffix++;
            result += "Campy::Types::Native_Accelerator_View * data" + suffix
                + " = (Campy::Types::Native_Accelerator_View *)this->native_accelerator_view;" + eol;
            result += "accelerator_view& _accerator_view"
                + " = *(accelerator_view*)data" + suffix + "->nav;" + eol;
            suffix++;

            // Associate a delegate with the name of the auto it is stored
            // in.
            Dictionary<String, Delegate> field_names = new Dictionary<string, Delegate>();
            {
                List<object> targets = new List<object>();
                foreach (System.Delegate d in delegates)
                {
                    if (targets.Contains(d.Target))
                        continue;
                    if (d.Target != null)
                    {
                        object dt = d.Target;
                        targets.Add(dt);
                        Type dtt = d.Target.GetType();
                        SR.FieldInfo[] fields = dtt.GetFields();
                        foreach (SR.FieldInfo fi in dtt.GetFields())
                        {
                            // Here, we output fields which are delegates as auto variables.
                            object field_value = fi.GetValue(d.Target);
                            Delegate to_del = field_value as System.Delegate;
                            if (to_del == null)
                                continue;
                            String na = fi.Name;
                            if (field_names.ContainsKey(na))
                                continue;
                            field_names.Add(na, to_del);
                        }
                    }
                }
            }

            // Add in lambdas for each delegate in reverse order.
            StackQueue<System.Delegate> stack = new StackQueue<Delegate>();
            foreach (System.Delegate del in delegates)
                stack.Push(del);
            {
                List<object> targets = new List<object>();
                while (stack.Count > 0)
                {
                    System.Delegate del = stack.Pop();
                    if (targets.Contains(del.Target))
                        continue;
                    if (del.Target != null)
                    {
                        object del_target = del.Target;
                        targets.Add(del_target);
                        Type del_target_type = del.Target.GetType();
                        foreach (SR.FieldInfo fi in del_target_type.GetFields())
                        {
                            // Here, we output fields which are delegates as auto variables.
                            object field_value = fi.GetValue(del.Target);
                            if (field_value as System.Delegate == null)
                                continue;

                            String na = fi.Name;
                            result += "auto " + na + " = [=]" + eol;
                            // Find method of delegate.
                            Delegate to_del = field_names[na];
                            MethodDefinition md = ConvertToMonoCecilType(to_del);
                            {
                                Campy.TreeWalker.MethodParametersAstBuilder astBuilder = new Campy.TreeWalker.MethodParametersAstBuilder(
                                    new ICSharpCode.Decompiler.DecompilerContext(
                                        mod_def) { CurrentType = td });
                                astBuilder.AddMethod(md);
                                StringWriter output = new StringWriter();
                                astBuilder.GenerateCode(new PlainTextOutput(output));
                                String field_result = output.ToString();
                                result += field_result;
                                output.Dispose();
                            }
                            result += " restrict(amp) ";
                            {
                                Campy.TreeWalker.MethodBodyAstBuilder astBuilder = new Campy.TreeWalker.MethodBodyAstBuilder(
                                    new ICSharpCode.Decompiler.DecompilerContext(
                                        mod_def) { CurrentType = td });
                                astBuilder.AddMethod(md);
                                StringWriter output = new StringWriter();
                                astBuilder.GenerateCode(new PlainTextOutput(output));
                                String field_result = output.ToString();
                                field_result = field_result.Replace("this.", "");
                                result += field_result;
                                output.Dispose();
                            }
                            result += ";" + eol;
                        }
                    }
                }
                result += eol;
            }
            result += eol;
            result += "parallel_for_each(e, [=]";
            MethodDefinition main_md = ConvertToMonoCecilType(delegates.First());
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
                xxx = xxx.Replace("this.", "");
                result += xxx;
                output.Dispose();
            }
            result += ");" + eol;
            result += "}" + eol;
            _assembly.unmanaged_cpp_files.Add(unmanaged_cpp_file_name, result);
        }
        
        public Converter(Assembly assembly)
        {
            // Save task list.
            _assembly = assembly;
        }



        public void Convert(System.Delegate del)
        {
            List<Delegate> delegates = Analysis.FindAllTargets(del);

            // Create a class in C++ CLI which contains the top-level
            // delegate method. This method will need to take the entire
            // closure of delegates, inline the chain of method calls.
            // All data in each target will be enclosed within a struct
            // within the class in order to keep each nice and tidy.
            // The name of the class will be the name of the top-level
            // delegate.

            // Convert multidelegate type to Mono.Cecil type, required to convert to C++ AMP.
            MethodDefinition xxxxx = ConvertToMonoCecilType(del);
            TypeDefinition multidelegate_mc = xxxxx.DeclaringType;
            ModuleDefinition mod_def = multidelegate_mc.Module;

            // Derive name of output files based on the name of the full name.
            // Get full name of kernel, including normalization because they cannot be compared directly with Mono.Cecil names.
            String kernel_full_name = string.Format("{0} {1}.{2}({3})", del.Method.ReturnType.FullName, del.Method.ReflectedType.FullName, del.Method.Name, string.Join(",", del.Method.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
            kernel_full_name = Utility.NormalizeSystemReflectionName(kernel_full_name);
            String file_name_stem = kernel_full_name;
            String managed_cpp_file_name = file_name_stem + "_managed.cpp";
            String managed_h_file_name = file_name_stem + "_managed.cpp";
            String unmanaged_cpp_file_name = file_name_stem + "_unmanaged.cpp";
            String unmanaged_h_file_name = file_name_stem + "_unmanaged.h";

            // Generate managed code files.
            GenerateManagedCode(
                kernel_full_name, delegates,
                mod_def, multidelegate_mc,
                managed_cpp_file_name, managed_h_file_name, unmanaged_h_file_name);

            // Generate unmanaced code files.
            GenerateUnmanagedCode(
                kernel_full_name, delegates,
                mod_def, multidelegate_mc,
                unmanaged_cpp_file_name, unmanaged_h_file_name);
        }
    }
}
