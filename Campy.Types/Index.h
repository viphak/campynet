// Extent.h

#pragma once

using namespace System;

namespace Campy {
	namespace Types {

		public ref class Index
		{
		internal:
			int _Rank;
			array<int>^ _M_base;
			static Index^ default_value = gcnew Index();

		public:
			Index()
			{
				_Rank = 1;
				_M_base = gcnew array<int>(_Rank);
			}

		public:
			Index(int _I)
			{
				_Rank = 1;
				_M_base = gcnew array<int>(_Rank);
				_M_base[0] = _I;
			}

		public:
			Index(int _I0, int _I1)
			{
				_Rank = 2;
				_M_base = gcnew array<int>(_Rank);
				_M_base[0] = _I0;
				_M_base[1] = _I1;
			}

		public:
			Index(int _I0, int _I1, int _I2)
			{
				_Rank = 3;
				_M_base = gcnew array<int>(_Rank);
				_M_base[0] = _I0;
				_M_base[1] = _I1;
				_M_base[2] = _I2;
			}

		public:
			property int rank
			{
				int get()
				{
					return _Rank;
				}
			}

		public:
			static operator int(Index^ idx)
			{
				return 0;
			}

			property int default[int]
			{
				int get(int i)
				{
					return _M_base[i];
				}
			}

			static property Index^ Default_Value
			{
				Index^ get();
			}
		};
	}
}
