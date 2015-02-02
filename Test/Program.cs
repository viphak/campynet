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
            int size = 64;
            int[] data = new int[size];
            for (int i = 0; i < size; ++i) data[i] = 2 * i;
            Extent e = new Extent(size);
            Array_View<int> d = new Array_View<int>(size, ref data);
            d.synchronize();
            //for (int i = 0; i < size; ++i)
            //{
            //    System.Console.WriteLine(data[i]);
            //}
            //for (int i = 0; i < size; ++i)
            //{
            //    System.Console.WriteLine(d[i]);
            //}
            //AMP.Parallel_For_Each(d.extent, (Index idx) =>
            //{
            //    int j = idx[0];
            //    d[j] = size - j;
            //});
            //d.synchronize();
            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(data[i]);
            }

            Tile_Static<int> s = new Tile_Static<int>(64);

            AMP.Parallel_For_Each(d.extent.tile(64), (Tiled_Index idx) =>
            {
                int t = idx.local[0];
                int tr = size - t - 1;
                s[t] = d[t];
                idx.barrier.wait();
                d[t] = s[tr];
            });
            d.synchronize();

        }
    }
}

