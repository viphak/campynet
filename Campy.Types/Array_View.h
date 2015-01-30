// Array_View.h

#pragma once
#include "Extent.h"
#include "Index.h"
#include "Native_Array_View.h"

using namespace System;
using namespace System::Runtime::InteropServices;


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
				GCHandle gchandle;
				static array<_Value_type>^ default_data = gcnew array<_Value_type>(1);
				static Array_View^ default_value = gcnew Array_View(1, default_data);

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
				static property Array_View^ Default_Value
				{
					Array_View^ get();
				}

			public:
				// Native array view, provided for kernels.
				void * native();
			};
	}
}