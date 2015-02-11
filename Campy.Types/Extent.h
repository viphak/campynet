// Extent.h

#pragma once
#include "Index.h"

using namespace System;

namespace Campy {
	namespace Types {

		ref class Tiled_Extent;

		public ref class Extent
		{
		internal:
			array<int>^ _M_base;
			void * _native;
			static Extent^ default_value = gcnew Extent();

		public:
			int _Rank;
			Extent();
			Extent(Extent^ e);
			Extent(int _I0);
			Extent(int _I0, int _I1);
			Extent(int _I0, int _I1, int _I2);
			Extent(array<int>^ _Array);
			Tiled_Extent^ Tile(int _I0);
			Tiled_Extent^ Tile(int _I0, int _I1);
			Tiled_Extent^ Tile(int _I0, int _I1, int _I2);
			int Size();
			int operator[](int i);
			int operator[](Index^ i);
			static Extent^ operator +(Extent^ _Lhs, Index^ _Rhs);
			static Extent^ operator ++(Extent^ _Lhs);
			static Extent^ operator -(Extent^ _Lhs, Index^ _Rhs);
			static Extent^ operator --(Extent^ _Lhs);
			void * native();
			static property Extent^ Default_Value
			{
				Extent^ get();
			}

			// C# does not support post-increment, post-decrement, +=, -=, etc. operator overloading.
		};
	}
}
