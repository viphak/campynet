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
        List<String> multicastdelegates = new List<string>();

        public Mono.Cecil.TypeDefinition ConvertToMonoCecilType(System.MulticastDelegate multidelegate)
        {
            SR.MethodInfo mi = multidelegate.Method;

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

            return lambda_method.DeclaringType;
        }

        void GenerateManagedCode(
            String kernel_full_name,
            ModuleDefinition mod_def, TypeDefinition td,
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
            result += "// Names of field are the same as closure in C#." + eol;
            foreach (FieldDefinition fi in td.Fields)
            {
                String na = fi.Name;
                if (multicastdelegates.Contains(na))
                    continue;
                TreeWalker.ClassFieldsAstBuilder astBuilder = new TreeWalker.ClassFieldsAstBuilder(
                    new ICSharpCode.Decompiler.DecompilerContext(
                        mod_def) { CurrentType = td });
                astBuilder.AddField(fi);
                StringWriter output = new StringWriter();
                astBuilder.GenerateCode(new PlainTextOutput(output));
                String field_result = output.ToString();
                result += field_result;
                output.Dispose();
            }
            result += "public: Accelerator_View^ accelerator_view;" + eol;
            result += "public: Extent^ extent;" + eol;
            result += eol;
            foreach (MethodDefinition method_definition in td.Methods)
            {
                // Don't output contructor methods.
                if (method_definition.Name.Equals(".ctor"))
                    continue;
                // Don't output anything but lambdas with void(Index) signature.
                if ("System.Void" != method_definition.MethodReturnType.ReturnType.FullName)
                    continue;
                if (method_definition.Parameters.Count != 1)
                    continue;
                if (method_definition.Parameters.First().ParameterType.FullName != "Campy.Types.Index")
                    continue;

                String method_name = method_definition.Name;
                method_name = Utility.NormalizeMonoCecilName(method_name);
                result += "// delegate" + eol;
                {
                    String na = method_definition.Name;
                    //TreeWalker.MethodSignatureAstBuilder astBuilder = new TreeWalker.MethodSignatureAstBuilder(
                    //    new ICSharpCode.Decompiler.DecompilerContext(
                    //        mod_def) { CurrentType = td });
                    //astBuilder.AddMethod(method_definition);
                    //StringWriter output = new StringWriter();
                    //astBuilder.GenerateCode(new PlainTextOutput(output));
                    //String field_result = output.ToString();
                    //result += field_result;
                    //output.Dispose();
                    result += "void " + method_name + "()" + eol;
                }
                
                result += "{" + eol;
                result += "// Create unmanaged object." + eol;
                result += kernel_full_name + "_unmanaged * unm = new "
                     + kernel_full_name + "_unmanaged" + "();" + eol + eol;
                result += "// Copy data from C# lambda into unmanaged object." + eol;
                foreach (FieldDefinition fi in td.Fields)
                {
                    String na = fi.Name;
                    if (multicastdelegates.Contains(na))
                        continue;
                    if (fi.FieldType.Name.Contains("Array_View"))
                    {
                        result += "unm->nav_" + fi.Name + " = (void *)" + fi.Name + "->nav();" + eol;
                    }
                    else
                    {
                        result += "unm->" + fi.Name + " = " + fi.Name + ";" + eol;
                    }
                }
                result += "unm->native_accelerator_view = accelerator_view->nav();" + eol;
                result += "unm->native_extent = extent->ne();" + eol;
                //if ("System.Void" != method_definition.MethodReturnType.ReturnType.FullName)
                //    result += "return ";
                result += "unm->" + method_name + "();" + eol;
                result += "}" + eol;
            }
            result += "};" + eol;

            _assembly.managed_cpp_files.Add(managed_cpp_file_name, result);
        }

        void GenerateUnmanagedCode(
            String kernel_full_name,
            ModuleDefinition mod_def, TypeDefinition td,
            String unmanaged_cpp_file_name, String unmanaged_h_file_name)
        {
            String result = "";

            // Set up class in header.
            result += "class "
                 + kernel_full_name + "_unmanaged" + eol;
            result += "{" + eol;
            foreach (FieldDefinition fi in td.Fields)
            {
                String na = fi.Name;
                if (multicastdelegates.Contains(na))
                    continue;
                if (fi.FieldType.Name.Contains("Array_View"))
                {
                    result += "public: void * nav_" + fi.Name + ";" + eol;
                }
                else
                {
                    TreeWalker.ClassFieldsAstBuilder astBuilder = new TreeWalker.ClassFieldsAstBuilder(
                        new ICSharpCode.Decompiler.DecompilerContext(
                            mod_def) { CurrentType = td });
                    astBuilder.AddField(fi);
                    StringWriter output = new StringWriter();
                    astBuilder.GenerateCode(new PlainTextOutput(output));
                    String field_result = output.ToString();
                    result += field_result;
                    output.Dispose();
                }
            }
            result += "void * native_accelerator_view;" + eol;
            result += "void * native_extent;" + eol;
            foreach (MethodDefinition method_definition in td.Methods)
            {
                // Don't output contructor methods.
                if (method_definition.Name.Equals(".ctor"))
                    continue;
                // Don't output anything but lambdas with void(Index) signature.
                if ("System.Void" != method_definition.MethodReturnType.ReturnType.FullName)
                    continue;
                if (method_definition.Parameters.Count != 1)
                    continue;
                ParameterDefinition xxxxxxx = method_definition.Parameters.First();
                if (method_definition.Parameters.First().ParameterType.FullName != "Campy.Types.Index")
                    continue;
                String method_name = method_definition.Name;
                method_name = Utility.NormalizeMonoCecilName(method_name);
                result += "void " + method_name + "();" + eol;
            }
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

            // Output delegates.
            foreach (MethodDefinition method_definition in td.Methods)
            {
                // Don't output contructor methods.
                if (method_definition.Name.Equals(".ctor"))
                    continue;
                // Don't output anything but lambdas with void(Index) signature.
                if ("System.Void" != method_definition.MethodReturnType.ReturnType.FullName)
                    continue;
                if (method_definition.Parameters.Count != 1)
                    continue;
                ParameterDefinition xxxxxxx = method_definition.Parameters.First();
                if (method_definition.Parameters.First().ParameterType.FullName != "Campy.Types.Index")
                    continue;
                String method_name = method_definition.Name;
                method_name = Utility.NormalizeMonoCecilName(method_name);
                result += "// delegate" + eol;
                result += "void " + kernel_full_name + "_unmanaged::" + method_name + "()" + eol;
                result += "{" + eol;
                int suffix = 0;
                // Add locals initialization from fields of the unmanaged class.
                foreach (FieldDefinition fi in td.Fields)
                {
                    String na = fi.Name;
                    if (multicastdelegates.Contains(na))
                        continue;
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
                        TreeWalker.ClassFieldsAstBuilder astBuilder = new TreeWalker.ClassFieldsAstBuilder(
                            new ICSharpCode.Decompiler.DecompilerContext(
                                mod_def) { CurrentType = td });
                        astBuilder.AddField(fi);
                        StringWriter output = new StringWriter();
                        astBuilder.GenerateCode(new PlainTextOutput(output));
                        String field_result = output.ToString();
                        field_result = field_result.Replace("public: ", "");
                        field_result = field_result.Replace(";", "");
                        field_result = field_result.Replace("\r", "");
                        field_result = field_result.Replace("\n", "");
                        field_result += " = this->" + na + ";";
                        result += field_result + eol;
                        output.Dispose();
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

                // Add in lambdas for each delegate.
                int i = 1;
                foreach (FieldDefinition fi in td.Fields)
                {
                    String na = fi.Name;
                    if (!multicastdelegates.Contains(na))
                        continue;
                    result += "auto " + na + " = [=]";
                    // Get corresponding delegate from field value.

                    // for now, assume first method of class/first field.
                    MethodDefinition md = td.Methods[i];
                    i++;
                    {
                        TreeWalker.MethodParametersAstBuilder astBuilder = new TreeWalker.MethodParametersAstBuilder(
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
                        TreeWalker.MethodBodyAstBuilder astBuilder = new TreeWalker.MethodBodyAstBuilder(
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
                result += "parallel_for_each(e, [=]";
                {
                    TreeWalker.MethodParametersAstBuilder astBuilder = new TreeWalker.MethodParametersAstBuilder(
                        new ICSharpCode.Decompiler.DecompilerContext(
                            mod_def) { CurrentType = td });
                    astBuilder.AddMethod(method_definition);
                    StringWriter output = new StringWriter();
                    astBuilder.GenerateCode(new PlainTextOutput(output));
                    String xxx = output.ToString();
                    xxx = xxx.Replace("Index", "index<1>");
                    result += xxx;
                    output.Dispose();
                }
                result += " restrict(amp)";
                {
                    TreeWalker.MethodBodyAstBuilder astBuilder = new TreeWalker.MethodBodyAstBuilder(
                        new ICSharpCode.Decompiler.DecompilerContext(
                            mod_def) { CurrentType = td });
                    astBuilder.AddMethod(method_definition);
                    StringWriter output = new StringWriter();
                    astBuilder.GenerateCode(new PlainTextOutput(output));
                    String xxx = output.ToString();
                    xxx = xxx.Replace("this.", "");
                    result += xxx;
                    output.Dispose();
                }
                result += ");" + eol;
                result += "}" + eol;
            }
            _assembly.unmanaged_cpp_files.Add(unmanaged_cpp_file_name, result);
        }
        
        public Converter(Assembly assembly)
        {
            // Save task list.
            _assembly = assembly;
        }

        public void Convert(System.MulticastDelegate multidelegate)
        {
            // Get the class instance that this delegate invokes.
            // See https://msdn.microsoft.com/en-us/library/system.multicastdelegate(v=vs.110).aspx
            object target = multidelegate.Target;

            // Check if this target is already being converted. If so, stop.
            bool found = false;
            if (this.compiled_targets.TryGetValue(target, out found))
                return;
            this.compiled_targets.Add(target, true);

            Type target_type = target.GetType();

            // Assert that the target type actually contains the method.
            SR.MethodInfo[] target_type_methodinfo = target_type.GetMethods();
            foreach (SR.MethodInfo method in target_type_methodinfo)
            {
                if (method == multidelegate.Method)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                throw new Exception("Internal assumption failure: the method is not contained in the delegate target!");

            // Convert all fields which happen to also be multicast delegates.
            SR.FieldInfo[] target_type_fieldinfo = target_type.GetFields();
            foreach (var field in target_type_fieldinfo)
            {
                var value = field.GetValue(target);
                Type ft = value.GetType();
                if (value as System.MulticastDelegate != null)
                {
                    // All local fields which are multicastdelegate should be noted.
                    // We don't keep a corresponding field in the converted object types.
                    if (multicastdelegates.Contains(field.Name))
                        continue;
                    multicastdelegates.Add(field.Name);

                    // Chase down the field.
                    Convert(value as System.MulticastDelegate);
                }
            }

            // multidelegate is a new target. Convert all methods associated
            // with the target type.

            // Convert multidelegate type to Mono.Cecil type, required to convert to C++ AMP.
            TypeDefinition multidelegate_mc = ConvertToMonoCecilType(multidelegate);
            ModuleDefinition mod_def = multidelegate_mc.Module;

            // Derive name of output files based on the name of the full name.
            String kernel_full_name = multidelegate_mc.FullName;
            kernel_full_name = Utility.NormalizeSystemReflectionName(kernel_full_name);
            String file_name_stem = kernel_full_name;
            String managed_cpp_file_name = file_name_stem + "_managed.cpp";
            String managed_h_file_name = file_name_stem + "_managed.cpp";
            String unmanaged_cpp_file_name = file_name_stem + "_unmanaged.cpp";
            String unmanaged_h_file_name = file_name_stem + "_unmanaged.h";

            // Generate managed code files.
            GenerateManagedCode(
                kernel_full_name, mod_def, multidelegate_mc,
                managed_cpp_file_name, managed_h_file_name, unmanaged_h_file_name);

            // Generate unmanaced code files.
            GenerateUnmanagedCode(
                kernel_full_name, mod_def, multidelegate_mc,
                unmanaged_cpp_file_name, unmanaged_h_file_name);
        }
    }
}
