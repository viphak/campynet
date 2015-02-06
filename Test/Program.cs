using Campy;
using Campy.Types;
using System;

namespace Test
{
    class Program
    {
        class Point
        {
            public float x;
            public float y;
        }

        static void ComputePiGPU()
        {
            int half_size = 5000;
            int size = half_size * half_size;

            Point[] points = new Point[size];
            for (int i = 0; i < size; ++i) points[i] = new Point();
            DateTime start;
            TimeSpan time;
            start = DateTime.Now;
            for (int i = 0; i < size; ++i)
            {
                points[i].x = (float)(1.0 * (i / half_size) / half_size);
                points[i].y = (float)(1.0 * (i % half_size) / half_size);
            }
            int[] ins = new int[size];
            for (int i = 0; i < size; ++i)
            {
                float radius = 1.0f;
                float tx = points[i].x;
                float ty = points[i].y;
                float t = (float)Math.Sqrt(tx * tx + ty * ty);
                ins[i] = (t <= radius) ? 1 : 0;
            }
            Extent e_half = new Extent(half_size);
            int[] res = new int[1];
            res[0] = 0;
            for (int i = 0; i < half_size; ++i)
            {
                for (int j = 1; j < half_size; ++j)
                {
                    int k = i * half_size;
                    int t1 = ins[k + j];
                    int t2 = ins[k];
                    int t3 = t1 + t2;
                    ins[k] = t3;
                    // cannot decompile!!! ins[i * half_size] += ins[i * half_size + j];
                }
                res[0] += ins[i * half_size];
            }
            int cou = res[0];
            float pi = (4.0f * cou) / size;
            time = DateTime.Now - start;
            System.Console.WriteLine("Count is " + cou + " out of " + size);
            System.Console.WriteLine("Pi is " + pi);
            System.Console.WriteLine(String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0')));
        }

        static void ComputePiCPU()
        {
            int half_size = 5000;
            int size = half_size * half_size;

            Point[] data = new Point[size];
            for (int i = 0; i < size; ++i) data[i] = new Point();
            Array_View<Point> points = new Array_View<Point>(ref data);
            Extent e = new Extent(size);
            DateTime start;
            TimeSpan time;
            start = DateTime.Now;
            AMP.Parallel_For_Each(e, (Index idx) =>
            {
                int i = idx[0];
                points[i].x = (float)(1.0 * (i / half_size) / half_size);
                points[i].y = (float)(1.0 * (i % half_size) / half_size);
            });
            int[] insc = new int[size];
            Array_View<int> ins = new Array_View<int>(ref insc);
            //ins.discard_data();
            AMP.Parallel_For_Each(e, (Index idx) =>
            {
                int i = idx[0];
                float radius = 1.0f;
                float tx = points[i].x;
                float ty = points[i].y;
                float t = (float)Math.Sqrt(tx * tx + ty * ty);
                ins[i] = (t <= radius) ? 1 : 0;
            });
            Extent e_half = new Extent(half_size);
            int[] count = new int[1];
            count[0] = 0;
            Array_View<int> res = new Array_View<int>(ref count);
            AMP.Parallel_For_Each(e_half, (Index idx) =>
            {
                int i = idx[0];
                for (int j = 1; j < half_size; ++j)
                {
                    int k = i * half_size;
                    int t1 = ins[k + j];
                    int t2 = ins[k];
                    int t3 = t1 + t2;
                    ins[k] = t3;
                    // cannot decompile!!! ins[i * half_size] += ins[i * half_size + j];
                }
                AMP.Atomic_Fetch_Add(ref res, 0, ins[i * half_size]);
            });
            int cou = res[0];
            float pi = (4.0f * cou) / size;
            time = DateTime.Now - start;
            System.Console.WriteLine("Count is " + cou + " out of " + size);
            System.Console.WriteLine("Pi is " + pi);
            System.Console.WriteLine(String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0')));
        }

        static void Main(string[] args)
        {
            ComputePiCPU();
            ComputePiGPU();
        }
    }
}

