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
            AMP.AnalyzeThisAssembly();
            int size = 100000;
            int[] data = new int[size];
            int[] data2 = new int[size];
            Extent e = new Extent(size);
            Array_View<int> d = new Array_View<int>(ref data);
            Array_View<int> d2 = new Array_View<int>(ref data2);
            AMP.Parallel_For_Each(e, (Index idx) =>
            {
                int j = idx[0];
                d[j] = (int)size - j - 1;
            });
            AMP.Parallel_For_Each(e, (Index idx) =>
            {
                int j = idx[0];
                d2[j] = 1;
            });
            AMP.Parallel_For_Each(e, (Index idx) =>
            {
                int j = idx[0];
                d[j] = d[j] % 2;
            });
            AMP.Parallel_For_Each(e, (Index idx) =>
            {
                int j = idx[0];
                d[j] = d[j] + d2[j];
            });
            d.Synchronize();
            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(data[i]);
            }
        }
    }
}
