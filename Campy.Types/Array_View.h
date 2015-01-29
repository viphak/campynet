// Array_View.h

#pragma once
#include "Extent.h"
#include "Index.h"

using namespace System;

namespace Campy {
	namespace Types {

		generic<typename _Value_type>
			public ref class Array_View
			{

			private:
				int _Rank = 1;
				array<_Value_type> ^ _data;
				int _length;
				Extent ^ _extent;
				void * _native;

			public:
				Array_View(int length, array<_Value_type> ^% data);

				property Extent^ extent
				{
					Extent^ get();
					void set(Extent^ extent);
				}

				property _Value_type default[int]
				{
					_Value_type get(int i);
					void set(int i, _Value_type value);
				}

				void synchronize();
				array<_Value_type>^ data();

			public:
				// Native array view, provided for kernels.
				void * native();
			};
	}
}