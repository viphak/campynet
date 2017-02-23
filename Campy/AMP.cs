using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.PE;
using SR = System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using Campy.Graphs;
using Campy.Types;
using Campy.Utils;
using Campy.Builder;

namespace Campy
{
    public class AMP
    {
        [DllImport("kernel32.dll")]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        static extern bool LoadLibraryA(string hModule);

        [DllImport("kernel32.dll")]
        static extern bool GetModuleHandleExA(int dwFlags, string ModuleName, ref IntPtr phModule);

        static Build builder = new Build();
        static Dictionary<String, Assembly> assemblies = new Dictionary<String, Assembly>();

        public delegate void _Kernel_type(Campy.Types.Index idx);
        public delegate void _Kernel_tiled_type(Tiled_Index idx);


        public static Tile_Static<T> AllocateTileStatic<T>(int size)
        {
            Tile_Static<T> tile = new Tile_Static<T>(size);
            return tile;
        }

        /// <summary>
        /// Atomic add of _Dest. Note: C# does not allow ref passing of properties,
        /// dynamic members, and most importantly, indices, such as in Array_View's.
        /// So, in order to handle this, many forms of the atomic function are provided.
        /// </summary>
        /// <param name="_Dest"></param>
        /// <param name="_Value"></param>
        /// <returns></returns>
        static public int Atomic_Fetch_Add(ref int _Dest, int _Value)
        {
            int orig = _Dest;
            _Dest++;
            return orig;
        }

        static public int Atomic_Fetch_Add(ref Array_View<int>_Dest, int index, int _Value)
        {
            int orig = _Dest[index];
            _Dest[index] = _Dest[index] + _Value;
            return orig;
        }

        static public void Copy<T>(Array_View<T> src, ref T[] dest)
        {
            for (int i = 0; i < dest.Length; ++i)
                dest[i] = src[i];
        }

        static public void Copy<T>(Array<T> src, ref T[] dest)
        {
            Array_View<T> from = src.Section(0, src.Extent.Size());
            for (int i = 0; i < dest.Length; ++i)
                dest[i] = from[i];
        }

        static public void AnalyzeThisAssembly()
        {
            System.Diagnostics.StackTrace stack_trace = new System.Diagnostics.StackTrace(true);
            System.Diagnostics.StackFrame stack_frame = stack_trace.GetFrame(1);
            System.Reflection.Assembly assembly = stack_frame.GetMethod().DeclaringType.Assembly;
            CFG control_flow_graph = CFG.Singleton(Analysis.Singleton());
            control_flow_graph.AddAssembly(assembly);
            control_flow_graph.ExtractBasicBlocks();
        }

        static public void Parallel_For_Each(Extent extent, _Kernel_type _kernel)
        {
            Accelerator_View view = new Accelerator_View();
            Parallel_For_Each(view, extent, _kernel);
        }

        static public void Parallel_For_Each(Accelerator_View view, Extent extent, _Kernel_type _kernel)
        {
            // Compile and link any "to do" work before any DLL loading.
            builder.Make();

            Structure structure = Analysis.Singleton().FindAllTargets(_kernel);

            // Get corresponding Campy code for C# kernel.
            Type thunk = GetThunk(_kernel, extent, structure);

            // Create thunk object.
            object obj = Activator.CreateInstance(thunk);

            // Set fields of thunk based on lambda.
            CopyFieldsFromHostToStaging(_kernel, structure, ref obj);

            // Set extent.
            CopyExtentToStaging(extent, ref obj);

            // Set extent.
            CopyViewToStaging(view, ref obj);

            // Get address of thunk method.
            SR.MethodInfo mi2 = thunk.GetMethod(Utility.NormalizeSystemReflectionName(_kernel.Method.Name));

            // Call thunk method.
            mi2.Invoke(obj, new object[] { });
        }

        static public void Parallel_For_Each(Tiled_Extent extent, _Kernel_tiled_type _kernel)
        {
            Accelerator_View view = new Accelerator_View();
            Parallel_For_Each(view, extent, _kernel);
        }

        static public void Parallel_For_Each(Accelerator_View view, Tiled_Extent extent, _Kernel_tiled_type _kernel)
        {
            // Compile and link any "to do" work before any DLL loading.
            builder.Make();

            Structure structure = Analysis.Singleton().FindAllTargets(_kernel);

            // Get corresponding Campy code for C# kernel.
            Type thunk = GetThunk(_kernel, extent, structure);

            // Create thunk object.
            object obj = Activator.CreateInstance(thunk);

            // Set fields of thunk based on lambda.
            CopyFieldsFromHostToStaging(_kernel, structure, ref obj);

            // Set extent.
            CopyExtentToStaging(extent, ref obj);

            // Set extent.
            CopyViewToStaging(view, ref obj);

            // Get address of thunk method.
            SR.MethodInfo mi2 = thunk.GetMethod(Utility.NormalizeSystemReflectionName(_kernel.Method.Name));

            // Call thunk method.
            mi2.Invoke(obj, new object[] { });
        }

        private static Type GetThunk(Delegate kernel, Campy.Types.Extent extent, Structure structure)
        {
            // Get MethodInfo for lambda.
            SR.MethodInfo mi = kernel.Method;
            object target = kernel.Target;
            Type target_type = target.GetType();
            String target_type_name = target_type.FullName;
            target_type_name = Utility.NormalizeSystemReflectionName(target_type_name);

            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = mi.DeclaringType.Assembly.Location;

            // Get directory containing the assembly.
            String full_path = Path.GetFullPath(kernel_assembly_file_name);
            full_path = Path.GetDirectoryName(full_path);

            // Get full name of kernel, including normalization because they cannot be compared directly with Mono.Cecil names.
            String kernel_full_name = string.Format("{0} {1}.{2}({3})", mi.ReturnType.FullName, Utility.RemoveGenericParameters(mi.ReflectedType), mi.Name, string.Join(",", mi.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
            kernel_full_name = Utility.NormalizeSystemReflectionName(kernel_full_name) + "_managed";

            // Get short name of Campy kernel.
            String campy_kernel_class_short_name = target_type_name
                + "_managed";

            // Derive name of assembly containing corresponding Campy code for lambda.
            String campy_assembly_file_name = full_path + "\\" + kernel_full_name;
            //String ext = Path.GetExtension(campy_assembly_file_name);
            //campy_assembly_file_name = campy_assembly_file_name.Replace(ext, "");
            campy_assembly_file_name = campy_assembly_file_name + "_aux";
            campy_assembly_file_name = campy_assembly_file_name + ".dll";

            bool rebuild = false;
            SR.Assembly dll = null;
            Type thunk = null;

            // Determine if this campy assembly has been seen before.
            Assembly assembly = null;
            bool found = assemblies.TryGetValue(campy_assembly_file_name, out assembly);
            if (!found)
            {
                assembly = new Assembly(campy_assembly_file_name);
                assemblies.Add(campy_assembly_file_name, assembly);
            }

            // Create app domain in order to test the dll.
            //SR.AssemblyName assemblyName = new SR.AssemblyName();
            //assemblyName.CodeBase = assembly.Name;
            try
            {
                dll = SR.Assembly.LoadFile(campy_assembly_file_name);
            }
            catch
            {
                rebuild = true;
            }


            // Determine if this kernel has been executed before.
            if (!assembly.executed_lambdas.Contains(kernel_full_name))
            {
                // Check timestamps.
                if (!rebuild)
                {
                    DateTime dt_kernel_assembly = File.GetLastWriteTime(kernel_assembly_file_name);
                    DateTime dt_campy_kernel_assembly = File.GetLastWriteTime(campy_assembly_file_name);
                    if (dt_campy_kernel_assembly < dt_kernel_assembly)
                        rebuild = true;
                }

                if (!rebuild)
                {
                    //dll = dom.Load(assemblyName);

                    // Get address of thunk class corresponding to lambda.
                    thunk = dll.GetType(kernel_full_name);
                    if (thunk == null)
                    {
                        rebuild = true;
                    }
                }

                if (rebuild)
                {
                    Converter converter = new Converter(assembly);

                    // Convert lambda into GPU target code.
                    converter.Convert(kernel, extent, structure);

                    // Compile target code into object code.
                    builder.Compile(assembly);

                    // Link object code.
                    builder.Link(assembly);
                }

                // Note that lambda was generated and compiled.
                assembly.executed_lambdas.Add(kernel_full_name);
            }

            // Load/reload assembly.
            //dll = dom.Load(assemblyName);
            dll = SR.Assembly.LoadFile(campy_assembly_file_name);

            // Get address of thunk class corresponding to lambda.
            thunk = dll.GetType(kernel_full_name);

            return thunk;
        }

        private static void AssignmentManagedStruct(Structure structure, ref object staging_object)
        {
            Type s = staging_object.GetType();
            SR.FieldInfo[] sfi = s.GetFields();
            foreach (SR.FieldInfo fi in structure.simple_fields)
            {
                object field_value = fi.GetValue(structure._class_instance);
                String na = fi.Name;
                String tys = Utility.GetFriendlyTypeName(fi.FieldType);
                // Copy.
                SR.FieldInfo hostObjectField = fi;
                object value = field_value;
                // Never copy tile_statics...
                if (TypesUtility.IsCampyTileStaticType(fi.FieldType))
                    continue;
                var deviceObjectField = sfi.Where(f => f.Name == fi.Name).FirstOrDefault();
                if (deviceObjectField == null)
                    throw new ArgumentException("Field not found.");
                if (field_value != null)
                {
                    deviceObjectField.SetValue(staging_object, value);
                }
                else
                {
                    // In order to prevent a lot of segv's ...
                    // Create a default value based on field type?
                    if (TypesUtility.IsCampyArrayViewType(fi.FieldType))
                    {
                        deviceObjectField.SetValue(staging_object, Array_View<int>.Default_Value);
                    }
                    else if (TypesUtility.IsCampyArrayType(fi.FieldType))
                    {
                        deviceObjectField.SetValue(staging_object, Array<int>.Default_Value);
                    }
                    else if (TypesUtility.IsCampyAcceleratorType(fi.FieldType))
                    {
                        deviceObjectField.SetValue(staging_object, Accelerator.Default_Value);
                    }
                    else if (TypesUtility.IsCampyAcceleratorViewType(fi.FieldType))
                    {
                        deviceObjectField.SetValue(staging_object, Accelerator_View.Default_Value);
                    }
                    else if (TypesUtility.IsCampyIndexType(fi.FieldType))
                    {
                        deviceObjectField.SetValue(staging_object, Index.Default_Value);
                    }
                    else if (TypesUtility.IsCampyExtentType(fi.FieldType))
                    {
                        deviceObjectField.SetValue(staging_object, Extent.Default_Value);
                    }
                    else
                    {
                    }
                }
            }
            // Add in other structures.
            foreach (Structure child in structure.nested_structures)
            {
                SR.FieldInfo sn = sfi.Where(f => child.Name == f.Name).FirstOrDefault();
                if (sn == null)
                    throw new ArgumentException("Field not found.");
                object vsn = sn.GetValue(staging_object);
                if (vsn == null)
                    throw new ArgumentException("Value not found.");
                AssignmentManagedStruct(child, ref vsn);
            }
        }

        private static void CopyFieldsFromHostToStaging(
            System.Delegate del,
            Structure structure,
            ref object staging_object)
        {
            Type s = staging_object.GetType();
            SR.FieldInfo[] sfi = s.GetFields();
            SR.FieldInfo s1 = sfi.Where(f => "s1" == f.Name).FirstOrDefault();
            if (s1 == null)
                throw new ArgumentException("Field not found.");
            object vs1 = s1.GetValue(staging_object);
            if (vs1 == null)
                throw new ArgumentException("Value not found.");

            AssignmentManagedStruct(structure, ref vs1);
        }

        private static void CopyViewToStaging(Accelerator_View view, ref object staging_object)
        {
            Type s = staging_object.GetType();
            SR.FieldInfo[] sfi = s.GetFields();

            bool found = false;
            foreach (SR.FieldInfo field in sfi)
            {
                if (field.Name.Equals("accelerator_view"))
                {
                    if (found)
                        throw new Exception("CopyViewToStaging encountered an internal error--found duplicate accelerator_view field in managed object.");
                    found = true;
                    field.SetValue(staging_object, view);
                }
            }
            if (!found)
                throw new Exception("CopyViewToStaging encountered an internal error--did not find accelerator_view field in managed object.");
        }

        private static void CopyExtentToStaging(Extent extent, ref object staging_object)
        {
            Type s = staging_object.GetType();
            SR.FieldInfo[] sfi = s.GetFields();

            bool found = false;
            foreach (SR.FieldInfo field in sfi)
            {
                if (field.Name.Equals("extent"))
                {
                    if (found)
                        throw new Exception("CopyExtentToStaging encountered an internal error--found duplicate extent field in managed object.");
                    found = true;
                    field.SetValue(staging_object, extent);
                }
            }
            if (!found)
                throw new Exception("CopyExtentToStaging encountered an internal error--did not find extent field in managed object.");
        }

        private static void CopyExtentToStaging(Tiled_Extent extent, ref object staging_object)
        {
            Type s = staging_object.GetType();
            SR.FieldInfo[] sfi = s.GetFields();

            bool found = false;
            foreach (SR.FieldInfo field in sfi)
            {
                if (field.Name.Equals("extent"))
                {
                    if (found)
                        throw new Exception("CopyExtentToStaging encountered an internal error--found duplicate extent field in managed object.");
                    found = true;
                    field.SetValue(staging_object, extent);
                }
            }
            if (!found)
                throw new Exception("CopyExtentToStaging encountered an internal error--did not find extent field in managed object.");
        }
    }
}
