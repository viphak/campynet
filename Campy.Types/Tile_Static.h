// Array_View.h

#pragma once
#include "Basic_Types.h"
#include "Extent.h"
#include "Index.h"
#include "Native_Array_View.h"

using namespace System;
using namespace System::Runtime::InteropServices;


namespace Campy {
	namespace Types {

		generic<typename _Value_type>
			public ref class Tile_Static : Base_Tile_Static
			{

			private:
				int _Rank = 1;
				int _length;
				Extent ^ _extent;
				void * _native;
				GCHandle gchandle;

			public:
				Tile_Static(int length);

				property _Value_type default[int]
				{
					_Value_type get(int i);
					void set(int i, _Value_type value);
				}

				property int Length
				{
					int get()
					{
						return _length;
					}
				}
			};
	}
}