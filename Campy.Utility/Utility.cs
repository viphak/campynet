using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace Campy.Utils
{
    public class Utility
    {

        /// <summary>
        /// C# .NET really does not provide any API to get a "user friendly" name of a type,
        /// especially generics. The function Simplify and GetFriendlyTypeName fill in that gap.
        /// The prefix for a type, e.g., "System.Collections." is removed only if it is in the
        /// System namespace.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Simplify(String str)
        {
            if (str.Equals("System.Boolean"))
                return "bool";
            if (str.Equals("System.Byte"))
                return "byte";
            if (str.Equals("System.Char"))
                return "char";
            if (str.Equals("System.Decimal"))
                return "decimal";
            if (str.Equals("System.Double"))
                return "double";
            if (str.Equals("System.Single"))
                return "float";
            if (str.Equals("System.Int32"))
                return "int";
            if (str.Equals("System.Int64"))
                return "long";
            if (str.Equals("System.SByte"))
                return "sbyte";
            if (str.Equals("System.Int16"))
                return "short";
            if (str.Equals("System.UInt32"))
                return "uint";
            if (str.Equals("System.UInt64"))
                return "ulong";
            if (str.Equals("System.UInt16"))
                return "ushort";
            if (str.IndexOf("System.") == 0)
                return str.Substring(1 + str.LastIndexOf("."));
            if (str.IndexOf("Campy.Types.") == 0)
                return str.Substring(1 + str.LastIndexOf("."));
            str = str.Replace('+', '.');
            return str;
        }

        /// <summary>
        /// C# .NET really does not provide any API to get a "user friendly" name of a type,
        /// especially generics. The function Simplify and GetFriendlyTypeName fill in that gap.
        /// The prefix for a type, e.g., "System.Collections." is removed only if it is in the
        /// System namespace.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericParameter)
            {
                return Simplify(type.Name);
            }

            if (!type.IsGenericType)
            {
                return Simplify(type.FullName);
            }

            StringBuilder builder = new StringBuilder();
            String name = Simplify(type.Name);

            // If generic, then a backtick occurs in the name. In that case, remove the trailing information.
            String pre;
            int index = name.IndexOf("`");
            if (index >= 0)
                pre = String.Format("{0}.{1}", type.Namespace, Simplify(name.Substring(0, index)));
            else
                pre = String.Format("{0}.{1}", type.Namespace, Simplify(name));
            pre = Simplify(pre);
            builder.Append(pre);
            builder.Append('<');
            bool first = true;
            foreach (Type arg in type.GetGenericArguments())
            {
                if (!first)
                {
                    builder.Append(',');
                }
                builder.Append(GetFriendlyTypeName(arg));
                first = false;
            }
            builder.Append('>');
            // Convert "+" signs into "." since it's just a nested class.
            String result = builder.ToString();
            result = result.Replace('+', '.');
            return result;
        }

        public static String RemoveGenericParameters(Type type)
        {
            String result = type.FullName;
            result = Regex.Replace(result, "\\[\\[.*\\]\\]", "");
            return result;
        }

        /// <summary>
        /// This function is a mostly a hack. It substitutes underscores for any characters
        /// in a string so that it can be used as a variable in C++ AMP code, and for file naming.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String NormalizeSystemReflectionName(String name)
        {
            String result = name;
            result = result.Replace(" ", "_");
            result = result.Replace("<", "_");
            result = result.Replace(">", "_");
            result = result.Replace("::", "_");
            result = result.Replace("/", "_");
            result = result.Replace("+", "_");
            result = result.Replace("(", "_");
            result = result.Replace(")", "_");
            result = result.Replace(".", "_");
            result = result.Replace("`", "_");
            return result;
        }

        public static String NormalizeMonoCecilName(String name)
        {
            String result = name;
            result = result.Replace(" ", "_");
            result = result.Replace("<", "_");
            result = result.Replace(">", "_");
            result = result.Replace("::", "_");
            result = result.Replace("/", "_");
            result = result.Replace("+", "_");
            result = result.Replace("(", "_");
            result = result.Replace(")", "_");
            result = result.Replace(".", "_");
            result = result.Replace("`", "_");
            return result;
        }


        /// <summary>
        /// Check whether extension is recognized associated with a program.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        static public bool IsRecognizedExtension(string ext)
        {
            List<string> progs = new List<string>();
            string baseKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" + ext;
            using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(baseKey + @"\OpenWithList"))
            {
                if (rk != null)
                {
                    string mruList = (string)rk.GetValue("MRUList");
                    if (mruList != null)
                    {
                        foreach (char c in mruList.ToString())
                            if (rk.GetValue(c.ToString()) != null)
                                progs.Add(rk.GetValue(c.ToString()).ToString());
                    }
                }
            }
            using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(baseKey + @"\OpenWithProgids"))
            {
                if (rk != null)
                {
                    foreach (string item in rk.GetValueNames())
                        progs.Add(item);
                }
            }
            return progs.Count > 0;
        }

        public static String GetVersionFromRegistry()
        {
            // Opens the registry key for the .NET Framework entry. 
            using (RegistryKey ndpKey =
                RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").
                OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
            {
                // As an alternative, if you know the computers you will query are running .NET Framework 4.5  
                // or later, you can use: 
                // using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,  
                // RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
                foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                {
                    if (versionKeyName.StartsWith("v"))
                    {

                        RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);
                        string name = (string)versionKey.GetValue("Version", "");
                        string sp = versionKey.GetValue("SP", "").ToString();
                        string install = versionKey.GetValue("Install", "").ToString();
                        if (install == "") //no install info, must be later.
                            Console.WriteLine(versionKeyName + "  " + name);
                        else
                        {
                            if (sp != "" && install == "1")
                            {
                                Console.WriteLine(versionKeyName + "  " + name + "  SP" + sp);
                            }

                        }
                        if (name != "")
                        {
                            continue;
                        }
                        foreach (string subKeyName in versionKey.GetSubKeyNames())
                        {
                            RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
                            name = (string)subKey.GetValue("Version", "");
                            if (name != "")
                                sp = subKey.GetValue("SP", "").ToString();
                            install = subKey.GetValue("Install", "").ToString();
                            if (install == "") //no install info, must be later.
                                Console.WriteLine(versionKeyName + "  " + name);
                            else
                            {
                                if (sp != "" && install == "1")
                                {
                                    Console.WriteLine("  " + subKeyName + "  " + name + "  SP" + sp);
                                }
                                else if (install == "1")
                                {
                                    Console.WriteLine("  " + subKeyName + "  " + name);
                                }

                            }

                        }

                    }
                }
            }
            return "";
        }

    }
}
