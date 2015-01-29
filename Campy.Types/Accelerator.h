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

		public:
			Accelerator();
			static System::Collections::Generic::List<Accelerator^>^ get_all();
			static bool set_default(String^ path);
			static Accelerator_View^ get_default_view();
			property bool is_emulated
			{
				bool get();
			}
			String^ description();
			String^ device_path();

		public:
			// Native accelerator, provided for kernels.
			void * native();
		};
	}
}