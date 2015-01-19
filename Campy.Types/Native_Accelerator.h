// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once
#include <string>

#pragma managed(push, off)

namespace Campy {
	namespace Types {

		class Native_Accelerator
		{
		public:
			void * na; // concurrency::accelerator
			Native_Accelerator();
			bool is_emulated();
			static Native_Accelerator** get_all();
			static bool set_default(std::wstring path);
			std::wstring description();
			std::wstring device_path();
		};
	}
}

#pragma managed(pop)
