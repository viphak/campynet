#include "stdafx.h"
#include "Extent.h"
#include "Index.h"
#include "Native_Extent.h"

using namespace System;

namespace Campy {
	namespace Types {

		Extent::Extent()
		{
			_Rank = 1;
			_M_base = gcnew array<int>(_Rank);
			_native = (void*) new Native_Extent<1>();
		}

		Extent::Extent(int _I0)
		{
			_Rank = 1;
			_M_base = gcnew array<int>(_Rank);
			_M_base[0] = _I0;
			_native = (void*) new Native_Extent<1>(_I0);
		}

		Extent::Extent(int _I0, int _I1)
		{
			_Rank = 2;
			_M_base = gcnew array<int>(_Rank);
			_M_base[0] = _I0;
			_M_base[1] = _I1;
			_native = (void*) new Native_Extent<2>();
		}

		Extent::Extent(int _I0, int _I1, int _I2)
		{
			_Rank = 3;
			_M_base = gcnew array<int>(_Rank);
			_M_base[0] = _I0;
			_M_base[1] = _I1;
			_M_base[2] = _I2;
			_native = (void*) new Native_Extent<3>();
		}

		Extent::Extent(array<int>^ _Array)
		{
			_Rank = _Array->Length;
			_M_base = gcnew array<int>(_Rank);
			for (int i = 0; i < _Rank; ++i)
				_M_base[i] = _Array[i];
			if (_Rank == 1)	_native = (void*) new Native_Extent<1>();
			if (_Rank == 2)	_native = (void*) new Native_Extent<2>();
			if (_Rank == 3)	_native = (void*) new Native_Extent<3>();
		}

		int Extent::size()
		{
			int result = 1;
			for (int i = 0; i < _Rank; ++i)
				result *= _M_base[i];
			return result;
		}

		int Extent::operator[](int i)
		{
			return _M_base[i];
		}

		int Extent::operator[](Index^ i)
		{
			int j = 0; // i;
			return _M_base[j];
		}

		Extent^ Extent::operator +(Extent^ _Lhs, Index^ _Rhs)
		{
			Extent^ result = gcnew Extent();
			result->_Rank = _Rhs->_Rank;
			for (int i = 0; i < _Rhs->_Rank; ++i)
				result->_M_base[i] = _Lhs->_M_base[i] + _Rhs->_M_base[i];
			return result;
		}

		Extent^ Extent::operator ++(Extent^ _Lhs)
		{
			for (int i = 0; i < _Lhs->_Rank; ++i)
				_Lhs->_M_base[i]++;
			return _Lhs;
		}

		Extent^ Extent::operator -(Extent^ _Lhs, Index^ _Rhs)
		{
			Extent^ result = gcnew Extent();
			result->_Rank = _Rhs->_Rank;
			for (int i = 0; i < _Rhs->_Rank; ++i)
				result->_M_base[i] = _Lhs->_M_base[i] - _Rhs->_M_base[i];
			return result;
		}

		Extent^ Extent::operator --(Extent^ _Lhs)
		{
			for (int i = 0; i < _Lhs->_Rank; ++i)
				_Lhs->_M_base[i]--;
			return _Lhs;
		}

		void* Extent::native()
		{
			return this->_native;
		}

	}
}
