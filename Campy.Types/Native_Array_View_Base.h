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

			Native_Array_View_Base() {}
			Native_Array_View_Base(int num_elements, int byte_size_of_element, void * ptr, char * representation) {}

			// Basic API.
			virtual void Discard_Data() = 0;
			virtual void * Get(int i) = 0;
			virtual Native_Array_View_Base * Section(int _I0, int _E0) = 0;
			virtual void Set(int i, void * value) = 0;
			virtual void Synchronize() = 0;
			virtual void Synchronize_Async() = 0;
		};
	}
}

#pragma managed(pop)
