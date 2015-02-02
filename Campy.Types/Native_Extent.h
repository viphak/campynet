// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once

#pragma managed(push, off)

namespace Campy {
	namespace Types {

		class Native_Tiled_Extent;

		class Native_Extent
		{
		public:
			void * native;
			int _rank;
			int _dims[3];
			Native_Extent();
			Native_Extent(int dim0);
			Native_Extent(int dim0, int dim2);
			Native_Extent(int dim0, int dim2, int dim3);
			Native_Extent(int rank, int* dims);
			Native_Tiled_Extent * tile();
			Native_Tiled_Extent * tile(int dim0);
			Native_Tiled_Extent * tile(int dim0, int dim2);
			Native_Tiled_Extent * tile(int dim0, int dim2, int dim3);
		};
	}
}

#pragma managed(pop)
