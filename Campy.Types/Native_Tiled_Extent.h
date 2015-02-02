// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once

#pragma managed(push, off)


namespace Campy {
	namespace Types {

		class Native_Extent;

		class Native_Tiled_Extent : Native_Extent
		{
		public:
			Native_Tiled_Extent(Native_Extent * e);
			Native_Tiled_Extent(int _I0, Native_Extent * e);
			Native_Tiled_Extent(int _I0, int _I1, Native_Extent * e);
			Native_Tiled_Extent(int _I0, int _I1, int _I2, Native_Extent * e);
			Native_Tiled_Extent(int rank, int * dim, Native_Extent * e);
			void deal_with_incredibly_stupid_design_decision_in_cppamp();
		};
	}
}

#pragma managed(pop)
