// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once
#include <string>
#include "Basic_Types.h"
#pragma managed(push, off)

namespace Campy {
	namespace Types {

		class Native_Accelerator_View;

		class Native_Accelerator
		{
		public:
			enum queuing_mode {
				queuing_mode_immediate,
				queuing_mode_automatic
			};

			void * native; // concurrency::accelerator
			Native_Accelerator();
			static Native_Accelerator** get_all();
			static bool set_default(std::wstring path);
			std::wstring get_device_path();
			unsigned int get_version();
			std::wstring get_description();
			bool get_is_debug();
			bool get_is_emulated();
			bool get_has_display();
			bool get_supports_double_precision();
			bool get_supports_limited_double_precision();
			bool get_supports_cpu_shared_memory();
			Native_Accelerator_View* get_default_view();
			size_t get_dedicated_memory();
			access_type get_default_cpu_access_type();
			bool set_default_cpu_access_type(access_type _Default_cpu_access_type);
			Native_Accelerator_View create_view(queuing_mode qmode = queuing_mode_automatic);

			bool operator==(const Native_Accelerator &_Other);
			bool operator!=(const Native_Accelerator &_Other);

		};
	}
}

#pragma managed(pop)
