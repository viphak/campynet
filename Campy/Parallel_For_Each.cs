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

            // Set fields of thunk based on lambda.
            CopyFieldsFromHostToStaging(_kernel.Target, ref obj);

            // Set extent.
            CopyExtentToStaging(extent, ref obj);

            // Set extent.
            CopyViewToStaging(view, ref obj);

            // Get address of thunk method.
            SR.MethodInfo mi2 = thunk.GetMethod("Thunk");

            // Call thunk method.
            mi2.Invoke(obj, new object[] { });
        }

        private static Type GetThunk(_Kernel_type kernel)
        {
            // Get MethodInfo for lambda.
            SR.MethodInfo mi = kernel.Method;

            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = mi.DeclaringType.Assembly.Location;

            // Get directory containing the assembly.
            String full_path = Path.GetFullPath(kernel_assembly_file_name);
            full_path = Path.GetDirectoryName(full_path);

            // Get full name of kernel, including normalization because they cannot be compared directly with Mono.Cecil names.
            String kernel_full_name = string.Format("{0} {1}.{2}({3})", mi.ReturnType.FullName, mi.ReflectedType.FullName, mi.Name, string.Join(",", mi.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
            kernel_full_name = Utility.NormalizeSystemReflectionName(kernel_full_name);

            // Get short name of Campy kernel.
            String campy_kernel_class_short_name = kernel_full_name;
            campy_kernel_class_short_name = campy_kernel_class_short_name + "_managed";

            // Derive name of assembly containing corresponding Campy code for lambda.
            String campy_assembly_file_name = kernel_full_name;
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
            dll = SR.Assembly.LoadFile(full_path + "\\" + assembly.Name);

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
                    thunk = dll.GetType(campy_kernel_class_short_name);
                    if (thunk == null)
                    {
                        // Try to delete the app domain, but it usually the dll doesn't unload.
                        //AppDomain.Unload(dom);
                        //var hMod = IntPtr.Zero;
                        //LoadLibraryA(assembly.Name);
                        //if (GetModuleHandleExA(0, assembly.Name, ref hMod))
                        //{
                        //    while (FreeLibrary(hMod))
                        //    { }
                        //}
                        //dom = AppDomain.CreateDomain("something");
                        //dll = null;
                        rebuild = true;
                        // Force Dll unload.
                    }
                }

                if (rebuild)
                {
                    // Rebuild....

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
                    MethodDefinition lambda_method = null;
                    foreach (TypeDefinition td in type_definitions_closure)
                    {
                        foreach (MethodDefinition md2 in td.Methods)
                        {
                            String md2_name = Utility.NormalizeMonoCecilName(md2.FullName);
                            if (md2_name.Contains(kernel_full_name))
                                lambda_method = md2;
                        }
                    }

                    // lambda_method is the delegate. Find the enclosing type.
                    TypeDefinition closure = lambda_method.DeclaringType;

                    String check_managed_closure_name = Utility.NormalizeSystemReflectionName(kernel_full_name);
                    check_managed_closure_name = Utility.NormalizeMonoCecilName(check_managed_closure_name);
                    check_managed_closure_name = check_managed_closure_name + "_managed";

                    // Make sure it's the same as short name of lambda.
                    if (!check_managed_closure_name.Equals(campy_kernel_class_short_name))
                        throw new Exception("Name mismatch.");

                    Converter converter = new Converter(assembly);

                    // Convert lambda into GPU target code.
                    converter.Convert(lambda_method);

                    // Compile target code into object code.
                    builder.Compile(assembly);
                    builder.Link(assembly);
                }

                // Note that lambda was generated and compiled.
                assembly.executed_lambdas.Add(kernel_full_name);
            }

            // Load/reload assembly.
            //dll = dom.Load(assemblyName);
            //dll = SR.Assembly.LoadFile(assembly.Name);

            // Get address of thunk class corresponding to lambda.
            thunk = dll.GetType(campy_kernel_class_short_name);

            return thunk;
        }

        private static void CopyFieldsFromHostToStaging(object host_object, ref object staging_object)
        {
            Type t = host_object.GetType();
            SR.FieldInfo[] tfi = t.GetFields();

            Type s = staging_object.GetType();
            SR.FieldInfo[] sfi = s.GetFields();

            foreach (var field in tfi)
            {
                if (field.FieldType.IsArray)
                {
                    SR.FieldInfo hostObjectField = field;
                    var deviceObjectField = sfi.Where(f => f.Name == hostObjectField.Name).FirstOrDefault();
                    if (deviceObjectField == null)
                        throw new ArgumentException("Field not found.");

                    // Get array and copy to the device.
                    Array hostArray = (Array)hostObjectField.GetValue(host_object);
                }
                else
                {
                    SR.FieldInfo hostObjectField = field;

                    var deviceObjectField = sfi.Where(f => f.Name == field.Name).FirstOrDefault();
                    if (deviceObjectField == null)
                        throw new ArgumentException("Field not found.");

                    object value = hostObjectField.GetValue(host_object);
                    deviceObjectField.SetValue(staging_object, value);
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
