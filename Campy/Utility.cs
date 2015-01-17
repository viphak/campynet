using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campy
{
    class Utility
    {
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
    }
}
