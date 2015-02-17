using Campy;
using Campy.Types;
using System;
using System.Diagnostics;

namespace Test
{
    class Program
    {
        static int factorial(int x)
        {
            if (x == 1)
                return 1;
            return x * factorial(x - 1);
        }

        static void Main(string[] args)
        {
            int size = 100000;
            int[] f = new int[size];
            Array_View<int> fg = new Array_View<int>(ref f);
            AMP.Parallel_For_Each(new Extent(size), (Index idx) =>
            {
                int i = idx[0];
                fg[i] = factorial(10);
            });
            fg.Synchronize();
        }
    }
}

