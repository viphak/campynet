// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once
#pragma managed(push, off)

#include "Native_Array_Base.h"
#include "Native_Array_View_Base.h"

namespace Campy {
	namespace Types {

		// Note native array view isn't templated. This is because
		// we would then have to create a type corresponding to a C# element
		// type, and then compile that here. Instead, the "native" pointer
		// will be allocated and assigned via Array_View over in managed C++
		// world.
		template<typename T>
		class Native_Array : public Native_Array_Base
		{
		public:
			Native_Array();
			Native_Array(int rank, int * dims, int byte_size_of_element, char * representation);
			virtual void * Get(int i);
			virtual Native_Array_View_Base * Section(int _I0, int _E0);
			virtual void Set(int i, void * value);
		};
	}
}

#pragma managed(pop)
