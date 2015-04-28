using Campy;
using Campy.Types;
using System;
using System.Diagnostics;

namespace Test
{
    class A
    {
        int _aaa;
        public A(int aaa)
        {
            _aaa = aaa;
        }
    }

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

        delegate int fun(int xxx);
        static int ssize = 10;

        static void Main(string[] args)
        {
            AMP.AnalyzeThisAssembly();
            A a = new A(10);
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
            int local_size = size;
            fun test2 = (int k) =>
            {
                return k % (local_size / 2); // Cannot reference size directly!!!
            };
            int[] data = new int[size];
            Extent e = new Extent(size);
            Array_View<int> d = new Array_View<int>(ref data);
            int xsize = size; // required because static size cannot be referenced.
            AMP.Parallel_For_Each(d.Extent, (Index idx) =>
            {
                int j = idx[0];
                d[j] = test2(j);
            });
            d.Synchronize();
            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(data[i]);
            }
            AMP.Parallel_For_Each(d.Extent, (Index idx) =>
            {
                int j = idx[0];
                d[j] = xsize - j - test2(j);
            });
            d.Synchronize(); fg.Synchronize();
            for (int i = 0; i < 30; ++i)
                System.Console.WriteLine(f[i]);
        }
    }
}

