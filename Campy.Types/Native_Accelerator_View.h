// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once

#pragma managed(push, off)

namespace Campy {
	namespace Types {

		class Native_Accelerator;

		class Native_Accelerator_View
		{
		public:
			void * nav; // concurrency::accelerator_view
			Native_Accelerator_View();
			void flush();
			Native_Accelerator* get_accelerator();
			void wait();
		};
	}
}

#pragma managed(pop)
