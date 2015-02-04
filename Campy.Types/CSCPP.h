
#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;

namespace Campy {
	namespace Types {

		// Conversion from C# type into C++ type.
		public ref class CSCPP
		{
		public:
			static System::String^ ConvertToCPP(System::Type^ type, int level);
			static System::String^ ConvertToCPPCLI(System::Type^ type, int level);
		};
	}
}

