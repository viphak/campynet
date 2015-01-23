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

        static void Main(string[] args)
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
            int size = 10;
            int[] data = new int[size];
            Extent e = new Extent(size);
            Array_View<int> d = new Array_View<int>(size, ref data);
            Stopwatch sw = new Stopwatch();
            sw.Start();

            fun test = (int k) =>
            {
                return k % (size/2);
            };

            Parallel_For_Each.loop(d.extent, (Index idx) =>
            {
                int j = idx[0];
                //d[j] = size - j - (test(j) ? 1 : 2); // Capture size and d.
                d[j] = test(j);
            });
            d.synchronize();

            // Do something you want to time

            sw.Stop();

            long microseconds = sw.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));

            Console.WriteLine("Operation completed in: " + microseconds + " (us)");

            for (int i = 0; i < size; ++i)
            {
                System.Console.WriteLine(data[i]);
            }
        }
    }
}

