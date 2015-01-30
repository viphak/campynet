// Accelerator.h

#pragma once

using namespace System;
using namespace System::Collections::Generic;

namespace Campy {
	namespace Types {

		ref class Accelerator;

		public ref class Accelerator_View
		{
		private:
			void * _native;
			static Accelerator_View^ default_value = gcnew Accelerator_View();

		public:
			Accelerator_View();
			void flush();
			Accelerator^ get_accelerator();
			void wait();
			static property Accelerator_View^ Default_Value
			{
				Accelerator_View^ get();
			}

		public:
			// Native accelerator, provided for kernels.
			void * native();
		};
	}
}