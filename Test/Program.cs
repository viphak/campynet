using Campy;
using Campy.Types;
using System;
using System.Diagnostics;

namespace Test
{
    class Program
    {
        class Point
        {
            public float x;
            public float y;
        }

        static void ComputePiCPU()
        {
            int half_size = 5000;
            int size = half_size * half_size;
            int[] ins = new int[size];

            DateTime start;
            TimeSpan time;
            start = DateTime.Now;
            for (int i = 0; i < size; ++i)
            {
                float radius = 1.0f;
                float tx = (float)(1.0 * (i / half_size) / half_size);
                float ty = (float)(1.0 * (i % half_size) / half_size);
                float t = (float)Math.Sqrt(tx * tx + ty * ty);
                ins[i] = (t <= radius) ? 1 : 0;
            }
            int res = 0;
            for (int i = 0; i < half_size; ++i)
            {
                for (int j = 1; j < half_size; ++j)
                {
                    int k = i * half_size;
                    int t1 = ins[k + j];
                    int t2 = ins[k];
                    int t3 = t1 + t2;
                    ins[k] = t3;
                }
                res += ins[i * half_size];
            }
            int cou = res;
            float pi = (4.0f * cou) / size;
            time = DateTime.Now - start;
            System.Console.WriteLine("Count is " + cou + " out of " + size);
            System.Console.WriteLine("Pi is " + pi);
            System.Console.WriteLine(String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0')));
        }

        static void ComputePiGPU()
        {
            int half_size = 5000;
            int size = half_size * half_size;

            int[] insc = new int[size];
            Array_View<int> ins = new Array_View<int>(ref insc);
            ins.Discard_Data();
            DateTime start;
            TimeSpan time;
            start = DateTime.Now;
            AMP.Parallel_For_Each(new Extent(size), (Index idx) =>
            {
                int i = idx[0];
                // Pseudo random number generated point.
                float x = (float)(1.0 * (i / half_size) / half_size);
                float y = (float)(1.0 * (i % half_size) / half_size);
                float t = (float)Math.Sqrt(x * x + y * y);
                ins[i] = (t <= 1.0f) ? 1 : 0;
            });
            int[] count = new int[1];
            count[0] = 0;

            Array_View<int> res = new Array_View<int>(ref count);
            //AMP.Parallel_For_Each(new Extent(half_size), (Index idx) =>
            //{
            //    int i = idx[0];
            //    int c = 0;
            //    for (int j = 0; j < half_size; ++j)
            //    {
            //        int k = i * half_size;
            //        int t1 = ins[k + j];
            //        int t2 = c;
            //        int t3 = t1 + t2;
            //        c = t3;
            //    }
            //    AMP.Atomic_Fetch_Add(ref res, 0, c);
            //});

            Func<int, int> pow2 = (int e) =>
            {
                int x = 1;
                for (int i = 0; i < e; ++i)
                    x *= 2;
                return x;
            };

            Func<int, int> flog2 = (int v) =>
            {
                int x = 0;
                while ((v = (v >> 1)) != 0)
                {
                    x++;
                }
                return x;
            };

            // Round up H to power of 2.
            int rounded_size = 1;
            for (uint i = 0; rounded_size < size; ++i) rounded_size <<= 1;
            int increment = 1;
            int levels = flog2(rounded_size);
            Accelerator_View acc = Accelerator_View.Default_Value;
            for (int level = 1; level <= levels; level += increment)
            {
                int step = pow2(level);
                int threads = rounded_size / step;
                if (threads == 0)
                    threads = 1;
                AMP.Parallel_For_Each(new Extent(threads), (Index idx) =>
                {
                    int i = idx[0] * step;
                    if (i < size)
                    {
                        int t1 = ins[i];
                        int t2 = ins[i + step / 2];
                        ins[i] = t1 + t2;
                    }
                });
                //ins.Synchronize();
            }
	        acc.Wait();
            int[] result = new int[1];
            AMP.Copy(ins.Section(0, 1), ref result);
            int cou = result[0];

            //std::cout << "result = " << d << "\n";
            float pi = (4.0f * cou) / size;
            time = DateTime.Now - start;
            System.Console.WriteLine("Count is " + cou + " out of " + size);
            System.Console.WriteLine("Pi is " + pi);
            System.Console.WriteLine(String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0')));
        }

        static void Main(string[] args)
        {
            Array<int> xx = new Array<int>(10);
            Array<int> xx2 = new Array<int>(10, 20);

            ComputePiCPU();
            ComputePiGPU();
        }
    }
}

