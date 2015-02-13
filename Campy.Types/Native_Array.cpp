#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Array.h"
#include "Native_Array_View.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		template<typename T>
		Native_Array<T>::Native_Array(int rank, int * dims, int element_length, char * representation)
		{
			if (rank == 1)
				native = (void*) new array<T, 1>(dims[0]);
			else if (rank == 2)
				native = (void*) new array<T, 2>(dims[0], dims[1]);
			else if (rank == 3)
				native = (void*) new array<T, 3>(dims[0], dims[1], dims[2]);
		}

		template<typename T>
		Native_Array<T>::Native_Array()
		{
			native = (void*)0;
		}

		template<typename T>
		Native_Array_View_Base * Native_Array<T>::Section(int _I0, int _E0)
		{
			array<T, 1> * n = (array<T, 1>*)native;
			array_view<T, 1> s = n->section(_I0, _E0);
			// Note, s has no buffers yet.
			// Convert into pointer to array_view with copy constructor.
			// We need to do this so we have a persistent handle to it.
			void * x = (void *)new array_view<T, 1>(s);
			Native_Array_View<T> * new_array_view = new Native_Array_View<T>();
			new_array_view->native = x;
			return new_array_view;
		}

		template<typename T>
		void * Native_Array<T>::Get(int i)
		{
			return (void *)&((*(array<T, 1>*)native)[i]);
		}

		template<typename T>
		void Native_Array<T>::Set(int i, void * value)
		{
			(*(array<T, 1>*)native)[i] = *(T*)value;
		}

		// Instantiate templates.
		template Native_Array<int>;
		template Native_Array<unsigned int>;
		template Native_Array<long>;
		template Native_Array<unsigned long>;
	}
}