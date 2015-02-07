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

		void Accelerator_View::Flush()
		{
		}

		Accelerator^ Accelerator_View::Get_Accelerator()
		{
			Native_Accelerator * na = ((Native_Accelerator_View*)(this->_native))->Get_Accelerator();
			Accelerator^ result = gcnew Accelerator();
			result->_native = na;
			return result;
		}

		void Accelerator_View::Wait()
		{
			Native_Accelerator_View * nav = (Native_Accelerator_View*)(this->_native);
			nav->Wait();
		}

		void* Accelerator_View::native()
		{
			return (void*)this->_native;
		}

		Accelerator_View^ Accelerator_View::Default_Value::get()
		{
			return Accelerator_View::default_value;
		}
	}
}
