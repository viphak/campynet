using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Campy;
using Campy.Types;
using Campy.Types.Utils;
using Campy.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            int size = 10;
            int[] data = new int[size];
            for (int i = 0; i < size; ++i) data[i] = 2 * i;
            Extent e = new Extent(size);
            Array_View<int> d = new Array_View<int>(size, ref data);
            AMP.Parallel_For_Each(d.extent, (Index idx) =>
            {
                int j = idx[0];
                d[j] = size - j;
            });
            d.synchronize();
            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(data[i]);
            }
        }
    }
}

