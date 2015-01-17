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

namespace Campy
{
    class Converter
    {
        String eol = "\r\n";
        Assembly _assembly;

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
                TreeWalker.CAMPY_AstBuilder astBuilder = new TreeWalker.CAMPY_AstBuilder(
                    new ICSharpCode.Decompiler.DecompilerContext(
                        mod_def) { CurrentType = td });
                astBuilder.AddField(fi);
                StringWriter output = new StringWriter();
                astBuilder.GenerateCode(new PlainTextOutput(output));
                String field_result = output.ToString();
                result += field_result + eol;
                output.Dispose();
            }
            result += "Extent^ extent;" + eol;
            result += eol;
            result += "// Closure delegate always named Thunk" + eol;
            result += "void Thunk()" + eol;
            result += "{" + eol;
            result += "// Create unmanaged object." + eol;
            result += kernel_full_name + "_unmanaged * unm = new "
                 + kernel_full_name + "_unmanaged" + "();" + eol + eol;
            result += "// Copy data from C# lambda into unmanaged object." + eol;
            foreach (FieldDefinition fi in td.Fields)
            {
                String na = fi.Name;
                if (fi.FieldType.Name.Contains("Array_View"))
                {
                    result += "unm->nav_" + fi.Name + " = (void *)" + fi.Name + "->nav();" + eol;
                }
                else
                {
                    result += "unm->" + fi.Name + " = " + fi.Name + ";" + eol;
                }
            }
            result += "unm->ne = extent->ne();" + eol;
            result += "unm->Thunk();" + eol;
            result += "}" + eol;
            result += "};" + eol;
            
            _assembly.managed_cpp_files.Add(managed_cpp_file_name, result);
        }

        void GenerateUnmanagedCode(
            String kernel_full_name,
            ModuleDefinition mod_def, TypeDefinition td,
            MethodDefinition lambda_method,
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
                if (fi.FieldType.Name.Contains("Array_View"))
                {
                    result += "void * nav_" + fi.Name + ";" + eol;
                }
                else
                {
                    TreeWalker.CAMPY_AstBuilder astBuilder = new TreeWalker.CAMPY_AstBuilder(
                        new ICSharpCode.Decompiler.DecompilerContext(
                            mod_def) { CurrentType = td });
                    astBuilder.AddField(fi);
                    StringWriter output = new StringWriter();
                    astBuilder.GenerateCode(new PlainTextOutput(output));
                    String field_result = output.ToString();
                    result += field_result + eol;
                    output.Dispose();
                }
            }
            result += "void Thunk();" + eol;
            result += "void * ne;" + eol;
            result += "};" + eol;
            _assembly.unmanaged_h_files.Add(unmanaged_h_file_name, result);

            // Set up unmanaged cpp file.
            result = "";
            result += "#include <amp.h>" + eol;
            result += "#include \"" + unmanaged_h_file_name.Replace("\\", "\\\\") + "\"" + eol;
            result += "#include \"Native_Array_View.h\"" + eol;
            result += "#include \"Native_Extent.h\"" + eol + eol;
            result += "using namespace concurrency;" + eol + eol;
            result += "void " + kernel_full_name + "_unmanaged::Thunk()" + eol;
            result += "{" + eol;
            int suffix = 0;
            // Add locals initialization from fields of the unmanaged class.
            foreach (FieldDefinition fi in td.Fields)
            {
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
                    TreeWalker.CAMPY_AstBuilder astBuilder = new TreeWalker.CAMPY_AstBuilder(
                        new ICSharpCode.Decompiler.DecompilerContext(
                            mod_def) { CurrentType = td });
                    astBuilder.AddField(fi);
                    StringWriter output = new StringWriter();
                    astBuilder.GenerateCode(new PlainTextOutput(output));
                    String field_result = output.ToString();
                    field_result = field_result.Replace("public:", "");
                    field_result = field_result.Replace(";", "");
                    field_result = field_result.Replace("\r", "");
                    field_result = field_result.Replace("\n", "");
                    field_result += " = this->" + na + ";";
                    result += field_result + eol;
                    output.Dispose();
                }
            }
            result += "Campy::Types::Native_Extent<1> * data" + suffix
                + " = (Campy::Types::Native_Extent<1> *)this->ne;" + eol;
            result += "extent<1>& e"
                + " = *(extent<1>*)data" + suffix + "->ne;" + eol;
            result += "parallel_for_each(e, [=]";
            {
                TreeWalker.MethodParameters_CAMPY_AstBuilder astBuilder = new TreeWalker.MethodParameters_CAMPY_AstBuilder(
                    new ICSharpCode.Decompiler.DecompilerContext(
                        mod_def) { CurrentType = td });
                astBuilder.AddMethod(lambda_method);
                StringWriter output = new StringWriter();
                astBuilder.GenerateCode(new PlainTextOutput(output));
                String xxx = output.ToString();
                xxx= xxx.Replace("Index", "index<1>");
                result += xxx;
                output.Dispose();
            }
            result += " restrict(amp)";
            {
                TreeWalker.MethodBody_CAMPY_AstBuilder astBuilder = new TreeWalker.MethodBody_CAMPY_AstBuilder(
                    new ICSharpCode.Decompiler.DecompilerContext(
                        mod_def) { CurrentType = td });
                astBuilder.AddMethod(lambda_method);
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

        public void Convert(MethodDefinition lambda_method)
        {
            String kernel_full_name = lambda_method.FullName;
            kernel_full_name = Utility.NormalizeSystemReflectionName(kernel_full_name);

            TypeDefinition td = lambda_method.DeclaringType;
            
            ModuleDefinition mod_def = td.Module;

            // Derive name of output files based on the name of the full name.
            //String file_name_stem = mod_def.FullyQualifiedName;
            //if (Path.HasExtension(file_name_stem) && Utility.IsRecognizedExtension(Path.GetExtension(file_name_stem)))
            //{
            //    String extension = Path.GetExtension(file_name_stem);
            //    file_name_stem = file_name_stem.Replace(extension, "");
            //}
            String file_name_stem = kernel_full_name;
            String managed_cpp_file_name = file_name_stem + "_managed.cpp";
            String managed_h_file_name = file_name_stem + "_managed.cpp";
            String unmanaged_cpp_file_name = file_name_stem + "_unmanaged.cpp";
            String unmanaged_h_file_name = file_name_stem + "_unmanaged.h";

            // Generate managed code files.
            GenerateManagedCode(
                kernel_full_name, mod_def, td,
                managed_cpp_file_name, managed_h_file_name, unmanaged_h_file_name);

            // Generate unmanaced code files.
            GenerateUnmanagedCode(
                kernel_full_name, mod_def, td, lambda_method,
                unmanaged_cpp_file_name, unmanaged_h_file_name);
        }
    }
}
