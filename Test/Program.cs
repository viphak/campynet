using Campy;
using Campy.Types;
using System;
using System.Diagnostics;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Parallel.AnalyzeThisAssembly();
            int size = 100000;
            int[] data = new int[size];
            int[] data2 = new int[size];
            Extent e = new Extent(size);
            Array_View<int> d = new Array_View<int>(ref data);
            Array_View<int> d2 = new Array_View<int>(ref data2);
            Parallel.For(e, (Index idx) =>
            {
                int j = idx[0];
                d[j] = (int)size - j - 1;
            });
            Parallel.For(e, (Index idx) =>
            {
                int j = idx[0];
                d2[j] = 1;
            });
            Parallel.For(e, (Index idx) =>
            {
                int j = idx[0];
                d[j] = d[j] % 2;
            });
            Parallel.For(e, (Index idx) =>
            {
                int j = idx[0];
                d[j] = d[j] + d2[j];
            });
            d.Synchronize();
            d2.Synchronize();
            for (int i = 0; i < 10; ++i)
            {
                System.Console.WriteLine(data[i]);
            }
        }
    }
}
