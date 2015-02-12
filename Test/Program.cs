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

        delegate T op<T>(T lhs, T rhs);

        static T reduction_simple_1<T>(Campy.Types.Array<T> a, op<T> operation)
        {
	        int size = a.Extent.Size();
	        int element_count = size;
	        Debug.Assert(element_count != 0); // Cannot reduce an empty sequence.
	        if (element_count == 1)
	        {
		        return a[0];
	        }

	        // Using array, as we mostly need just temporary memory to store
	        // the algorithm state between iterations and in the end we have to copy
	        // back only the first element.
	        //array<T, 1> a(element_count, source.begin());

	        // Takes care of odd input elements – we could completely avoid tail sum
	        // if we would require source to have even number of elements.
            T val = (element_count % 2 == 1) ? a[element_count - 1] : default(T);
	        T[] tail_sum = new T[1]; 
            tail_sum[0] = val;

            Array_View<T> av_tail_sum = new Array_View<T>(ref tail_sum);

	        // Each thread reduces two elements.
	        for (int s = element_count / 2; s > 0; s /= 2)
	        {
		        AMP.Parallel_For_Each(new Extent(s), (Index idx) =>
		        {
                    int i = idx[0];
                    T lhs = a[i];
                    T rhs = a[i + s];
			        a[i] = operation(lhs, rhs);

			        // Reduce the tail in cases where the number of elements is odd.
			        if ((idx[0] == s - 1) && ((s & 0x1) == 1) && (s != 1))
			        {
                        lhs = av_tail_sum[0];
                        rhs = a[s - 1];
				        av_tail_sum[0] = operation(lhs, rhs);
			        }
		        });
	        }

	        // Copy the results back to CPU.
	        T[] result = new T[1];
	        av_tail_sum.Synchronize();
            AMP.Copy(a.Section(0, 1), ref result);

	        return operation(result[0], tail_sum[0]);
        }

        static int add(int a, int b)
        {
            return a + b;
        }

        static void doit()
        {
            int half_size = 5000;
            int size = half_size * half_size;

            Array<int> ins = new Array<int>(size);
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

            int res = reduction_simple_1(ins, add);
        }

        static void Main(string[] args)
        {
            doit();
            int half_size = 10;
            int size = half_size * half_size;

            Point[] data = new Point[size];
            for (int i = 0; i < size; ++i) data[i] = new Point();
            for (int i = 0; i < size; ++i)
            {
                data[i].x = i;
                data[i].y = -i;
            }
            Array_View<Point> points = new Array_View<Point>(ref data);
            System.Console.WriteLine(points[1].x);

            Array<int> xx = new Array<int>(10);
            Array<int> xx2 = new Array<int>(10, 20);

            ComputePiCPU();
            ComputePiGPU();
        }
    }
}

