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

		public:
			Accelerator_View();
			void flush();
			Accelerator^ get_accelerator();
			void wait();

		public:
			// Native accelerator, provided for kernels.
			void * native();
		};
	}
}