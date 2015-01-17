using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpConcurrency
{
    public class Extent
    {
        internal int _Rank;
        internal int[] _M_base;

        public Extent()
        {
            _Rank = 1;
            _M_base = new int[_Rank];
        }

        public Extent(int _I)
        {
            _Rank = 1;
            _M_base = new int[_Rank];
            _M_base[0] = _I;
        }

        public Extent(int _I0, int _I1)
        {
            _Rank = 2;
            _M_base = new int[_Rank];
            _M_base[0] = _I0;
            _M_base[1] = _I1;
        }

        public Extent(int _I0, int _I1, int _I2)
        {
            _Rank = 3;
            _M_base = new int[_Rank];
            _M_base[0] = _I0;
            _M_base[1] = _I1;
            _M_base[2] = _I2;
        }

        public Extent(int[] _Array)
        {
            _Rank = _Array.Length;
            _M_base = new int[_Rank];
            for (int i = 0; i < _Rank; ++i)
                _M_base[i] = _Array[i];
        }

        public int size()
        {
            int result = 1;
            for (int i = 0; i < _Rank; ++i)
                result *= _M_base[i];
            return result;
        }

        public int this[int _Index]
        {
            get
            {
                return _M_base[_Index];
            }
            set
            {
                _M_base[_Index] = value;
            }
        }

        static public Extent operator +(Extent _Lhs, Index _Rhs)
        {
            Extent result = new Extent();
            result._Rank = _Rhs._Rank;
            for (int i = 0; i < _Rhs._Rank; ++i)
                result._M_base[i] = _Lhs._M_base[i] + _Rhs._M_base[i];
            return result;
        }

        static public Extent operator ++(Extent _Lhs)
        {
            for (int i = 0; i < _Lhs._Rank; ++i)
                _Lhs._M_base[i]++;
            return _Lhs;
        }

        static public Extent operator -(Extent _Lhs, Index _Rhs)
        {
            Extent result = new Extent();
            result._Rank = _Rhs._Rank;
            for (int i = 0; i < _Rhs._Rank; ++i)
                result._M_base[i] = _Lhs._M_base[i] - _Rhs._M_base[i];
            return result;
        }

        static public Extent operator --(Extent _Lhs)
        {
            for (int i = 0; i < _Lhs._Rank; ++i)
                _Lhs._M_base[i]--;
            return _Lhs;
        }

        // C# does not support post-increment, post-decrement, +=, -=, etc. operator overloading.

    }
}
