using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpConcurrency
{
    public class Index
    {
        internal int _Rank;
        internal int[] _M_base;

        public Index()
        {
            _Rank = 1;
            _M_base = new int[_Rank];
        }

        public Index(int _I)
        {
            _Rank = 1;
            _M_base = new int[_Rank];
            _M_base[0] = _I;
        }

        public Index(int _I0, int _I1)
        {
            _Rank = 2;
            _M_base = new int[_Rank];
            _M_base[0] = _I0;
            _M_base[1] = _I1;
        }

        public Index(int _I0, int _I1, int _I2)
        {
            _Rank = 3;
            _M_base = new int[_Rank];
            _M_base[0] = _I0;
            _M_base[1] = _I1;
            _M_base[2] = _I2;
        }

        public int rank
        {
            get { return _Rank; }
        }

        public static implicit operator int(Index idx)  // implicit
        {
            return 0;
        }
    }
}
