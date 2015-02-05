using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Campy;
using Campy.Types;
using Campy.Types.Utils;
using Campy.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Test
{
    class Program
    {
        class Point
        {
            public float x;
            public float y;
        }
        static void Main(string[] args)
        {
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

            Extent e = new Extent(size);
            AMP.Parallel_For_Each(e, (Index idx) =>
            {
                int i = idx[0];
                points[i].x = (float)(1.0 * (i / half_size) / half_size);
                points[i].y = (float)(1.0 * (i % half_size) / half_size);
            });
            points.synchronize();
            int[] insc = new int[size];
            Array_View<int> ins = new Array_View<int>(ref insc);
            //ins.discard_data();
            AMP.Parallel_For_Each(e, (Index idx) =>
            {
                int i = idx[0];
                float radius = 1.0f;
                float tx = points[i].x;
                float ty = points[i].y;
                float t = 1;//sqrt(tx*tx + ty*ty);
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
                    ins[i * half_size] += ins[i * half_size + j];
                AMP.Atomic_Fetch_Add(ref res, 0, ins[i * half_size]);
            });
            float pi = (4.0f * res[0]) / size;
        }
    }
}

