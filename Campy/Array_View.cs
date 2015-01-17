using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpConcurrency
{

    public class Array_View<_Value_type>
    {
        private int _Rank = 1;
        private _Value_type[] arr;

        public Array_View(int length, ref _Value_type[] data)
        {
        }

        public Extent extent
        {
            get;
            set;
        }

        public _Value_type this[int i]
        {
            get
            {
                return arr[i];
            }
            set
            {
                arr[i] = value;
            }
        }

        public _Value_type this[Index i]
        {
            get
            {
                int j = 0;
                return arr[i];
            }
            set
            {
                arr[i] = value;
            }
        }

        public void synchronize()
        { }

        public _Value_type data()
        {
            return this[new Index(_Rank)];
        }
    }
}
