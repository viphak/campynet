using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campy
{
    class Assembly
    {
        public String Name
        {
            get;
            private set;
        }

        public Dictionary<String, String> managed_cpp_files
        {
            get;
            private set;
        }

        public Dictionary<String, String> managed_h_files
        {
            get;
            private set;
        }

        public Dictionary<String, String> unmanaged_cpp_files
        {
            get;
            private set;
        }

        public Dictionary<String, String> unmanaged_h_files
        {
            get;
            private set;
        }

        public List<String> executed_lambdas
        {
            get;
            private set;
        }

        public Assembly(String name)
        {
            Name = name;
            managed_cpp_files = new Dictionary<string, string>();
            managed_h_files = new Dictionary<string, string>();
            unmanaged_cpp_files = new Dictionary<string, string>();
            unmanaged_h_files = new Dictionary<string, string>();
            executed_lambdas = new List<string>();
        }
    }
}
