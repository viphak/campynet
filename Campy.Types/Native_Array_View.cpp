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
			std::cout << "Here3" << std::endl;
			native = (void*) new array_view<T, 1>(length, (T*)data);
			std::cout << "Here2" << std::endl;
			array_view<T, 1> xxx = *(array_view<T, 1>*)native;
			std::cout << "Here " << native << std::endl;
		}

		template<typename T>
		Native_Array_View<T>::Native_Array_View()
		{
			native = (void*)0;
		}

		template<typename T>
		Native_Array_View_Base * Native_Array_View<T>::Section(int _I0, int _E0)
		{
			array_view<T, 1> * n = (array_view<T, 1>*)native;
			array_view<T, 1> s = n->section(_I0, _E0);
			void * x = (void *)new array_view<T, 1>(s);
			Native_Array_View<T> * new_array_view = new Native_Array_View<T>();
			new_array_view->native = x;
			return new_array_view;
		}

		template<typename T>
		void Native_Array_View<T>::Synchronize()
		{
			// load unmanaged type and call synchronize.
			array_view<T, 1> * n = (array_view<T, 1>*)native;
			n->synchronize();
		}

		template<typename T>
		void Native_Array_View<T>::Synchronize_Async()
		{
			// load unmanaged type and call synchronize.
			array_view<T, 1> * n = (array_view<T, 1>*)native;
			n->synchronize_async();
		}

		template<typename T>
		void * Native_Array_View<T>::Get(int i)
		{
			array_view<T, 1> * n = (array_view<T, 1>*)native;
			return (void *)&((*n)[i]);
		}

		template<typename T>
		void Native_Array_View<T>::Set(int i, void * value)
		{
			(*(array_view<T, 1>*)native)[i] = *(T*) value;
		}

		template<typename T>
		void Native_Array_View<T>::Discard_Data()
		{
			array_view<T, 1>* nav = (array_view<T, 1>*)this->native;
			nav->discard_data();
		}

		// Instantiate templates.
		template Native_Array_View<int>;
		template Native_Array_View<unsigned int>;
		template Native_Array_View<long>;
		template Native_Array_View<unsigned long>;
	}
}