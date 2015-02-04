// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once
#pragma managed(push, off)

namespace Campy {
	namespace Types {

		class Native_Array_View_Base
		{
		public:
			void * _data_ptr;
			char * _element_representation;
			int _num_elements;
			int _byte_size_of_element;
			void * native;

			Native_Array_View_Base();
			Native_Array_View_Base(int num_elements, int byte_size_of_element, void * ptr, char * representation);

			// Basic API.
			virtual void synchronize();
			virtual void * get(int i);
			virtual void set(int i, void * value);
		};
	}
}

#pragma managed(pop)
