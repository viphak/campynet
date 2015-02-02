#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Array_View.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		template<typename _Value_type>
		Native_Array_View<_Value_type>::Native_Array_View(int length, _Value_type * data)
		{
			native = (void*) new array_view<_Value_type, 1>(length, data);
		}

		template<typename _Value_type>
		Native_Array_View<_Value_type>::Native_Array_View()
		{
			native = (void*)0;
		}

		template<typename _Value_type>
		void Native_Array_View<_Value_type>::synchronize()
		{
			((array_view<_Value_type, 1>*)native)->synchronize();
		}

		template<typename _Value_type>
		_Value_type Native_Array_View<_Value_type>::operator [](int i) const
		{
			return (_Value_type)((*(array_view<_Value_type, 1>*)native)[i]);
		}

		template<typename _Value_type>
		_Value_type & Native_Array_View<_Value_type>::store(int i)
		{
			return (_Value_type&)((array_view<_Value_type, 1>*)native)[i];
		}

		// Instantiate templates.
		template Native_Array_View<int>;

	}
}