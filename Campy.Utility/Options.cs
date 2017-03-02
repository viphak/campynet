using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campy.Utils
{
    public class Options
    {
        public enum OptionType
        {
            DisplayFinalGraph = 1,
            DisplaySSAComputation,
            DisplayStructureComputation,
            DoNotAnalyzeCampyAssemblies,
        };

        public static Options Singleton { get; } = new Options();

        public UInt64 All { get; set; }

        public bool Get(OptionType t)
        {
            UInt64 one = 1;
            one = one << (((int)t) - 1);
            UInt64 v = All & one;
            return v != 0;
        }

        public void Set(OptionType t, bool v)
        {
            UInt64 one = 1;
            one = one << (((int) t) - 1);
            if (v)
                All = All | one;
            else
                All = All & ~one;
        }

        private Options()
        {
        }
    }
}
