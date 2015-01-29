#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Array_View.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		template<typename _Value_type, int _Rank = 1>
		Native_Array_View<_Value_type, _Rank>::Native_Array_View(int length, _Value_type * data)
		{
			native = (void*) new array_view<_Value_type, _Rank>(length, data);
		}

		template<typename _Value_type, int _Rank = 1>
		Native_Array_View<_Value_type, _Rank>::Native_Array_View()
		{
			native = (void*)0;
		}

		template<typename _Value_type, int _Rank = 1>
		void Native_Array_View<_Value_type, _Rank>::synchronize()
		{
			((array_view<_Value_type, _Rank>*)native)->synchronize();
		}


		// Instantiate templates.
		template Native_Array_View<int, 1>;

	}
}