#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Array_View.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		template<typename T>
		Native_Array_View<T>::Native_Array_View(int length, int element_length, void * data, char * representation)
		{
			native = (void*) new array_view<T, 1>(length, (T*)data);
		}

		template<typename T>
		Native_Array_View<T>::Native_Array_View()
		{
			native = (void*)0;
		}

		template<typename T>
		void Native_Array_View<T>::synchronize()
		{
			// load unmanaged type and call synchronize.
			((array_view<T, 1>*)native)->synchronize();
		}

		template<typename T>
		void * Native_Array_View<T>::get(int i)
		{
			return (void *)&((*(array_view<T, 1>*)native)[i]);
		}

		template<typename T>
		void Native_Array_View<T>::set(int i, void * value)
		{
			(*(array_view<T, 1>*)native)[i] = *(T*) value;
		}


		// Instantiate templates.
		template Native_Array_View<int>;
		template Native_Array_View<unsigned int>;
		template Native_Array_View<long>;
		template Native_Array_View<unsigned long>;
	}
}