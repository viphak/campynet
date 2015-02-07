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

		System::Collections::Generic::List<Accelerator^>^ Accelerator::Get_All()
		{
			System::Collections::Generic::List<Accelerator^>^ result = gcnew System::Collections::Generic::List<Accelerator^>();
			Native_Accelerator** list = Native_Accelerator::Get_All();
			for (int i = 0; *list; ++i)
			{
				Accelerator^ a = gcnew Accelerator();
				a->_native = (void*)*list;
				list++;
				result->Add(a);
			}
			return result;
		}

		Accelerator_View^ Accelerator::Get_Default_View()
		{
			Accelerator_View^ result = gcnew Accelerator_View();
			return result;
		}

		bool Accelerator::Is_Emulated::get()
		{
			return ((Native_Accelerator*)_native)->Get_Is_Emulated();
		}

		bool Accelerator::Set_Default(String^ path)
		{
			pin_ptr<const wchar_t> str1 = PtrToStringChars(path);
			return Native_Accelerator::Set_Default(str1);
		}

		String^ Accelerator::Description()
		{
			String^ result = gcnew String(((Native_Accelerator*)this->_native)->Get_Description().c_str());
			return result;
		}

		String^ Accelerator::Device_Path()
		{
			String^ result = gcnew String(((Native_Accelerator*)this->_native)->Get_Device_Path().c_str());
			return result;
		}

		void* Accelerator::native()
		{
			return (void*)this->_native;
		}

		Accelerator^ Accelerator::Default_Value::get()
		{
			return Accelerator::default_value;
		}

//		Accelerator^ Accelerator::default_value = gcnew Accelerator();
	}
}
