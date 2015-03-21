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
using Campy.Graphs;
using System.Text.RegularExpressions;
using Campy.Utils;
using Campy.Types;
using Campy.Types.Utils;
using Campy.Builder;

namespace Campy
{
    class Converter
    {
        String eol = "\r\n";
        Assembly _assembly;
        Dictionary<System.Object, bool> compiled_targets = new Dictionary<object, bool>();
        Dictionary<String, MulticastDelegate> multicastdelegates = new Dictionary<string, MulticastDelegate>();

        String EmitManagedStruct(Structure structure)
        {
            String result = "";
            result += "ref struct " + structure.Name + eol;
            result += "{" + eol;
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(fi.FieldType);
                tys = tys.Replace(".", "::");
                if (TypesUtility.IsCampyTileStaticType(fi.FieldType))
                {
                    // While we may be able to capture Campy Tile_Static objects,
                    // it isn't useful because static tiles must be declared
                    // local in the body of the kernel.
                }
                else if (TypesUtility.IsCampyArrayType(fi.FieldType))
                {
                    result += "Array_Base^ " + na + ";" + eol;
                }
                else if (TypesUtility.IsCampyArrayViewType(fi.FieldType))
                {
                    result += "Array_View_Base^ " + na + ";" + eol;
                }
                else if (TypesUtility.IsSimpleCampyType(fi.FieldType))
                {
                    result += tys + "^ " + na + ";" + eol;
                }
                else
                {
                    // If it isn't a delegate, or Campy type, then
                    // it's a class.
                    na = Campy.Utils.Utility.NormalizeSystemReflectionName(na);
                    tys = Campy.Utils.Utility.NormalizeSystemReflectionName(tys);
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
            foreach (SR.MethodInfo met in structure.methods)
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
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(fi.FieldType);
                String prefix = structure.FullName + ".";
                if (TypesUtility.IsCampyTileStaticType(fi.FieldType))
                {
                    // While we may be able to capture Campy Tile_Static objects,
                    // it isn't useful because static tiles must be declared
                    // local in the body of the kernel.
                }
                else if (TypesUtility.IsSimpleCampyType(fi.FieldType))
                {
                    result += "(void*)" + prefix + na + "->native()," + eol;
                }
                else
                {
                    // If it isn't a delegate, or Campy type, then
                    // it's a class.
                    na = Campy.Utils.Utility.NormalizeSystemReflectionName(na);
                    tys = Campy.Utils.Utility.NormalizeSystemReflectionName(tys);
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
            foreach (SR.MethodInfo met in structure.methods)
            {
            }
            result += "}" + eol;
            return result;
        }

        String EmitUsingDLLs(Structure structure)
        {
            String result = "";
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(fi.FieldType);
                String prefix = structure.FullName + ".";
                if (TypesUtility.IsCampyArrayViewType(fi.FieldType)
                    || TypesUtility.IsCampyArrayType(fi.FieldType)
                    || TypesUtility.IsCampyTileStaticType(fi.FieldType))
                {
                    Type b = fi.FieldType;
                    foreach (Type p in b.GenericTypeArguments)
                    {
                        if (p.IsClass || Campy.Types.Utils.ReflectionCecilInterop.IsStruct(p))
                        {
                            // Output using for class/struct.
                            String target_type_name = "Native_Array_View<"
                                + Campy.Utils.Utility.NormalizeSystemReflectionName(p.Name)
                                + ">";
                            String kernel_assembly_file_name = Campy.Utils.Utility.NormalizeSystemReflectionName(target_type_name);
                            String full_path = Path.GetFullPath(kernel_assembly_file_name);
                            full_path = Path.GetDirectoryName(full_path);
                            String type_full_name = target_type_name;
                            type_full_name = Campy.Utils.Utility.NormalizeSystemReflectionName(type_full_name);
                            String campy_assembly_file_name = full_path + "\\" + type_full_name;
                            campy_assembly_file_name = campy_assembly_file_name + ".dll";
                            result += @"#using """ + campy_assembly_file_name.Replace("\\", "\\\\") + @"""
";
                        }
                    }
                }
                else if (TypesUtility.IsSimpleCampyType(fi.FieldType))
                {
                }
                else
                {
                }
            }
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitUsingDLLs(child);
            }
            foreach (SR.MethodInfo met in structure.methods)
            {
            }
            return result;
        }

        String EmitIncludeHeaders(Structure structure)
        {
            String result = "";
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(fi.FieldType);
                String prefix = structure.FullName + ".";
                if (TypesUtility.IsCampyArrayViewType(fi.FieldType)
                    || TypesUtility.IsCampyArrayType(fi.FieldType)
                    || TypesUtility.IsCampyTileStaticType(fi.FieldType))
                {
                    Type b = fi.FieldType;
                    foreach (Type p in b.GenericTypeArguments)
                    {
                        if (p.IsClass || Campy.Types.Utils.ReflectionCecilInterop.IsStruct(p))
                        {
                            result += @"
#include """ + Campy.Utils.Utility.NormalizeSystemReflectionName(p.FullName) + @"_unmanaged.h""
";
                        }
                    }
                }
                else if (TypesUtility.IsSimpleCampyType(fi.FieldType))
                {
                }
                else
                {
                }
            }
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitUsingDLLs(child);
            }
            foreach (SR.MethodInfo met in structure.methods)
            {
            }
            return result;
        }

        String EmitUsingNamespaces(Structure structure)
        {
            String result = "";
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(fi.FieldType);
                String prefix = structure.FullName + ".";
                if (TypesUtility.IsCampyTileStaticType(fi.FieldType))
                {
                }
                else if (TypesUtility.IsSimpleCampyType(fi.FieldType))
                {
                }
                else
                {
                }
            }
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitUsingNamespaces(child);
            }
            foreach (SR.MethodInfo met in structure.methods)
            {
            }
            return result;
        }

        void GenerateManagedCode(
            System.Delegate del,
            Structure structure,
            String kernel_full_name,
            Extent extent,
            ModuleDefinition mod_def,
            TypeDefinition td,
            String managed_cpp_file_name,
            String managed_h_file_name,
            String unmanaged_h_file_name)
        {
            String result = "";

            // Get assembly qualifed name.
            String file_name_stem = mod_def.FullyQualifiedName;
            if (Path.HasExtension(file_name_stem) && Campy.Utils.Utility.IsRecognizedExtension(Path.GetExtension(file_name_stem)))
            {
                String ext = Path.GetExtension(file_name_stem);
                file_name_stem = file_name_stem.Replace(ext, "");
            }

            // Generate CPP code for managed class.
            result += @"
/* This file, " + managed_cpp_file_name + @", is automatically generated
 * via Campy.NET, " + FileVersionInfo.GetVersionInfo(this.GetType().Assembly.Location).FileVersion + @".
 * The user's assembly, which contains Campy.NET calls, is located at 
 * " + mod_def.FullyQualifiedName + @"
 */

#using ""Campy.Types.dll""
";

            // Emit code to include DLLs.
            result += EmitUsingDLLs(structure);

            // Emit code to include headers.
            result += @"

#include """ + unmanaged_h_file_name.Replace("\\", "\\\\") + @"""
";
            //result += EmitIncludeHeaders(structure);

            // Include namespaces.
            result += @"

using namespace System;
using namespace Campy::Types;
";
            result += EmitUsingNamespaces(structure);

            // Emit class definition.
            result += @"

public ref class " + kernel_full_name + @"_managed
{
";

            // Create class member fields to retain the
            // graph of target objects.
            result += @"
public:
    Accelerator_View^ accelerator_view;
";
            if (null != extent as Tiled_Extent)
                result += "Tiled_Extent^ extent;" + eol;
            else
                result += "Extent^ extent;" + eol;
            result += eol;

            result += EmitManagedStruct(structure);

            object ob = del;
            String method_name = (ob as System.Delegate).Method.Name;
            method_name = Campy.Utils.Utility.NormalizeMonoCecilName(method_name);
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
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(fi.FieldType);
                if (TypesUtility.IsCampyTileStaticType(fi.FieldType))
                {
                }
                else if (TypesUtility.IsSimpleCampyType(fi.FieldType))
                {
                    result += "void * n_" + na + ";" + eol;
                }
                else
                {
                    na = Campy.Utils.Utility.NormalizeSystemReflectionName(na);
                    tys = Campy.Utils.Utility.NormalizeSystemReflectionName(tys);
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
            foreach (SR.MethodInfo met in structure.methods)
            {
            }
            result += "} " + structure.Name + ";" + eol;
            return result;
        }

        String EmitAssignmentUnmanagedStruct1(Structure structure, ModuleDefinition mod_def, Extent extent)
        {
            String result = "";
            result += "struct " + structure.Name.Replace("s", "a") + eol;
            result += "{" + eol;
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(fi.FieldType);
                if (TypesUtility.IsCampyArrayViewType(fi.FieldType))
                {
                    Type element_type = fi.FieldType.GetGenericArguments().First();
                    String element_type_name = Campy.Utils.Utility.GetFriendlyTypeName(element_type);
                    result += "array_view<"
                        + element_type_name.Replace(".", "::")
                        + ", 1> "
                        + na + ";" + eol;
                }
                else if (TypesUtility.IsCampyArrayType(fi.FieldType))
                {
                    Type element_type = fi.FieldType.GetGenericArguments().First();
                    String element_type_name = Campy.Utils.Utility.GetFriendlyTypeName(element_type);
                    result += "array_view<"                            // NOTE MUST BE ARRAY_VIEW, ASSIGNED FROM ARRAY<>!!!!!
                        + element_type_name.Replace(".", "::")
                        + ", 1> "
                        + na + ";" + eol;
                }
                else if (TypesUtility.IsCampyAcceleratorType(fi.FieldType))
                {
                    result += "accelerator "
                        + na + ";" + eol;
                }
                else if (TypesUtility.IsCampyAcceleratorViewType(fi.FieldType))
                {
                    result += "accelerator_view "
                        + na + ";" + eol;
                }
                else if (TypesUtility.IsCampyIndexType(fi.FieldType))
                {
                    result += "index<1> "
                        + na + ";" + eol;
                }
                else if (TypesUtility.IsCampyExtentType(fi.FieldType))
                {
                    result += "extent<1> "
                        + na + ";" + eol;
                }
                else if (TypesUtility.IsCampyTileStaticType(fi.FieldType))
                {
                }
                else
                {
                    na = Campy.Utils.Utility.NormalizeSystemReflectionName(na);
                    tys = Campy.Utils.Utility.NormalizeSystemReflectionName(tys);
                    result += tys + " " + na + ";" + eol;
                }
            }
            result += eol;
            // Add in other structures.
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitAssignmentUnmanagedStruct1(child, mod_def, extent);
            }
            // Add in function declarations.
            foreach (SR.MethodInfo dd in structure.methods)
            {
                MethodDefinition md = Campy.Types.Utils.ReflectionCecilInterop.ConvertToMonoCecilMethodDefinition(dd);
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(dd.ReturnType);
                tys = Campy.Utils.Utility.NormalizeSystemReflectionName(tys);
                result += tys + " " + Campy.Utils.Utility.NormalizeSystemReflectionName(md.Name);
                // Find method of delegate.
                result += EmitMethodParameters(structure, extent, mod_def, md, dd);
                result += " const restrict(amp) ";
                String the_method = ConvertMethodBody(structure, mod_def, md, dd);
                result += ModifyMethodBody(structure, the_method);
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

        String EmitAssignmentUnmanagedStruct2(Structure structure, ModuleDefinition mod_def, Extent extent)
        {
            String result = "";
            result += "{" + eol;
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(fi.FieldType);
                String prefix = structure.FullName + ".";
                if (TypesUtility.IsCampyArrayViewType(fi.FieldType))
                {
                    Type element_type = fi.FieldType.GetGenericArguments().First();
                    String element_type_name = Campy.Utils.Utility.GetFriendlyTypeName(element_type);
                    result += "*(array_view<" + element_type_name.Replace(".", "::") + ", 1>*)"
                        + "(((Campy::Types::Native_Array_View<" + element_type_name.Replace(".", "::") + "> *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (TypesUtility.IsCampyArrayType(fi.FieldType))
                {
                    Type element_type = fi.FieldType.GetGenericArguments().First();
                    String element_type_name = Campy.Utils.Utility.GetFriendlyTypeName(element_type);
                    result += "*(array<" + element_type_name.Replace(".", "::") + ", 1>*)"
                        + "(((Campy::Types::Native_Array<" + element_type_name.Replace(".", "::") + "> *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (TypesUtility.IsCampyAcceleratorType(fi.FieldType))
                {
                    result += "*(accelerator*)"
                        + "(((Campy::Types::Native_Accelerator *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (TypesUtility.IsCampyAcceleratorViewType(fi.FieldType))
                {
                    result += "*(accelerator_view*)"
                        + "(((Campy::Types::Native_Accelerator_View *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (TypesUtility.IsCampyIndexType(fi.FieldType))
                {
                    result += "*(index<1>*)"
                        + "(((Campy::Types::Native_Index<1> *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (TypesUtility.IsCampyExtentType(fi.FieldType))
                {
                    result += "*(extent<1>*)"
                        + "(((Campy::Types::Native_Extent *) " + prefix + "n_" + na + ")->native)"
                        + "," + eol;
                }
                else if (TypesUtility.IsCampyTileStaticType(fi.FieldType))
                {
                }
                else
                {
                    na = Campy.Utils.Utility.NormalizeSystemReflectionName(na);
                    tys = Campy.Utils.Utility.NormalizeSystemReflectionName(tys);
                    result += prefix + na + "," + eol;
                }
            }
            result += eol;
            // Add in other structures.
            foreach (Structure child in structure.nested_structures)
            {
                result += EmitAssignmentUnmanagedStruct2(child, mod_def, extent);
            }
            // Add in function declarations.
            // Actually, for this function, don't emit functions.
            foreach (SR.MethodInfo pair in structure.methods)
            {
            }
            result += "}";
            if (structure.level > 1)
                result += "," + eol;
            else
                result += eol;
            return result;
        }

        String DeclareTileStatics(String code, Structure structure)
        {
            String result = code;
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Campy.Utils.Utility.GetFriendlyTypeName(fi.FieldType);
                String prefix = structure.FullName + ".";
                if (TypesUtility.IsCampyTileStaticType(fi.FieldType))
                {
                    // If no value for field, then I can't get the size.
                    // So, I cannot do substitutions.
                    Tile_Static<int> foo = field_value as Tile_Static<int>;
                    if (foo == null) continue;
                    String static_string = "tile_static int " +
                        fi.Name + "[" + foo.Length + "];" + eol;
                    // Replace first occurence only (outermost bracket).
                    Regex regex2 = new Regex(Regex.Escape("{"));
                    result = regex2.Replace(result, "{" + static_string, 1);

                    // In code, replace "this." + fi.Name with just fi.Name.
                    Regex regex = new Regex("this." + fi.Name + "([^a-zA-Z_]+)");
                    result = regex.Replace(result, fi.Name + "$1");
                }
            }

            // Add in other structures.
            foreach (Structure child in structure.nested_structures)
            {
            }

            return result;
        }

        void GenerateUnmanagedCode(
            System.Delegate del,
            Structure structure,
            String kernel_full_name,
            Extent extent,
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
                method_name = Campy.Utils.Utility.NormalizeMonoCecilName(method_name);
                result += "// primary delegate entry point" + eol;
                result += "public: void " + method_name + "();" + eol;
                result += "};" + eol;
                _assembly.unmanaged_h_files.Add(unmanaged_h_file_name, result);
            }

            {
                String method_name = (del as System.Delegate).Method.Name;
                method_name = Campy.Utils.Utility.NormalizeMonoCecilName(method_name);
                object ob = del;

                // Set up unmanaged cpp file.
                result = "";
                result += "#include <amp.h>" + eol;
                result += "#include <amp_math.h>" + eol;
                result += "#include \"" + unmanaged_h_file_name.Replace("\\", "\\\\") + "\"" + eol;
                result += "#include \"Native_Array.h\"" + eol;
                result += "#include \"Native_Array_View.h\"" + eol;
                result += "#include \"Native_Extent.h\"" + eol;
                result += "#include \"Native_Tiled_Extent.h\"" + eol;
                result += "#include \"Native_Accelerator_View.h\"" + eol;
                result += "#include \"Native_Atomics.h\"" + eol;
                result += EmitIncludeHeaders(structure);
                result += "using namespace concurrency;" + eol + eol;
                result += "void " + kernel_full_name + "_unmanaged::" + method_name + "()" + eol;
                result += "{" + eol;
                result += EmitAssignmentUnmanagedStruct1(structure, mod_def, extent);
                result += "=" + eol;
                result += EmitAssignmentUnmanagedStruct2(structure, mod_def, extent);
                result += ";" + eol;
                if (extent as Tiled_Extent == null)
                    result += "extent<1>& _extent"
                        + " = *(extent<1>*)"
                        + "(((Campy::Types::Native_Extent *) native_extent)->native)"
                        + ";" + eol;
                else
                {
                    Tiled_Extent te = extent as Tiled_Extent;
                    result += "tiled_extent<" + te.Tile_Dims[0] + "> & _extent"
                        + " = *(tiled_extent<" + te.Tile_Dims[0] + ">*)"
                        + "(((Campy::Types::Native_Extent *) native_extent)->native)"
                        + ";" + eol;
                }
                result += "accelerator_view& _accelerator_view"
                    + " = *(accelerator_view*)"
                    + "(((Campy::Types::Native_Accelerator_View *) native_accelerator_view)->native)"
                    + ";" + eol;

                result += eol;
                result += "parallel_for_each(_extent, [=]";

                MethodDefinition main_md = Campy.Types.Utils.ReflectionCecilInterop.ConvertToMonoCecilMethodDefinition(structure._main_method);
                String method_parameter = EmitMethodParameters(structure, extent, mod_def, main_md, structure._main_method);
                String p = method_parameter.Replace("(", "");
                p = p.Replace(")", "");
                p = p.Replace("tiled_index","");
                p = p.Replace("index", "");
                Regex reg = new Regex(@"\<\s?[0123456789]+\s?\>");
                p = reg.Replace(p, "");
                p = p.Replace(" ", "");
                result += method_parameter;
                result += " restrict(amp)";
                result += "{ a1." + Campy.Utils.Utility.NormalizeSystemReflectionName(main_md.Name) + "(" + p + "); }";
                result += ");" + eol;
                result += "}" + eol;
                _assembly.unmanaged_cpp_files.Add(unmanaged_cpp_file_name, result);
            }
        }

        private static String EmitMethodParameters(Structure structure, Extent extent, ModuleDefinition mod_def, MethodDefinition main_md, SR.MethodInfo dd)
        {
            String result = "";
                Campy.TreeWalker.MethodParametersAstBuilder astBuilder = new Campy.TreeWalker.MethodParametersAstBuilder(
                    new ICSharpCode.Decompiler.DecompilerContext(
                        mod_def) { CurrentType = main_md.DeclaringType });
                astBuilder.AddMethod(main_md);
                StringWriter output = new StringWriter();
                astBuilder.GenerateCode(new PlainTextOutput(output));
                String xxx = output.ToString();
                if (extent as Tiled_Extent != null)
                {
                    Tiled_Extent te = extent as Tiled_Extent;
                    xxx = xxx.Replace("Tiled_Index", "tiled_index<" + +te.Tile_Dims[0] + ">");
                }
                else
                    xxx = xxx.Replace("Index", "index<1>");
                result += xxx;
                output.Dispose();
            return result;
        }

        private string RecursiveRewriteClassField(Structure structure, String xxx)
        {
            // Rewrite class references for this structure.
            foreach (Tuple<System.Reflection.FieldInfo, object> tuple in structure._class_fields)
            {
                Structure nested_struct;
                if (Structure.map_target_to_structure.TryGetValue(tuple.Item2, out nested_struct))
                {
                    String cur_struct = structure.FullName;
                    String ne_struct = nested_struct.FullName;
                    String pre = "";
                    if (!cur_struct.Equals(ne_struct))
                        pre = ne_struct.Replace(cur_struct + ".", "").Replace("s", "a");
                    bool is_static = tuple.Item1.IsStatic;
                    if (is_static)
                    {
                        Regex reg = new Regex(
                            @"([\s\.\!])"
                            + (tuple.Item1.Name
                                .Replace("$", @"\$")
                                .Replace("<", @"\<")
                                .Replace(">", @"\>")
                                )
                            + @"([^a-zA-Z_]+)");
                        xxx = reg.Replace(xxx,
                            @"$1"
                            + pre
                            + @"$2");
                    }
                    else
                    {
                        Regex reg = new Regex(
                            @"this\s?\.\s?"
                            + (tuple.Item1.Name
                                .Replace("$", @"\$")
                                .Replace("<", @"\<")
                                .Replace(">", @"\>")
                                )
                            + @"([^a-zA-Z_]+)");
                        xxx = reg.Replace(xxx,
                            pre
                            + @"$1");
                    }
                }
            }
            // Apply recursively with children structures.
            foreach (Structure child in structure.nested_structures)
            {
                xxx = RecursiveRewriteClassField(child, xxx);
            }
            return xxx;
        }

        private string RecursiveRewriteDelegateField(Structure structure, String xxx)
        {
            // Rewrite delegate fields.
            foreach (Tuple<System.Reflection.FieldInfo, Delegate> tuple in structure._delegate_fields)
            {
                Structure nested_struct = null;
                if (tuple.Item2.Target == null)
                {
                    bool found = false;
                    // Delegate target null. Look for child structure.
                    foreach (Structure child in structure.nested_structures)
                    {
                        if (child._class_instance.GetType() == tuple.Item2.Method.DeclaringType)
                        {
                            found = true;
                            nested_struct = child;
                            break;
                        }
                    }
                    if (!found)
                    {
                        if (structure._class_instance.GetType() == tuple.Item2.Method.DeclaringType)
                        {
                            found = true;
                            nested_struct = structure;
                        }
                    }
                    Debug.Assert(found);
                }
                else
                {
                    Structure.map_target_to_structure.TryGetValue(tuple.Item2.Target, out nested_struct);
                }
                Debug.Assert(nested_struct != null);
                // Get prefix of current structure.
                String cur_struct = structure.FullName;
                String ne_struct = nested_struct.FullName;
                String pre = "";
                if (!cur_struct.Equals(ne_struct))
                    pre = ne_struct.Replace(cur_struct + ".", "").Replace("s", "a") + ".";
                bool is_static = tuple.Item1.IsStatic;
                if (is_static)
                {
                    Regex reg = new Regex(
                        @"([\s\.\!])"
                        + (tuple.Item1.Name
                                .Replace("$", @"\$")
                                .Replace("<", @"\<")
                                .Replace(">", @"\>")
                                )
                        + @"([^a-zA-Z_]+)");
                    xxx = reg.Replace(xxx,
                        @"$1"
                        + pre
                        + Campy.Utils.Utility.NormalizeSystemReflectionName(tuple.Item2.Method.Name)
                        + @"$2");
                }
                else
                {
                    // With "this"
                    Regex reg = new Regex(
                        @"this\s?\.\s?"
                        + tuple.Item1.Name
                        + @"([^a-zA-Z_]+)");
                    xxx = reg.Replace(xxx,
                        pre
                        + Campy.Utils.Utility.NormalizeSystemReflectionName(tuple.Item2.Method.Name)
                        + @"$1");
                    // Or, without "this"
                    Regex reg2 = new Regex(
                        @"([\s\.\!])"
                        + tuple.Item1.Name
                        + @"([^a-zA-Z_]+)");
                    xxx = reg2.Replace(xxx,
                        @"$1"
                        + pre
                        + Campy.Utils.Utility.NormalizeSystemReflectionName(tuple.Item2.Method.Name)
                        + @"$2");
                }
            }
            // Apply recursively with children structures.
            foreach (Structure child in structure.nested_structures)
            {
                xxx = RecursiveRewriteDelegateField(child, xxx);
            }
            return xxx;
        }

        private string ModifyMethodBody(Structure structure, String xxx)
        {
            // Here's where magic happens. Rewrite references to fields that are
            // classes or method calls.
            xxx = RecursiveRewriteClassField(structure, xxx);
            xxx = RecursiveRewriteDelegateField(structure, xxx);

            // Convert method calls that use "this.", or static prefix, into calls
            // referencing nested struct instead.
            foreach (SR.MethodInfo method in structure.methods)
            {
                if (method.IsStatic)
                {
                    String declaring_type_name = method.DeclaringType.FullName;
                    String prefix = FindMethodPrefix(method, structure);
                    prefix = prefix.Replace(structure.FullName, "");
                    prefix = prefix.Replace(structure.Name, "");
                    prefix = prefix.Replace("s", "a");
                    if (prefix != "")
                        xxx = xxx.Replace(declaring_type_name, prefix);
                    else
                        xxx = xxx.Replace(declaring_type_name + ".", "");
                }
                else
                {
                    String declaring_type_name = method.DeclaringType.FullName;
                    String prefix = FindMethodPrefix(method, structure);
                    prefix = prefix.Replace(structure.FullName, "");
                    prefix = prefix.Replace(structure.Name, "");
                    prefix = prefix.Replace("s", "a");
                    if (prefix != "")
                        xxx = xxx.Replace(declaring_type_name, prefix);
                    else
                        xxx = xxx.Replace("this." + method.Name, "" + method.Name);
                }
            }

            // Get all fields of structure and patch path.
            foreach (SR.FieldInfo field in structure.simple_fields)
            {
                if (field.IsStatic)
                {
                    String name = field.Name;
                    String declaring_type_name = field.DeclaringType.FullName;
                    String prefix = FindFieldPrefix(field, structure);
                    prefix = prefix.Replace(structure.FullName, "");
                    prefix = prefix.Replace(structure.Name, "");
                    prefix = prefix.Replace("s", "a");
                    if (prefix != "")
                        xxx = xxx.Replace(declaring_type_name, prefix);
                    else
                        xxx = xxx.Replace(declaring_type_name + ".", "");
                }
                else
                {
                    String name = field.Name;
                    String declaring_type_name = field.DeclaringType.FullName;
                    String prefix = FindFieldPrefix(field, structure);
                    prefix = prefix.Replace(structure.FullName, "");
                    prefix = prefix.Replace(structure.Name, "");
                    prefix = prefix.Replace("s", "a");
                    if (prefix != "")
                        xxx = xxx.Replace(declaring_type_name, prefix);
                    else
                        xxx = xxx.Replace("this." + name, "" + name);
                }
            }

            // Get all methods of target.
            Type owner = null;
            if (structure._class_instance != null)
                owner = structure._class_instance.GetType();
            else if (structure._main_method != null)
                owner = structure._main_method.DeclaringType;
            
            if (owner != null)
            {
                foreach (SR.FieldInfo fi in owner.GetFields())
                {
                    object v = fi.GetValue(structure._class_instance);
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
                    String repl = child.FullName.Replace("s", "a") + ".";
                    xxx = xxx.Replace(find, repl);
                }
            }

            xxx = xxx.Replace("Math.Sqrt", "concurrency::precise_math::sqrt");
            xxx = xxx.Replace("AMP.Atomic_Fetch_Add", "concurrency::atomic_fetch_add");
            xxx = xxx.Replace("Atomic_Fetch_Add", "concurrency::atomic_fetch_add");
            xxx = xxx.Replace(".Tile[", ".tile[");
            xxx = xxx.Replace(".Local[", ".local[");
            xxx = xxx.Replace(".global[", ".global[");
            xxx = xxx.Replace(".Barrier.", ".barrier.");
            xxx = xxx.Replace(".Wait()", ".wait()");
            xxx = xxx.Replace("AMP.", "AMP::");
            xxx = xxx.Replace("(ref", "(");

            // Replace "default(int)" to 0, etc.
            Regex regex = new Regex("default\\s?[(][a-zA-Z_]+[)]");
            var arr = regex.Matches(xxx)
                .Cast<Match>()
                .Select(m => m.Value)
                .ToArray();
            if (arr.Count() > 0)
            {
                List<String> subs = new List<string>();
                for (int i = 0; i < arr.Count(); ++i)
                {
                    String su = arr[i];
                    su = su.Replace("default", "");
                    su = su.Replace("(", "");
                    su = su.Replace(")", "");
                    su = su.Replace(" ", "");
                    su = su.Replace("\t", "");
                    if (su.Equals("int"))
                        subs.Add("0");
                    else if (su.Equals("float"))
                        subs.Add("0.0");
                    else if (su.Equals("uint"))
                        subs.Add("0");
                    else if (su.Equals("double"))
                        subs.Add("0.0");
                    else
                        subs.Add(arr[i]);
                }
                for (int i = 0; i < arr.Count(); ++i)
                {
                    xxx = regex.Replace(xxx, subs[i], 1);
                }
            }

            // Severe magic (hacking) here. There are no static variables within a method,
            // so here we opt for tile_statics to be declared in C# outside the parallel_for_each
            // then captured. Here, we add in all tile_statics into the body of the declaration.
            // Go through all structures and emit tile_static declarations, to be inserted
            // into the code block!!
            xxx = DeclareTileStatics(xxx, structure);

            // All remaining "this." assume at top level.
            xxx = xxx.Replace("this.", "a1.");
            return xxx;
        }

        private static String ConvertMethodBody(Structure structure, ModuleDefinition mod_def, MethodDefinition md, SR.MethodInfo dd)
        {
            String xxx;
            StringWriter output;
            Campy.TreeWalker.MethodBodyAstBuilder astBuilder = new Campy.TreeWalker.MethodBodyAstBuilder(
                new ICSharpCode.Decompiler.DecompilerContext(
                    mod_def) { CurrentType = md.DeclaringType });
            astBuilder.AddMethod(md);
            // Go up the type decls and collect generic parameters.
            Dictionary<String, String> rew = new Dictionary<string, string>();
            //Type z = Campy.Types.Utils.ReflectionCecilInterop.ConvertToSystemReflectionType(md.DeclaringType);
            Type z = dd.DeclaringType;
            while (z != null)
            {
                if (z.IsGenericType)
                {
                    Type z3 = z.GetGenericTypeDefinition();
                    Type[] z4 = z3.GetGenericArguments();
                    Type[] z2 = z.GetGenericArguments();
                    for (int z5 = 0; z5 < z4.Length; ++z5)
                        rew.Add(z4[z5].Name, Campy.Utils.Utility.GetFriendlyTypeName(z2[z5]));
                }
                z = z.DeclaringType;
            }
            if (rew.Count > 0)
                astBuilder.SetUpGenericSubstitition(rew);
            output = new StringWriter();
            astBuilder.GenerateCode(new PlainTextOutput(output));
            xxx = output.ToString();
            output.Dispose();
            return xxx;
        }

        String FindMethodName(SR.MethodInfo mi, Structure structure)
        {
            String result = null;
            foreach (SR.MethodInfo met in structure.methods)
            {
                if (mi == met)
                    return met.Name;
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
            foreach (SR.MethodInfo met in structure.methods)
            {
                if (mi == met)
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

        String FindFieldPrefix(SR.FieldInfo fi, Structure structure)
        {
            String result = null;
            foreach (SR.FieldInfo field in structure.simple_fields)
            {
                if (fi == field)
                    return structure.Name;
            }
            foreach (Structure child in structure.nested_structures)
            {
                String child_result = FindFieldPrefix(fi, child);
                if (child_result != null)
                    return structure.Name + "." + child_result;
            }
            return result;
        }

        public Converter(Assembly assembly)
        {
            // Save task list.
            _assembly = assembly;
        }

        public void Convert(System.Delegate del, Campy.Types.Extent extent, Structure structure)
        {
            // Create a class in C++ CLI which contains the top-level
            // delegate method. This method will need to take the entire
            // closure of list_of_targets, inline the chain of method calls.
            // All data in each target will be enclosed within a struct
            // within the class in order to keep each nice and tidy.
            // The name of the class will be the name of the top-level
            // delegate.

            // Convert multidelegate type to Mono.Cecil type, required to convert to C++ AMP.
            MethodDefinition xxxxx = Campy.Types.Utils.ReflectionCecilInterop.ConvertToMonoCecilMethodDefinition(del.Method);
            TypeDefinition multidelegate_mc = xxxxx.DeclaringType;
            ModuleDefinition mod_def = multidelegate_mc.Module;

            // Derive name of output files based on the name of the full name.
            // Get full name of kernel, including normalization because they cannot be compared directly with Mono.Cecil names.
            String kernel_full_name = string.Format("{0} {1}.{2}({3})", del.Method.ReturnType.FullName, Campy.Utils.Utility.RemoveGenericParameters(del.Method.ReflectedType), del.Method.Name, string.Join(",", del.Method.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
            kernel_full_name = Campy.Utils.Utility.NormalizeSystemReflectionName(kernel_full_name);
            String file_name_stem = kernel_full_name;
            String managed_cpp_file_name = file_name_stem + "_managed.cpp";
            String managed_h_file_name = file_name_stem + "_managed.cpp";
            String unmanaged_cpp_file_name = file_name_stem + "_unmanaged.cpp";
            String unmanaged_h_file_name = file_name_stem + "_unmanaged.h";

            // Generate managed code files.
            GenerateManagedCode(
                del, structure,
                kernel_full_name,
                extent,
                mod_def, multidelegate_mc,
                managed_cpp_file_name, managed_h_file_name, unmanaged_h_file_name);

            // Generate unmanaced code files.
            GenerateUnmanagedCode(
                del, structure,
                kernel_full_name,
                extent,
                mod_def, multidelegate_mc,
                unmanaged_cpp_file_name, unmanaged_h_file_name);
        }
    }
}
