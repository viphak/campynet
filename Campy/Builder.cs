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
            p.StartInfo.FileName = "uncrustify.exe";
            p.StartInfo.Arguments = "-c cfg.cfg -f \"" + source_file_name + "\" -o \"" + source_file_name + "\"";
            p.Start();
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd();
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
                    + " /I\"C:\\cygwin64\\home\\Ken\\Campy.NET\\Campy.Types\""
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
