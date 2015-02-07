// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once
#include <string>
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

			enum access_type
			{
				access_type_none,
				access_type_read,
				access_type_write,
				access_type_read_write = access_type_read | access_type_write,
				access_type_auto,
			};



			void * native; // concurrency::accelerator
			Native_Accelerator();
			static Native_Accelerator** Get_All();
			static bool Set_Default(std::wstring path);
			std::wstring Get_Device_Path();
			unsigned int Get_Version();
			std::wstring Get_Description();
			bool Get_Is_Debug();
			bool Get_Is_Emulated();
			bool Get_Has_Display();
			bool Get_Supports_Double_Precision();
			bool Get_Supports_Limited_Double_Precision();
			bool Get_Supports_Cpu_shared_Memory();
			Native_Accelerator_View* Get_Default_View();
			size_t Get_Dedicated_Memory();
			access_type Get_Default_Cpu_Access_Type();
			bool Set_Default_Cpu_Access_Type(access_type _Default_cpu_access_type);
			Native_Accelerator_View Create_View(queuing_mode qmode = queuing_mode_automatic);

			bool operator==(const Native_Accelerator &_Other);
			bool operator!=(const Native_Accelerator &_Other);

		};
	}
}

#pragma managed(pop)
