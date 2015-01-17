using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Campy
{
    class Builder
    {
        public Builder()
        {
        }

        public void Build()
        {
            // Load "Parallel_For_Each.todo"
            String file_name = "Parallel_For_Each.todo";
            if (File.Exists(file_name))
            {
                bool failed = false;
                string[] lines = System.IO.File.ReadAllLines("Parallel_For_Each.todo");
                foreach (string line in lines)
                {
                    // Compile and link all work.
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.FileName = "link.exe";
                    p.StartInfo.Arguments = line;
                    p.Start();
                    p.WaitForExit();
                    string output = p.StandardOutput.ReadToEnd();
                    if (p.ExitCode > 0)
                    {
                        failed = true;
                        System.Console.WriteLine("Link faild: " + output);
                    }
                }
                File.Delete("Parallel_For_Each.todo");
                if (failed)
                    throw new Exception();
            }
        }

        public void Compile(Assembly assembly)
        {
            bool do_write = true;

            // Create files.
            if (do_write)
            {
                foreach (KeyValuePair<String, String> kvp in assembly.managed_cpp_files)
                    System.IO.File.WriteAllText(kvp.Key, kvp.Value);
                foreach (KeyValuePair<String, String> kvp in assembly.unmanaged_cpp_files)
                    System.IO.File.WriteAllText(kvp.Key, kvp.Value);
                foreach (KeyValuePair<String, String> kvp in assembly.managed_h_files)
                    System.IO.File.WriteAllText(kvp.Key, kvp.Value);
                foreach (KeyValuePair<String, String> kvp in assembly.unmanaged_h_files)
                    System.IO.File.WriteAllText(kvp.Key, kvp.Value);
            }

            foreach (KeyValuePair<String, String> kvp in assembly.managed_cpp_files)
            {
                String cpp_source_file_name = kvp.Key;
                String pch_file_name = assembly.Name;
                String ext = Path.GetExtension(pch_file_name);
                pch_file_name = pch_file_name.Replace(ext, "");
                pch_file_name = pch_file_name + ".pch";

                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = "cl.exe";
                p.StartInfo.Arguments =
                    "/c"
                    + " /I\"C:\\Program Files (x86)\\Microsoft Visual Studio 12.0\\VC\\INCLUDE\""
                    + " /I\"C:\\Program Files (x86)\\Microsoft Visual Studio 12.0\\VC\\ATLMFC\\INCLUDE\""
                    + " /I\"C:\\Program Files (x86)\\Windows Kits\\8.1\\include\\shared\""
                    + " /I\"C:\\Program Files (x86)\\Windows Kits\\8.1\\include\\um\""
                    + " /I\"C:\\Program Files (x86)\\Windows Kits\\8.1\\include\\winrt\""
                    + " /GS"
                    + " /GR"
                    + " /analyze-"
                    + " /W3"
                    + " /Zc:wchar_t"
                    + " /Zi"
                    + " /Od"
                    + " /sdl"
                    + " /Fd\"vc120.pdb\""
                    + " /D \"WIN32\""
                    + " /D \"_DEBUG\""
                    + " /D \"_WINDLL\""
                    + " /D \"_UNICODE\""
                    + " /D \"UNICODE\""
                    + " /errorReport:prompt"
                    + " /FU\"C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.5\\mscorlib.dll\""
                    + " /FU\"C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.5\\System.Data.dll\""
                    + " /FU\"C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.5\\System.dll\""
                    + " /FU\"C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.5\\System.Xml.dll\""
                    + " /WX-"
                    + " /Zc:forScope"
                    + " /clr"
                    + " /Gd"
                    + " /Oy-"
                    + " /MDd"
                    + " /EHa"
                    + " /Fp\"" + pch_file_name + "\""
                    + " /nologo"
                    + " " + cpp_source_file_name;
                p.Start();
                p.WaitForExit();
                string output = p.StandardOutput.ReadToEnd();
                if (p.ExitCode > 0)
                {
                    System.Console.WriteLine("Compile failed: " + output);
                    throw new Exception("Compile failed.");
                }
            }

            foreach (KeyValuePair<String, String> kvp in assembly.unmanaged_cpp_files)
            {
                String cpp_source_file_name = kvp.Key;

                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = "cl.exe";
                p.StartInfo.Arguments =
                    "/c"
                    + " /I\"H:\\ParallelFor.GPU\\Campy.NET\\Campy.Types\""
                    + " /I\"C:\\Program Files (x86)\\Microsoft Visual Studio 12.0\\VC\\INCLUDE\""
                    + " /I\"C:\\Program Files (x86)\\Microsoft Visual Studio 12.0\\VC\\ATLMFC\\INCLUDE\""
                    + " /I\"C:\\Program Files (x86)\\Windows Kits\\8.1\\include\\shared\""
                    + " /I\"C:\\Program Files (x86)\\Windows Kits\\8.1\\include\\um\""
                    + " /I\"C:\\Program Files (x86)\\Windows Kits\\8.1\\include\\winrt\""
                    + " /GS"
                    + " /analyze-"
                    + " /W3"
                    + " /Zc:wchar_t"
                    + " /ZI"
                    + " /Gm"
                    + " /Od"
                    + " /sdl"
                    + " /Fd\"vc120.pdb\""
                    + " /D \"WIN32\""
                    + " /D \"_DEBUG\""
                    + " /D \"_CONSOLE\""
                    + " /D \"_LIB\""
                    + " /D \"_UNICODE\""
                    + " /D \"UNICODE\""
                    + " /errorReport:prompt"
                    + " /WX-"
                    + " /Zc:forScope"
                    + " /RTC1"
                    + " /Gd"
                    + " /Oy-"
                    + " /MDd"
                    + " /EHsc"
                    + " /nologo"
                    + " " + cpp_source_file_name;
                p.Start();
                p.WaitForExit();
                string output = p.StandardOutput.ReadToEnd();
                if (p.ExitCode > 0)
                {
                    System.Console.WriteLine("Compile failed: " + output);
                    throw new Exception("Compile failed.");
                }
            }
        }

        public void Link(Assembly assembly)
        {
            String pdb_file_name = assembly.Name;
            pdb_file_name = pdb_file_name.Replace(Path.GetExtension(assembly.Name), "");
            pdb_file_name = pdb_file_name + ".pdb";

            String pgd_file_name = assembly.Name;
            pgd_file_name = pgd_file_name.Replace(Path.GetExtension(assembly.Name), "");
            pgd_file_name = pgd_file_name + ".pgd";

            String manifest_file_name = assembly.Name;
            manifest_file_name = manifest_file_name + ".intermediate.manifest";

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "link.exe";
            p.StartInfo.Arguments =
                "/OUT:" + "\"" + assembly.Name + "\""
                + " /MANIFEST"
                + " /NXCOMPAT"
                + " /PDB:" + "\"" + pdb_file_name + "\""
                + " /DYNAMICBASE"
                + " /FIXED:NO"
                + " /DEBUG"
                + " /DLL"
                + " /MACHINE:X86"
                + " /INCREMENTAL"
                + " /LIBPATH:\"C:\\Program Files (x86)\\Microsoft Visual Studio 12.0\\VC\\lib\""
                + " /LIBPATH:\"C:\\Program Files (x86)\\Windows Kits\\8.1\\Lib\\winv6.3\\um\\x86\""
                // + " /MANIFESTUAC:\"level='asInvoker' uiAccess='false'\""
                + " /ManifestFile:" + "\"" + manifest_file_name + "\""
                + " /ERRORREPORT:PROMPT"
                + " /NOLOGO"
                + " /ASSEMBLYDEBUG"
                + " /TLBID:1"
                ;

            foreach (KeyValuePair<String, String> kvp in assembly.managed_cpp_files)
            {
                String cpp_source_file_name = kvp.Key;
                String obj_source_file_name = cpp_source_file_name;
                String ext = Path.GetExtension(obj_source_file_name);
                obj_source_file_name = obj_source_file_name.Replace(ext, "");
                obj_source_file_name = obj_source_file_name + ".obj";
                p.StartInfo.Arguments = p.StartInfo.Arguments + " " + obj_source_file_name;
            }
            foreach (KeyValuePair<String, String> kvp in assembly.unmanaged_cpp_files)
            {
                String cpp_source_file_name = kvp.Key;
                String obj_source_file_name = cpp_source_file_name;
                String ext = Path.GetExtension(obj_source_file_name);
                obj_source_file_name = obj_source_file_name.Replace(ext, "");
                obj_source_file_name = obj_source_file_name + ".obj";
                p.StartInfo.Arguments = p.StartInfo.Arguments + " " + obj_source_file_name;
            }
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            if (p.ExitCode > 0)
            {
                System.Console.WriteLine("Link failed: " + output);
                if (output.Contains("cannot open"))
                {
                    System.Console.WriteLine("Restart program in order to get link to work.");
                    using (StreamWriter sw = File.AppendText("Parallel_For_Each.todo"))
                    {
                        sw.WriteLine(p.StartInfo.Arguments);
                    }
                }
                throw new Exception("Link failed.");
            }
            p.WaitForExit();
        }
    }
}
