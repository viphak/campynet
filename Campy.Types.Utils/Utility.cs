using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Campy.Types.Utils
{
    public class Utility
    {
        /// <summary>
        /// Data class used by CreateBlittableType in order to create a blittable type
        /// corresponding to a host type.
        /// </summary>
        class Data
        {
            public AssemblyName assemblyName;
            public AssemblyBuilder ab;
            public ModuleBuilder mb;

            Data()
            {
                assemblyName = new AssemblyName("DynamicAssembly");
                ab = AppDomain.CurrentDomain.DefineDynamicAssembly(
                    assemblyName,
                    AssemblyBuilderAccess.RunAndSave);
                mb = ab.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
            }

            static Dictionary<Type, Data> assemblies = new Dictionary<Type, Data>();

            public static Data Find(Type t)
            {
                Data d = null;
                if (assemblies.TryGetValue(t, out d))
                    return d;
                d = new Data();
                assemblies.Add(t, d);
                return d;
            }
        }

        public static Type CreateBlittableType(Type hostType)
        {
            // Find if blittable type for hostType was already performed.
            TypeFilter tf = new TypeFilter((Type t, object o) =>
            {
                return t.Name == hostType.Name;
            });
            Type[] types = Data.Find(hostType).mb.FindTypes(tf, null);

            // If blittable type was not created, create one with all fields corresponding
            // to that in host, with special attention to arrays.
            if (types.Length == 0)
            {
                if (hostType.IsArray)
                {
                    // Recurse
                    Type elementType = CreateBlittableType(hostType.GetElementType());
                    object array_obj = Array.CreateInstance(elementType, 0);
                    Type array_type = array_obj.GetType();
                    TypeBuilder tb = null;
                    tb = Data.Find(array_type).mb.DefineType(
                        array_type.Name,
                        TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout
                            | TypeAttributes.Serializable, typeof(ValueType));
                    return tb.CreateType();
                }
                else if (IsStruct(hostType) || hostType.IsClass)
                {
                    TypeBuilder tb = null;
                    tb = Data.Find(hostType).mb.DefineType(
                        hostType.Name,
                        TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout
                            | TypeAttributes.Serializable, typeof(ValueType));
                    var fields = hostType.GetFields();
                    foreach (var field in fields)
                    {
                        if (field.FieldType.IsArray)
                        {
                            // Convert byte, int, etc., in host type to pointer in blittable type.
                            // With array, we need to also encode the length.
                            tb.DefineField(field.Name, typeof(IntPtr), FieldAttributes.Public);
                            tb.DefineField(field.Name + "Len0", typeof(Int32), FieldAttributes.Public);
                        }
                        else
                        {
                            // For non-array type fields, just define the field as is.
                            tb.DefineField(field.Name, field.FieldType, FieldAttributes.Public);
                        }
                    }
                    return tb.CreateType();
                }
                else return null;
            }
            else
                return types[0];
        }

        public static void CopyToBlittableType(object from, ref object to)
        {
            Type f = from.GetType();
            FieldInfo[] ffi = f.GetFields();
            Type t = to.GetType();
            FieldInfo[] tfi = t.GetFields();
            foreach (FieldInfo fi in ffi)
            {
                object field_value = fi.GetValue(from);
                String na = fi.Name;
                
                // Copy.
                var tfield = tfi.Where(k => k.Name == fi.Name).FirstOrDefault();
                if (tfield == null)
                    throw new ArgumentException("Field not found.");
                tfield.SetValue(to, field_value);
            }
        }

        public static void CopyFromBlittableType(object from, ref object to)
        {
            Type f = from.GetType();
            FieldInfo[] ffi = f.GetFields();
            Type t = to.GetType();
            FieldInfo[] tfi = t.GetFields();
            foreach (FieldInfo fi in ffi)
            {
                object field_value = fi.GetValue(from);
                String na = fi.Name;

                // Copy.
                var tfield = tfi.Where(k => k.Name == fi.Name).FirstOrDefault();
                if (tfield == null)
                    throw new ArgumentException("Field not found.");
                tfield.SetValue(to, field_value);
            }
        }

        public static void CopyFromPtrToBlittable(IntPtr ptr, object blittable_object)
        {
            Marshal.PtrToStructure(ptr, blittable_object);
        }

        public static IntPtr CreateNativeArray(Array from, Type blittable_element_type)
        {
            // Convert
            int size_element = Marshal.SizeOf(blittable_element_type);

            IntPtr mem = Marshal.AllocHGlobal(size_element * from.Length);
            IntPtr a = mem;

            for (int i = 0; i < from.Length; ++i)
            {
                // copy.
                object obj = Activator.CreateInstance(blittable_element_type);
                Campy.Types.Utils.Utility.CopyToBlittableType(from.GetValue(i), ref obj);
                Marshal.StructureToPtr(obj, mem, false);
                mem = new IntPtr((long)mem + size_element);
            }
            return a;
        }

        public static bool IsStruct(Type t)
        {
            return t.IsValueType && !t.IsPrimitive && !t.IsEnum;
        }

    }
}
