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
        delegate int fun(int xxx);
        static int size = 10;

        static void Part1()
        {
            int lsize = size;

            fun frick = (int k) =>
            {
                return k % (lsize / 2);
            };

            Part2(frick);
        }

        static void Part2(fun test)
        {
            Accelerator_View def = Accelerator.get_default_view();
            Accelerator def_acc = def.get_accelerator();
            System.Console.WriteLine("Default accelerator:");
            System.Console.WriteLine(def_acc.description());
            System.Console.WriteLine(def_acc.device_path());
            System.Console.WriteLine();
            List<Accelerator> la = Accelerator.get_all();
            foreach (Accelerator a in la)
            {
                System.Console.WriteLine(a.description());
                System.Console.WriteLine(a.device_path());
                if (a.description().Contains("GTX"))
                {
                    //bool was_set = Accelerator.set_default(a.device_path());
                }
                System.Console.WriteLine();
            }

            int[] data = new int[size];
            Extent e = new Extent(size);
            Array_View<int> d = new Array_View<int>(size, ref data);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int local_size = size;

            fun test2 = (int k) =>
            {
                return k % (local_size / 2); // Cannot reference size directly!!!
            };
            int xsize = size; // required because static size cannot be referenced.
            AMP.Parallel_For_Each(d.extent, (Index idx) =>
            {
                int j = idx[0];
                d[j] = test(j);
            });
            d.synchronize();
            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(data[i]);
            }
            AMP.Parallel_For_Each(d.extent, (Index idx) =>
            {
                int j = idx[0];
                d[j] = xsize - j - test2(j);
            });
            d.synchronize();
            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(data[i]);
            }
            sw.Stop();
            long microseconds = sw.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));
            Console.WriteLine("Operation completed in: " + microseconds + " (us)");
        }

        static void Main(string[] args)
        {
            //Part1();
            int size = 10;
            int[] data = new int[size];
            for (int i = 0; i < size; ++i) data[i] = 2 * i;
            Extent e = new Extent(size);
            Array_View<int> d = new Array_View<int>(size, ref data);
            d.synchronize();
            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(data[i]);
            }
            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(d[i]);
            }
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

