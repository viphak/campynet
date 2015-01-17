// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once

#pragma managed(push, off)

namespace Campy {
	namespace Types {

		template<int _Rank = 1>
		class Native_Extent
		{
		public:
			void * ne;
			Native_Extent();
			Native_Extent(int size);
		};
	}
}

#pragma managed(pop)
