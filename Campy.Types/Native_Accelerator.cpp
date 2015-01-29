#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Accelerator.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		Native_Accelerator::Native_Accelerator()
		{
			accelerator * a = new accelerator();
			this->native = (void*)a;
		}

		bool Native_Accelerator::set_default(std::wstring path)
		{
			return accelerator::set_default(path);
		}

		bool Native_Accelerator::is_emulated()
		{
			return ((accelerator*)(this->native))->is_emulated;
		}

		std::wstring Native_Accelerator::description()
		{
			return ((accelerator*)(this->native))->description;
		}

		std::wstring Native_Accelerator::device_path()
		{
			return ((accelerator*)(this->native))->device_path;
		}

		Native_Accelerator** Native_Accelerator::get_all()
		{
			std::vector<accelerator> ar = accelerator::get_all();
			Native_Accelerator** result = new Native_Accelerator*[ar.size() + 1];
			Native_Accelerator** p = result;
			int s = ar.size();
			for (int i = 1; i < s; ++i)
			{
				accelerator a = ar[i];
				Native_Accelerator * the_na = new Native_Accelerator();
				the_na->native = (void*) new accelerator(a);
				*p++ = the_na;
			}
			*p = 0;
			return result;
		}
	}
}
