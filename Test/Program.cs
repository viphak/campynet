using Campy;
using Campy.Types;
using System;
using System.Diagnostics;

namespace Test
{
    class Program
    {
        void foo1(int a)
        {
            int z = a + 16;
        }

        int foo2(int a)
        {
            int z = a + 16;
            return z;
        }

        static void foo3(int a)
        {
            int z = a + 16;
        }

        static int foo4(int a)
        {
            int z = a + 16;
            return z;
        }


        static int factorial(int x)
        {
            int result = 1;
            if (x == 0)
                return 1;
            if (x == 1)
                return 1;
            for (int i = x; i > 0; --i)
            {
                result = result * i;
            }
            return result;
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.foo1(1);
            int r1 = p.foo2(1);
            foo3(1);
            int r2 = foo4(1);
            int r3 = r1 + r2;

            int size = 100000;
            int[] f = new int[size];
            Array_View<int> fg = new Array_View<int>(ref f);
            AMP.Parallel_For_Each(new Extent(size), (Index idx) =>
            {
                int i = idx[0];
                fg[i] = factorial(i % 6);
            });
            fg.Synchronize();
            for (int i = 0; i < 30; ++i)
                System.Console.WriteLine(f[i]);
        }
    }
}

