using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.PE;
using SR = System.Reflection;
using Campy.Types;
using Campy.Utils;
using System.IO;
using System.Runtime.InteropServices;

namespace Campy
{
    public class Parallel_For_Each
    {
        [DllImport("kernel32.dll")]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        static extern bool LoadLibraryA(string hModule);

        [DllImport("kernel32.dll")]
        static extern bool GetModuleHandleExA(int dwFlags, string ModuleName, ref IntPtr phModule);

        static Builder builder = new Builder();
        static Dictionary<String, Assembly> assemblies = new Dictionary<String, Assembly>();

        public delegate void _Kernel_type(Index idx);

        static public void loop(Extent extent, _Kernel_type _kernel)
        {
            Accelerator_View view = new Accelerator_View();
            loop(view, extent, _kernel);
        }

        static public void loop(Accelerator_View view, Extent extent, _Kernel_type _kernel)
        {
            // Compile and link any "to do" work before any DLL loading.
            builder.Build();

            // Get corresponding Campy code for C# kernel.
            Type thunk = GetThunk(_kernel);

            // Create thunk object.
            object obj = Activator.CreateInstance(thunk);

            List<Delegate> delegates = Analysis.FindAllTargets(_kernel);

            // Set fields of thunk based on lambda.
            CopyFieldsFromHostToStaging(delegates, ref obj);

            // Set extent.
            CopyExtentToStaging(extent, ref obj);

            // Set extent.
            CopyViewToStaging(view, ref obj);

            // Get address of thunk method.
            SR.MethodInfo mi2 = thunk.GetMethod(Utility.NormalizeSystemReflectionName(_kernel.Method.Name));

            // Call thunk method.
            mi2.Invoke(obj, new object[] { });
        }

        private static Type GetThunk(_Kernel_type kernel)
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
            String kernel_full_name = string.Format("{0} {1}.{2}({3})", mi.ReturnType.FullName, mi.ReflectedType.FullName, mi.Name, string.Join(",", mi.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
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
                    converter.Convert(kernel);

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

        private static void CopyFieldsFromHostToStaging(List<Delegate> delegates, ref object staging_object)
        {
            Type s = staging_object.GetType();
            SR.FieldInfo[] sfi = s.GetFields();
            // Collect all information from fields of each delegate and pass to managed object.
            foreach (System.Delegate del in delegates)
            {
                if (del.Target != null)
                {
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
                        // Copy.
                            SR.FieldInfo hostObjectField = fi;

                            object value = field_value;
                            Type ft = value.GetType();
                            if (value as System.MulticastDelegate != null)
                                continue;

                            var deviceObjectField = sfi.Where(f => f.Name == fi.Name).FirstOrDefault();
                            if (deviceObjectField == null)
                                throw new ArgumentException("Field not found.");

                            deviceObjectField.SetValue(staging_object, value);
                    }
                }
            }
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
    }
}
