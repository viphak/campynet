// Extent.h

#pragma once
#include "Index.h"

using namespace System;

namespace Campy {
	namespace Types {

		public ref class Extent
		{
		internal:
			int _Rank;
			array<int>^ _M_base;
			void * _native;

		public:
			Extent();
			Extent(int _I0);
			Extent(int _I0, int _I1);
			Extent(int _I0, int _I1, int _I2);
			Extent(array<int>^ _Array);
			int size();
			int operator[](int i);
			int operator[](Index^ i);
			static Extent^ operator +(Extent^ _Lhs, Index^ _Rhs);
			static Extent^ operator ++(Extent^ _Lhs);
			static Extent^ operator -(Extent^ _Lhs, Index^ _Rhs);
			static Extent^ operator --(Extent^ _Lhs);
			void * native();

			// C# does not support post-increment, post-decrement, +=, -=, etc. operator overloading.
		};
	}
}
