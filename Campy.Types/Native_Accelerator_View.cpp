#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Accelerator.h"
#include "Native_Accelerator_View.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		Native_Accelerator_View::Native_Accelerator_View()
		{
			accelerator* d = new accelerator();
			accelerator_view * av = new accelerator_view(d->get_default_view());
			this->native = av;
		}

		void Native_Accelerator_View::Flush()
		{
			((accelerator_view*)(this->native))->flush();
		}

		Native_Accelerator* Native_Accelerator_View::Get_Accelerator()
		{
			accelerator* a = new accelerator(((accelerator_view*)(this->native))->get_accelerator());
			Native_Accelerator * na = new Native_Accelerator();
			na->native = (void*)a;
			return na;
		}

		void Native_Accelerator_View::Wait()
		{
			((accelerator_view*)(this->native))->wait();
		}
	}
}
