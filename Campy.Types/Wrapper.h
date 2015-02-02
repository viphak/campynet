
#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;

namespace Campy {
	namespace Types {


		public ref class Wrapper
		{

			GCHandle thisHandle;
		public:
			Wrapper()
			{
				thisHandle = GCHandle.Alloc(this, GCHandleType::Normal);
			}

			~Wrapper() // Dispose
			{
				if (thisHandle.IsAllocated)
					thisHandle.Free;
			}

			!Wrapper() // Finalize
			{
				//Native resource releasing
			}
		}
	}
}
