using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Campy;
using Campy.Types;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            int size = 100000;
            int[] data = new int[size];
            int[] data2 = new int[size];
            Extent e = new Extent(size);
            Int16 shortsize = (Int16)size;
            Array_View<int> d = new Array_View<int>(size, ref data);
            Array_View<int> d2 = new Array_View<int>(size, ref data2);
            Parallel_For_Each.loop(d.extent, (Index idx) =>
            {
                int j = idx[0];
                d[j] = size - j - 1;
            });
            Parallel_For_Each.loop(e, (Index idx) =>
            {
                int j = idx[0];
                d2[j] = 1;
            });
            Parallel_For_Each.loop(e, (Index idx) =>
            {
                int j = idx[0];
                d[j] = d[j] % 2;
            });
            Parallel_For_Each.loop(e, (Index idx) =>
            {
                int j = idx[0];
                d[j] = d[j] + d2[j];
            });
            d.synchronize();
            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(data[i]);
            }
        }
    }
}

