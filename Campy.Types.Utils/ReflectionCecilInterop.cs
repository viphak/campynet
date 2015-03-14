using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Campy.Utils;

namespace Campy.Types.Utils
{
    public class ReflectionCecilInterop
    {
        public static Mono.Cecil.ModuleDefinition GetMonoCecilModuleDefinition(System.Delegate del)
        {
            System.Reflection.MethodInfo mi = del.Method;

            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = mi.DeclaringType.Assembly.Location;

            // Decompile entire module.
            Mono.Cecil.ModuleDefinition md = Mono.Cecil.ModuleDefinition.ReadModule(kernel_assembly_file_name);
            return md;
        }

        public static Mono.Cecil.MethodDefinition ConvertToMonoCecilMethodDefinition(System.Reflection.MethodInfo mi)
        {
            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = mi.DeclaringType.Assembly.Location;

            // Get directory containing the assembly.
            String full_path = Path.GetFullPath(kernel_assembly_file_name);
            full_path = Path.GetDirectoryName(full_path);

            // Get full name of kernel, including normalization because they cannot be compared directly with Mono.Cecil names.
            String kernel_full_name = string.Format("{0} {1}.{2}({3})", mi.ReturnType.FullName, Campy.Utils.Utility.RemoveGenericParameters(mi.ReflectedType), mi.Name, string.Join(",", mi.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
            kernel_full_name = Campy.Utils.Utility.NormalizeSystemReflectionName(kernel_full_name);

            // Decompile entire module.
            Mono.Cecil.ModuleDefinition md = Mono.Cecil.ModuleDefinition.ReadModule(kernel_assembly_file_name);

            // Examine all types, and all methods of types in order to find the lambda in Mono.Cecil.
            List<Type> types = new List<Type>();
            StackQueue<Mono.Cecil.TypeDefinition> type_definitions = new StackQueue<Mono.Cecil.TypeDefinition>();
            StackQueue<Mono.Cecil.TypeDefinition> type_definitions_closure = new StackQueue<Mono.Cecil.TypeDefinition>();
            foreach (Mono.Cecil.TypeDefinition td in md.Types)
            {
                type_definitions.Push(td);
            }
            while (type_definitions.Count > 0)
            {
                Mono.Cecil.TypeDefinition ty = type_definitions.Pop();
                type_definitions_closure.Push(ty);
                foreach (Mono.Cecil.TypeDefinition ntd in ty.NestedTypes)
                    type_definitions.Push(ntd);
            }
            foreach (Mono.Cecil.TypeDefinition td in type_definitions_closure)
            {
                foreach (Mono.Cecil.MethodDefinition md2 in td.Methods)
                {
                    String md2_name = Campy.Utils.Utility.NormalizeMonoCecilName(md2.FullName);
                    if (md2_name.Contains(kernel_full_name))
                        return md2;
                }
            }
            return null;
        }

        public static Mono.Cecil.TypeDefinition ConvertToMonoCecilTypeDefinition(Type ty)
        {
            // Get assembly name which encloses code for kernel.
            String kernel_assembly_file_name = ty.Assembly.Location;

            // Get directory containing the assembly.
            String full_path = Path.GetFullPath(kernel_assembly_file_name);
            full_path = Path.GetDirectoryName(full_path);

            // Decompile entire module.
            Mono.Cecil.ModuleDefinition md = Mono.Cecil.ModuleDefinition.ReadModule(kernel_assembly_file_name);

            // Examine all types, and all methods of types in order to find the lambda in Mono.Cecil.
            List<Type> types = new List<Type>();
            StackQueue<Mono.Cecil.TypeDefinition> type_definitions = new StackQueue<Mono.Cecil.TypeDefinition>();
            StackQueue<Mono.Cecil.TypeDefinition> type_definitions_closure = new StackQueue<Mono.Cecil.TypeDefinition>();
            foreach (Mono.Cecil.TypeDefinition td in md.Types)
            {
                type_definitions.Push(td);
            }
            while (type_definitions.Count > 0)
            {
                Mono.Cecil.TypeDefinition td = type_definitions.Pop();
                if (Campy.Utils.Utility.IsSimilarType(ty, td))
                    return td;
                type_definitions_closure.Push(td);
                foreach (Mono.Cecil.TypeDefinition ntd in td.NestedTypes)
                    type_definitions.Push(ntd);
            }
            foreach (Mono.Cecil.TypeDefinition td in type_definitions_closure)
            {
                if (Campy.Utils.Utility.IsSimilarType(ty, td))
                    return td;
            }
            return null;
        }

        public static System.Reflection.MethodInfo ConvertToSystemReflectionMethodInfo(Mono.Cecil.MethodDefinition md)
        {
            String md_name = Campy.Utils.Utility.NormalizeMonoCecilName(md.FullName);
            // Get owning type.
            Mono.Cecil.TypeDefinition td = md.DeclaringType;
            Type t = ConvertToSystemReflectionType(td);
            foreach (System.Reflection.MethodInfo mi in t.GetMethods(System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static))
            {
                String full_name = string.Format("{0} {1}.{2}({3})", mi.ReturnType.FullName, Campy.Utils.Utility.RemoveGenericParameters(mi.ReflectedType), mi.Name, string.Join(",", mi.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
                full_name = Campy.Utils.Utility.NormalizeSystemReflectionName(full_name);
                if (md_name.Contains(full_name))
                    return mi;
            }
            return null;
        }

        public static Type ConvertToSystemReflectionType(Mono.Cecil.TypeReference tr)
        {
            Mono.Cecil.TypeDefinition td = tr.Resolve();
            return ConvertToSystemReflectionType(td);
        }

        public static Type ConvertToSystemReflectionType(Mono.Cecil.TypeDefinition td)
        {
            // Find equivalent to type definition in Mono to System Reflection type.

            // get module.
            String assembly_location = td.Module.FullyQualifiedName;
            System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFile(assembly_location);

            List<Type> types = new List<Type>();
            StackQueue<Type> type_definitions = new StackQueue<Type>();
            StackQueue<Type> type_definitions_closure = new StackQueue<Type>();
            foreach (Type t in assembly.GetTypes())
            {
                type_definitions.Push(t);
            }
            while (type_definitions.Count > 0)
            {
                Type t = type_definitions.Pop();
                if (Campy.Utils.Utility.IsSimilarType(t, td))
                    return t;
                type_definitions_closure.Push(t);
                foreach (Type ntd in t.GetNestedTypes())
                    type_definitions.Push(ntd);
            }
            foreach (Type t in type_definitions_closure)
            {
                if (Campy.Utils.Utility.IsSimilarType(t, td))
                    return t;
            }
            return null;
        }

        public static bool IsStruct(Type t)
        {
            return t.IsValueType && !t.IsPrimitive && !t.IsEnum;
        }

        public static bool IsStruct(Mono.Cecil.TypeReference t)
        {
            return t.IsValueType && !t.IsPrimitive;
        }

        class TypesEnumerator : IEnumerable<Mono.Cecil.TypeDefinition>
        {
            Mono.Cecil.ModuleDefinition _module;

            public TypesEnumerator(Mono.Cecil.ModuleDefinition module)
            {
                _module = module;
            }

            public IEnumerator<Mono.Cecil.TypeDefinition> GetEnumerator()
            {
                StackQueue<Mono.Cecil.TypeDefinition> type_definitions = new StackQueue<Mono.Cecil.TypeDefinition>();
                StackQueue<Mono.Cecil.TypeDefinition> type_definitions_closure = new StackQueue<Mono.Cecil.TypeDefinition>();
                foreach (Mono.Cecil.TypeDefinition td in _module.Types)
                {
                    type_definitions.Push(td);
                }
                while (type_definitions.Count > 0)
                {
                    Mono.Cecil.TypeDefinition td = type_definitions.Pop();
                    type_definitions_closure.Push(td);
                    foreach (Mono.Cecil.TypeDefinition ntd in td.NestedTypes)
                        type_definitions.Push(ntd);
                }
                foreach (Mono.Cecil.TypeDefinition td in type_definitions_closure)
                {
                    yield return td;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static IEnumerable<Mono.Cecil.TypeDefinition> GetTypes(Mono.Cecil.ModuleDefinition module)
        {
            return new TypesEnumerator(module);
        }


        class MethodsEnumerator : IEnumerable<Mono.Cecil.MethodDefinition>
        {
            Mono.Cecil.ModuleDefinition _module;

            public MethodsEnumerator(Mono.Cecil.ModuleDefinition module)
            {
                _module = module;
            }

            public IEnumerator<Mono.Cecil.MethodDefinition> GetEnumerator()
            {
                StackQueue<Mono.Cecil.TypeDefinition> type_definitions = new StackQueue<Mono.Cecil.TypeDefinition>();
                StackQueue<Mono.Cecil.TypeDefinition> type_definitions_closure = new StackQueue<Mono.Cecil.TypeDefinition>();
                foreach (Mono.Cecil.TypeDefinition td in _module.Types)
                {
                    type_definitions.Push(td);
                }
                while (type_definitions.Count > 0)
                {
                    Mono.Cecil.TypeDefinition td = type_definitions.Pop();
                    type_definitions_closure.Push(td);
                    foreach (Mono.Cecil.TypeDefinition ntd in td.NestedTypes)
                        type_definitions.Push(ntd);
                }
                foreach (Mono.Cecil.TypeDefinition type in type_definitions_closure)
                {
                    foreach (Mono.Cecil.MethodDefinition method in type.Methods)
                    {
                        yield return method;
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static IEnumerable<Mono.Cecil.MethodDefinition> GetMethods(Mono.Cecil.ModuleDefinition module)
        {
            return new MethodsEnumerator(module);
        }
    }
}
