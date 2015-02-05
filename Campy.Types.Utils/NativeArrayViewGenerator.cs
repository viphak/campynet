using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campy.Builder;
using Campy.Utils;

namespace Campy.Types.Utils
{
    public class NativeArrayViewGenerator
    {
        String eol = "\r\n";
        Assembly _assembly;
        Dictionary<System.Object, bool> compiled_targets = new Dictionary<object, bool>();
        Dictionary<String, MulticastDelegate> multicastdelegates = new Dictionary<string, MulticastDelegate>();
        static Build builder = new Build();
        static Dictionary<String, Assembly> assemblies = new Dictionary<String, Assembly>();

        public NativeArrayViewGenerator()
        {}

        public IntPtr Generate(
            Type target_type,
            int num_elements,
            int byte_size_of_element,
            IntPtr ptr,
            IntPtr representation)
        {
            // Get corresponding Campy code for C# kernel.
            Type thunk = GetThunk(target_type);

            // Create thunk object.
            object obj = Activator.CreateInstance(thunk);

            // Get address of thunk method.
            System.Reflection.MethodInfo mi = thunk.GetMethod("Doit");

            // Call thunk method.
            IntPtr result = (IntPtr)mi.Invoke(obj, new object[] {
                num_elements,
                byte_size_of_element,
                ptr,
                representation
            });

            return result;
        }

        private Type GetThunk(Type target_type)
        {
            //Type target_type = target.GetType();
            String target_type_name = "Native_Array_View<"
                +  Campy.Utils.Utility.NormalizeSystemReflectionName(target_type.Name)
                + ">";

            //// Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = Campy.Utils.Utility.NormalizeSystemReflectionName(target_type_name);

            //// Get directory containing the assembly.
            String full_path = Path.GetFullPath(kernel_assembly_file_name);
            full_path = Path.GetDirectoryName(full_path);

            //// Get full name of type, including normalization because they cannot be compared directly with Mono.Cecil names.
            String type_full_name = target_type_name;
            type_full_name = Campy.Utils.Utility.NormalizeSystemReflectionName(type_full_name);

            //// Get short name of Campy kernel.
            //String campy_kernel_class_short_name = target_type_name
            //    + "_managed";

            // Derive name of assembly containing corresponding Campy code for lambda.
            String campy_assembly_file_name = full_path + "\\" + type_full_name;
            //String ext = Path.GetExtension(campy_assembly_file_name);
            //campy_assembly_file_name = campy_assembly_file_name.Replace(ext, "");
            //campy_assembly_file_name = campy_assembly_file_name + "_aux";
            campy_assembly_file_name = campy_assembly_file_name + ".dll";

            bool rebuild = false;
            System.Reflection.Assembly dll = null;
            Type thunk = null;

            // Determine if this campy assembly has been seen before.
            Assembly assembly = null;
            bool found = assemblies.TryGetValue(campy_assembly_file_name, out assembly);
            if (!found)
            {
                assembly = new Assembly(campy_assembly_file_name);
                assemblies.Add(campy_assembly_file_name, assembly);
            }
            _assembly = assembly;

            try
            {
                dll = System.Reflection.Assembly.LoadFile(campy_assembly_file_name);
            }
            catch
            {
                rebuild = true;
            }


            // Determine if this kernel has been executed before.
            if (!assembly.executed_lambdas.Contains(target_type_name))
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
                    thunk = dll.GetType("Campy.Types.Create");
                    if (thunk == null)
                    {
                        rebuild = true;
                    }
                }

                if (rebuild)
                {
                    // Convert lambda into GPU target code.
                    Convert(target_type);

                    // Compile target code into object code.
                    builder.Compile(assembly);

                    // Link object code.
                    builder.Link(assembly);
                }

                // Note that lambda was generated and compiled.
                assembly.executed_lambdas.Add(target_type_name);
            }

            // Load/reload assembly.
            dll = System.Reflection.Assembly.LoadFile(campy_assembly_file_name);

            // Get address of thunk class corresponding to lambda.
            thunk = dll.GetType("Campy.Types.Create");

            return thunk;
        }

        void GenerateCode(Type type)
        {

            // Define the following code:
            // 1) A structure, defined in type, in C++ native.
            // 2) A type "Native_Array_View<type>", as a C++ native class.

            // Derive name of output files based on the name of the full name.
            // Get full name of kernel, including normalization because they cannot be compared directly with Mono.Cecil names.
            //String kernel_full_name = string.Format("{0} {1}.{2}({3})", del.Method.ReturnType.FullName, Utility.RemoveGenericParameters(del.Method.ReflectedType), del.Method.Name, string.Join(",", del.Method.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
            //kernel_full_name = Utility.NormalizeSystemReflectionName(kernel_full_name);
            String native_array_view_type_file_name_stem = "Native_Array_View<" + type.FullName + ">";
            native_array_view_type_file_name_stem = Campy.Utils.Utility.NormalizeSystemReflectionName(native_array_view_type_file_name_stem);

            String managed_cpp_native_array_view_type_file_name = native_array_view_type_file_name_stem + "_managed.cpp";
            String managed_h_native_array_view_type_file_name = native_array_view_type_file_name_stem + "_managed.cpp";
            String unmanaged_cpp_native_array_view_type_file_name = native_array_view_type_file_name_stem + "_unmanaged.cpp";
            String unmanaged_h_native_array_view_type_file_name = native_array_view_type_file_name_stem + "_unmanaged.h";

            String result = "";

            // Define structure in managed world.
            // This gets defined in managed DLL.
            // In actual fact, we really don't need this type, since
            // we only need to access the native array view from the kernel.
            // But, if we do need it, well here it is.
            //_assembly.managed_cpp_files.Add(
            //    Campy.Utils.Utility.NormalizeSystemReflectionName(type.FullName) + "_managed.cpp",
            //    CSCPP.ConvertToCPPCLIWithNameSpace(type, 0)
            //    );

            // Define structure in managed world.
            // This gets defined in managed DLL.
            // In actual fact, we really DO need this type, since
            // we have to define the type for C++ AMP.
            _assembly.unmanaged_h_files.Add(
                Campy.Utils.Utility.NormalizeSystemReflectionName(type.FullName) + "_unmanaged.h",
                CSCPP.ConvertToCPPWithNameSpace(type, 0)
                );

            // Now, define a class to instantiate a Native_Array_View, to link
            // the C# world with the C++ AMP world.
            result = @"

#include """ + Campy.Utils.Utility.NormalizeSystemReflectionName(type.FullName) + @"_unmanaged.h""
#using ""Campy.Types.dll""
#include ""Native_Array_View.h""

using namespace System;
using namespace Campy::Types;
";

            result += @"

namespace Campy {
	namespace Types {

        public ref class Create
        {
        public:
            Create() {}
            static IntPtr Doit(int num_elements, int byte_size_of_element, IntPtr ptr, IntPtr representation)
            {
                void * result = (void*)new Native_Array_View<" + type.FullName.Replace(".", "::") + @">(
                    num_elements,
                    byte_size_of_element,
                    (void*)ptr.ToPointer(),
                    (char*)representation.ToPointer());
                return IntPtr(result);
            }
        };
    }
}
";
            _assembly.managed_cpp_files.Add(managed_cpp_native_array_view_type_file_name, result);
            
            result = @"
#include <amp.h>
#include <iostream>
#include ""Native_Array_View.h""
#include """ + Campy.Utils.Utility.NormalizeSystemReflectionName(type.FullName) + @"_unmanaged.h""

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

        template<typename T>
        Native_Array_View<T>::Native_Array_View(int length, int element_length, void * data, char * representation)
        {
	        native = (void*) new array_view<T, 1>(length, (T*)data);
        }

        template<typename T>
        Native_Array_View<T>::Native_Array_View()
        {
	        native = (void*)0;
        }

        template<typename T>
        void Native_Array_View<T>::synchronize()
        {
	        // load unmanaged type and call synchronize.
	        ((array_view<T, 1>*)native)->synchronize();
        }

        template<typename T>
        void * Native_Array_View<T>::get(int i)
        {
	        return (void *)&((*(array_view<T, 1>*)native)[i]);
        }

        template<typename T>
        void Native_Array_View<T>::set(int i, void * value)
        {
	        (*(array_view<T, 1>*)native)[i] = *(T*) value;
        }

        template Native_Array_View<" + type.FullName.Replace(".", "::") + @">;
    }
}
";

            // (I should get source from Campy itself, but this will work, too.)
            _assembly.unmanaged_cpp_files.Add(unmanaged_cpp_native_array_view_type_file_name, result);

            result = @"
#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include ""Native_Array_View_Base.h""

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		Native_Array_View_Base::Native_Array_View_Base(int length, int element_length, void * data, char * representation)
		{
			native = (void*)data;
		}

		Native_Array_View_Base::Native_Array_View_Base()
		{
			native = (void*)0;
		}

		void Native_Array_View_Base::synchronize()
		{
		}

		void * Native_Array_View_Base::get(int i)
		{
			return 0;
		}

		void Native_Array_View_Base::set(int i, void * value)
		{
		}
	}
}
";
            
            _assembly.unmanaged_cpp_files.Add("Native_Array_View_Base.cpp", result);

        }

        public void Convert(Type type)
        {
            // Generate code files.
            GenerateCode(type);
        }
    }
}
