#include "Accelerator.h"
#include "Accelerator_View.h"
#include "Native_Accelerator_View.h"
#include <vcclr.h>          // PtrToStringChars

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;

namespace Campy {
	namespace Types {
		Accelerator_View::Accelerator_View()
		{
			this->_native = (void*) new Native_Accelerator_View();
		}

		void Accelerator_View::flush()
		{
		}

		Accelerator^ Accelerator_View::get_accelerator()
		{
			Native_Accelerator * na = ((Native_Accelerator_View*)(this->_native))->get_accelerator();
			Accelerator^ result = gcnew Accelerator();
			result->_native = na;
			return result;
		}

		void Accelerator_View::wait()
		{
			Native_Accelerator_View * nav = (Native_Accelerator_View*)(this->_native);
			nav->wait();
		}

		void* Accelerator_View::native()
		{
			return (void*)this->_native;
		}

	}
}
