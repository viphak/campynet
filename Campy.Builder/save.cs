using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Campy.Utils;

namespace Campy.Builder
{
    public class Build
    {
        String cl_path = null;
        String link_path = null;
        String uncrustify_path = null;
        String campy_root = null;
        String campy_include_path = null;
        String vc_include_path = null;
        String vc_atlmfc_include_path = null;
        String vc_lib_path = null;
        String wk_include_path = null;
        String wk_lib_path = null;
        String ref_asm_path = null;

        void SetupEnv()
        {

            // Need to know where Campy files are located.
            campy_root = Environment.GetEnvironmentVariable("CAMPYNETROOT");
            for (; ; )
            {
                // Check current path if built and run from Campy.Net test program.
                String inc = "..\\..\\..\\Campy.Types";
                inc = Path.GetFullPath(inc);
                if (Directory.Exists(inc))
                {
                    campy_include_path = inc;
                    campy_root = inc + "\\..";
                    campy_root = Path.GetFullPath(campy_root);
                    if (!Directory.Exists(campy_root))
                        throw new Exception("bad path " + campy_root);
                    break;
                }

                // Campy root must be set.
                if (campy_root == null)
                    throw new Exception("CAMPYNETROOT must be set.");
                
                // Campy root must exist.
                if (!Directory.Exists(campy_root))
                    throw new Exception("CAMPYNETROOT is not a directory.");

                // Campy root must have include directory.
                if (Directory.Exists(campy_root + "\\include"))
                {
                    campy_include_path = campy_root + "\\include";
                    break;
                }

                // Campy root must have if using source tree and campynetroot points to tree,
                // then types will be there.
                if (Directory.Exists(campy_root + "\\Campy.Types"))
                {
                    campy_include_path = campy_root + "\\Campy.Types";
                    break;
                }

                throw new Exception("Cannot determine Campy.NET root. Set CAMPYNETROOT to path of Campy.NET directory.");
            }

            // Set up cl.exe, link.exe, includes, etc., so that we can complete a build.
            // Look for environmental variables.
            // Try in order: VS140COMNTOOLS, VS120COMNTOOLS.
            String root_14;
            String root_12;
            root_14 = Environment.GetEnvironmentVariable("VS140COMNTOOLS");
            root_12 = null; // Environment.GetEnvironmentVariable("VS120COMNTOOLS");
            if (root_14 != null && root_14 != "")
            {
                for (; ; )
                {
                    String root = root_14;

                    // Check path for existence of cl.exe.
                    String path = root + "\\..\\..\\VC\\bin";
                    path = Path.GetFullPath(path);
                    bool found = File.Exists(path + "\\cl.exe");
                    if (!found)
                        break;
                    cl_path = path + "\\cl.exe";
                    found = File.Exists(path + "\\link.exe");
                    if (!found)
                        throw new Exception("cl.exe found but link.exe not found!!");
                    link_path = path + "\\link.exe";

                    // Check path for existence of includes.
                    path = root + "\\..\\..\\VC\\INCLUDE";
                    path = Path.GetFullPath(path);
                    found = File.Exists(path + "\\amp.h");
                    if (!found)
                        throw new Exception("amp.h not found!!");
                    vc_include_path = path;
                    vc_atlmfc_include_path = Path.GetFullPath(path + "..\\atfmfc\\include");
                    vc_lib_path = Path.GetFullPath(path + "..\\lib");
                    break;
                }
            }
            if (cl_path == null && root_12 != null && root_12 != "")
            {
                for (; ; )
                {
                    String root = root_12;

                    // Check path for existence of cl.exe.
                    String path = root + "\\..\\..\\VC\\bin";
                    path = Path.GetFullPath(path);
                    bool found = File.Exists(path + "\\cl.exe");
                    if (!found)
                        break;
                    cl_path = path + "\\cl.exe";
                    found = File.Exists(path + "\\link.exe");
                    if (!found)
                        throw new Exception("cl.exe found but link.exe not found!!");
                    link_path = path + "\\link.exe";
                    // Check path for existence of includes.
                    path = root + "\\..\\..\\VC\\INCLUDE";
                    path = Path.GetFullPath(path);
                    found = File.Exists(path + "\\amp.h");
                    if (!found)
                        throw new Exception("amp.h not found!!");
                    vc_include_path = path;
                    vc_atlmfc_include_path = Path.GetFullPath(path + "..\\atfmfc\\include");
                    vc_lib_path = Path.GetFullPath(path + "..\\lib");
                    break;
                }
            }
            if (cl_path == null || link_path == null)
                throw new Exception("Neither Visual Studio 2013 nor 2015 installed.");

            // Look for Windows Kits, using the compiler path and current OS
            // as a guide.
            PlatformID pid = Environment.OSVersion.Platform;
            String ver = Environment.OSVersion.Version.ToString();
            String pre = Path.GetDirectoryName(cl_path) + "\\..\\..\\..\\Windows Kits";
            pre = Path.GetFullPath(pre);
            if (ver.IndexOf("6.2") == 0 || ver.IndexOf("6.3") == 0)
            {
                String root = pre;
                String path = root + "\\8.1\\Include";
                bool found = Directory.Exists(path);
                if (!found)
                {
					path = root + "\\8.0\\Include";
                    found = Directory.Exists(path);
                }
                if (!found)
                    throw new Exception("Windows Kit not found.");
                wk_include_path = path;
                wk_lib_path = libpath;
                wk_lib_path = Path.GetFullPath(path + "..\\lib\\winv6.3\\um\\x86");
            }
            if (ver.IndexOf("6.1") == 0)
            {
                String root = pre;
                String path = root + "\\7.0\\Include";
                bool found = Directory.Exists(path);
                if (!found)
                    throw new Exception("Windows Kit not found.");
                wk_include_path = path;
                wk_lib_path = Path.GetFullPath(path + "..\\lib\\winv6.1\\um\\x86");
            }
            ref_asm_path = "C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.5";
            if (!Directory.Exists(ref_asm_path))
                throw new Exception("Expecting for .NET 4.5 to be installed.");


            // Find uncrustify.
            for (;;)
            {
                if (File.Exists(campy_root + "\\uncrustify.exe"))
                {
                    uncrustify_path = campy_root + "\\uncrustify.exe";
                    break;
                }

                // Check path.
                String where = Campy.Utils.Utility.FindExePath("uncrustify.exe");
                if (where != null)
                {
                    uncrustify_path = where;
                    break;
                }
                System.Console.WriteLine(
                    "Uncrustify.exe is required by Campy to clean up converted code."
                    + " Download and install on path, from http://uncrustify.sourceforge.net/");
                throw new Exception("uncrustify not found.");
                break;
            }

        }

        public Build()
        {
            SetupEnv();
            Make();
        }

        public void Make()
        {
            // Load "AMP.todo"
            String file_name = "AMP.todo";
            if (File.Exists(file_name))
            {
                bool failed = false;
                string[] lines = System.IO.File.ReadAllLines("AMP.todo");
                foreach (string line in lines)
                {
                    // Compile and link all work.
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.FileName = link_path;
                    p.StartInfo.Arguments = line;
                    p.Start();
                    p.WaitForExit();
                    string output = p.StandardOutput.ReadToEnd();
                    String more_output = p.StandardError.ReadToEnd();
                    if (p.ExitCode > 0)
                    {
                        failed = true;
                        System.Console.WriteLine("Link failed: " + output);
                        System.Console.WriteLine(more_output);
                    }
                }
                File.Delete("AMP.todo");
                if (failed)
                    throw new Exception();
            }
        }

        public void Uncrustify(String source_file_name)
        {
            String[] cfg = new String[] { @"

#
# My favorite format
#

indent_with_tabs		= 0		# 1=indent to level only, 2=indent with tabs
input_tab_size			= 8		# original tab size
output_tab_size			= 4		# new tab size
indent_columns			= output_tab_size
indent_label			= 2		# pos: absolute col, neg: relative column
indent_align_string		= False		# align broken strings
indent_brace			= 0
indent_braces = false
indent_namespace = true
indent_namespace_level                   = 4        # number
indent_class = true
indent_col1_comment = true
indent_access_spec = 5

nl_enum_brace			= add		# 'enum {' vs 'enum \n {'
nl_union_brace			= add		# 'union {' vs 'union \n {'
nl_struct_brace			= add		# 'struct {' vs 'struct \n {'
nl_do_brace			= add		# 'do {' vs 'do \n {'
nl_if_brace			= add		# 'if () {' vs 'if () \n {'
nl_for_brace			= add		# 'for () {' vs 'for () \n {'
nl_else_brace			= add		# 'else {' vs 'else \n {'
nl_while_brace			= add		# 'while () {' vs 'while () \n {'
nl_switch_brace			= add		# 'switch () {' vs 'switch () \n {'
# nl_func_var_def_blk		= 1
# nl_before_case			= 1
nl_fcall_brace			= add		# 'foo() {' vs 'foo()\n{'
nl_fdef_brace			= add		# 'int foo() {' vs 'int foo()\n{'
# nl_after_return			= TRUE
nl_brace_while			= remove
nl_brace_else			= add
nl_squeeze_ifdef		= TRUE

# mod_paren_on_return		= add		# 'return 1;' vs 'return (1);'
# mod_full_brace_if		= add		# 'if (a) a--;' vs 'if (a) { a--; }'
# mod_full_brace_for		= add		# 'for () a--;' vs 'for () { a--; }'
# mod_full_brace_do		= add		# 'do a--; while ();' vs 'do { a--; } while ();'
# mod_full_brace_while		= add		# 'while (a) a--;' vs 'while (a) { a--; }'

sp_before_semi			= remove
sp_paren_paren			= remove	# space between (( and ))
sp_return_paren			= remove	# 'return (1);' vs 'return(1);'
sp_sizeof_paren			= remove	# 'sizeof (int)' vs 'sizeof(int)'
sp_before_sparen		= force		# 'if (' vs 'if('
sp_after_sparen			= force		# 'if () {' vs 'if (){'
sp_after_cast			= add		# '(int) a' vs '(int)a'
sp_inside_braces		= force		# '{ 1 }' vs '{1}'
sp_inside_braces_struct		= force		# '{ 1 }' vs '{1}'
sp_inside_braces_enum		= force		# '{ 1 }' vs '{1}'
sp_inside_paren			= remove
sp_inside_fparen		= remove
sp_inside_sparen		= remove
#sp_type_func			= ignore
sp_assign			= force
sp_arith			= force
sp_bool				= force
sp_compare			= force
sp_assign			= force
sp_after_comma			= force
sp_func_def_paren		= remove	# 'int foo (){' vs 'int foo(){'
sp_func_call_paren		= remove	# 'foo (' vs 'foo('
sp_func_proto_paren		= remove	# 'int foo ();' vs 'int foo();'

align_with_tabs			= FALSE		# use tabs to align
align_on_tabstop		= FALSE		# align on tabstops
align_enum_equ_span		= 4
align_nl_cont			= TRUE
align_var_def_span		= 2
align_var_def_inline		= TRUE
align_var_def_star_style	= 1
align_var_def_colon		= TRUE
align_assign_span		= 1
align_struct_init_span		= 3
align_var_struct_span		= 3
align_right_cmt_span		= 3
align_pp_define_span		= 3
align_pp_define_gap		= 4
align_number_left		= TRUE
align_typedef_span		= 4
align_typedef_gap		= 3

# cmt_star_cont			= TRUE

eat_blanks_before_close_brace	= TRUE
eat_blanks_after_open_brace	= TRUE

" };
            File.WriteAllLines("cfg.cfg", cfg);
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = uncrustify_path;
            p.StartInfo.Arguments = "-c cfg.cfg -f \"" + source_file_name + "\" -o \"" + source_file_name + "\"";
            try
            {
                p.Start();
                p.WaitForExit();
            }
            catch
            {}
            String output = p.StandardOutput.ReadToEnd();
            String out_err = p.StandardError.ReadToEnd();
            if (p.ExitCode > 0)
            {
                System.Console.WriteLine("Uncrustify failed:");
                if (!output.Equals(""))
                    System.Console.WriteLine(output);
                if (!out_err.Equals(""))
                    System.Console.WriteLine(out_err);
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

            bool uncrustify = true;
            if (uncrustify)
            {
                foreach (KeyValuePair<String, String> kvp in assembly.managed_cpp_files)
                    Uncrustify(kvp.Key);
                foreach (KeyValuePair<String, String> kvp in assembly.unmanaged_cpp_files)
                    Uncrustify(kvp.Key);
                foreach (KeyValuePair<String, String> kvp in assembly.managed_h_files)
                    Uncrustify(kvp.Key);
                foreach (KeyValuePair<String, String> kvp in assembly.unmanaged_h_files)
                    Uncrustify(kvp.Key);
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
                p.StartInfo.FileName = cl_path;
                p.StartInfo.Arguments =
                    "/c"
                    + " /I\"" + campy_include_path + "\""
                    + " /I\"" + vc_include_path + "\""
                    + " /I\"" + vc_atlmfc_include_path + "\""
                    + " /I\"" + wk_include_path + "\\shared\""
                    + " /I\"" + wk_include_path + "\\ucrt\""
                    + " /I\"" + wk_include_path + "\\um\""
                    + " /I\"" + wk_include_path + "\\winrt\""
                    + " /GS"
                    + " /GR"
                    + " /analyze-"
                    + " /W3"
                    + " /Zc:wchar_t"
                    + " /Zi"
                    + " /Od"
                    + " /sdl"
                    + " /Fd\"vc140.pdb\""
                    + " /D \"WIN32\""
                    + " /D \"_DEBUG\""
                    + " /D \"_WINDLL\""
                    + " /D \"_UNICODE\""
                    + " /D \"UNICODE\""
                    + " /errorReport:prompt"
                    + " /FU\"" + ref_asm_path + "\\mscorlib.dll\""
                    + " /FU\"" + ref_asm_path + "\\System.Data.dll\""
                    + " /FU\"" + ref_asm_path + "\\System.dll\""
                    + " /FU\"" + ref_asm_path + "\\System.Xml.dll\""
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
                using (StreamWriter sw = File.AppendText("save.save"))
                {
                    sw.WriteLine("\"" + p.StartInfo.FileName + "\"" + " " + p.StartInfo.Arguments);
                }
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                String more_output = p.StandardError.ReadToEnd();
                if (!p.HasExited)
                    p.WaitForExit();
                if (p.ExitCode > 0)
                {
                    System.Console.WriteLine("Compile failed: " + output);
                    System.Console.WriteLine(more_output);
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
                p.StartInfo.FileName = cl_path;
                p.StartInfo.Arguments =
                    "/c"
                    + " /I\"" + campy_include_path + "\""
                    + " /I\"" + vc_include_path + "\""
                    + " /I\"" + vc_atlmfc_include_path + "\""
                    + " /I\"" + wk_include_path + "\\shared\""
                    + " /I\"" + wk_include_path + "\\ucrt\""
                    + " /I\"" + wk_include_path + "\\um\""
                    //+ " /I\"" + wk_include_path + "\\winrt\""
                    + " /GS"
                    + " /analyze-"
                    + " /W3"
                    + " /Zc:wchar_t"
                    + " /ZI"
                    + " /Gm"
                    + " /Od"
                    + " /sdl"
                    + " /Fd\"vc140.pdb\""
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
                using (StreamWriter sw = File.AppendText("save.save"))
                {
                    sw.WriteLine("\"" + p.StartInfo.FileName + "\"" + " " + p.StartInfo.Arguments);
                }
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                String more_output = p.StandardError.ReadToEnd();
                if (!p.HasExited)
                    p.WaitForExit();
                if (p.ExitCode > 0)
                {
                    System.Console.WriteLine("Compile failed: " + output);
                    System.Console.WriteLine(more_output);
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
            p.StartInfo.FileName = link_path;
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
                + " /LIBPATH:\"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\lib\""
                //+ $@" /LIBPATH:""C:\Program Files (x86)\Windows Kits\10\Lib\10.0.10240.0\um\x86\"""
                + " /LIBPATH:\"C:\\Program Files (x86)\\Windows Kits\\10\\Lib\\10.0.10240.0\\ucrt\\x86\""
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
            using (StreamWriter sw = File.AppendText("save.save"))
            {
                sw.WriteLine("\"" + p.StartInfo.FileName + "\"" + " " + p.StartInfo.Arguments);
            }
            p.Start();
            String output = p.StandardOutput.ReadToEnd();
            String more_output = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode > 0)
            {
                System.Console.WriteLine("Link failed: " + output);
                System.Console.WriteLine(more_output);
                if (output.Contains("cannot open") || more_output.Contains("cannot open"))
                {
                    System.Console.WriteLine("Restart program in order to get link to work.");
                    using (StreamWriter sw = File.AppendText("AMP.todo"))
                    {
                        sw.WriteLine(p.StartInfo.Arguments);
                    }
                }
                throw new Exception("Link failed.");
            }
        }
    }
}
