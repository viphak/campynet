// Accelerator.h

#pragma once

using namespace System;
using namespace System::Collections::Generic;

namespace Campy {
	namespace Types {

		ref class Accelerator_View;

		public ref class Accelerator
		{
		internal:
			void * _native;
			static Accelerator^ default_value;

		public:
			Accelerator();
			static System::Collections::Generic::List<Accelerator^>^ Get_All();
			static bool Set_Default(String^ path);
			static Accelerator_View^ Get_Default_View();
			property bool Is_Emulated
			{
				bool get();
			}
			String^ Description();
			String^ Device_Path();
			static property Accelerator^ Default_Value
			{
				Accelerator^ get();
			}
			// Native accelerator, provided for kernels.
			void * native();
		};
	}
}