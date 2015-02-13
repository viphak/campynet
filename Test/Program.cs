using Campy;
using Campy.Types;
using System;
using System.Diagnostics;

namespace Test
{
    class Program
    {
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

        static bool IS_POWER_OF_2(int x)
        {
            return (x & 1) == 0;
        }

        static T reduction_cascade<T>(Campy.Types.Array<T> a, op<T> operation,
            int _tile_size, int _tile_count)
        {
            Debug.Assert(_tile_count > 0, "Tile count must be positive!");
            Debug.Assert(IS_POWER_OF_2(_tile_size), "Tile size must be a positive integer power of two!");

            Debug.Assert(a.Extent.Size() <= System.Int32.MaxValue);
            int size = a.Extent.Size();
            int element_count = size;
            Debug.Assert(element_count != 0); // Cannot reduce an empty sequence.
            if (element_count == 1)
            {
                return a[0];
            }

            int stride = _tile_size * _tile_count * 2;

            // Reduce tail elements.
            T tail_sum = default(T);
            int tail_length = element_count % stride;
            if (tail_length != 0)
            {
                // Copy partially from "a" into array because
                // it is not accessible from CPU.
                Array_View<T> partial = a.Section(size - tail_length, tail_length);
                
                // Now sum.
                for (int i = 0; i < tail_length; ++i)
                    tail_sum = operation(tail_sum, partial[i]);

                element_count -= tail_length;
                if (element_count == 0)
                {
                    return tail_sum;
                }
            }

            // Using arrays as a temporary memory.
            //array<float, 1> a(element_count, source.begin());
            Array<T> a_partial_result = new Array<T>(_tile_count);

            // Use tile_static as a scratchpad memory.
            Tile_Static<T> tile_data = new Tile_Static<T>(_tile_size);

            AMP.Parallel_For_Each(new Extent(_tile_count * _tile_size).Tile(_tile_size), (Tiled_Index tidx) =>
            {
                int local_idx = tidx.Local[0];

                // Reduce data strides of twice the tile size into tile_static memory.
                int input_idx = (tidx.Tile[0] * 2 * _tile_size) + local_idx;
                tile_data[local_idx] = default(T);
                do
                {
                    T t1 = a[input_idx];
                    T t2 = a[input_idx + _tile_size];
                    T t3 = operation(t1, t2);
                    T t4 = tile_data[local_idx];
                    tile_data[local_idx] = operation(t4, t3);
                    input_idx += stride;
                } while (input_idx < element_count);

                tidx.Barrier.Wait();

                // Reduce to the tile result using multiple threads.
                for (int s = _tile_size / 2; s > 0; s /= 2)
                {
                    if (local_idx < s)
                    {
                        T t1 = tile_data[local_idx];
                        T t2 = tile_data[local_idx + s];
                        tile_data[local_idx] = operation(t1, t2);
                    }

                    tidx.Barrier.Wait();
                }

                // Store the tile result in the global memory.
                if (local_idx == 0)
                {
                    a_partial_result[tidx.Tile[0]] = tile_data[0];
                }
            });

            // Reduce results from all tiles on the CPU.
            T[] v_partial_result = new T[_tile_count];
            AMP.Copy<T>(a_partial_result, ref v_partial_result);

            T result = default(T);
            for (int i = 0; i < v_partial_result.Length; ++i)
                result = operation(result, v_partial_result[i]);

            return operation(result, tail_sum);
        }

        static int add(int a, int b)
        {
            return a + b;
        }

        static void ComputePi1()
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
            float pi = (4.0f * res) / size;
            System.Console.WriteLine("pi = " + pi);
        }

        static void ComputePi2()
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

            int res = Program.reduction_cascade(ins, add, 1024, 128);
            float pi = (4.0f * res) / size;
            System.Console.WriteLine("pi = " + pi);
        }

        static void Main(string[] args)
        {
            ComputePi1();
            ComputePi2();
        }
    }
}

