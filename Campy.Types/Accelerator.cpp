#include "Accelerator.h"
#include "Accelerator_View.h"
#include "Native_Accelerator.h"
#include <vcclr.h>          // PtrToStringChars

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;

namespace Campy {
	namespace Types {
		Accelerator::Accelerator()
		{
		}

		System::Collections::Generic::List<Accelerator^>^ Accelerator::get_all()
		{
			System::Collections::Generic::List<Accelerator^>^ result = gcnew System::Collections::Generic::List<Accelerator^>();
			Native_Accelerator** list = Native_Accelerator::get_all();
			for (int i = 0; *list; ++i)
			{
				Accelerator^ a = gcnew Accelerator();
				a->_native = (void*)*list;
				list++;
				result->Add(a);
			}
			return result;
		}

		Accelerator_View^ Accelerator::get_default_view()
		{
			Accelerator_View^ result = gcnew Accelerator_View();
			return result;
		}

		bool Accelerator::is_emulated::get()
		{
			return ((Native_Accelerator*)_native)->is_emulated();
		}

		bool Accelerator::set_default(String^ path)
		{
			pin_ptr<const wchar_t> str1 = PtrToStringChars(path);
			return Native_Accelerator::set_default(str1);
		}

		String^ Accelerator::description()
		{
			String^ result = gcnew String(((Native_Accelerator*)this->_native)->description().c_str());
			return result;
		}

		String^ Accelerator::device_path()
		{
			String^ result = gcnew String(((Native_Accelerator*)this->_native)->device_path().c_str());
			return result;
		}

		void* Accelerator::native()
		{
			return (void*)this->_native;
		}

	}
}
