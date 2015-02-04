#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Array_View_Base.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		Native_Array_View_Base::Native_Array_View_Base(int length, int element_length, void * data, char * representation)
		{
			native = (void*)data;
		}

		Native_Array_View_Base::Native_Array_View_Base()
		{
			native = (void*)0;
		}

		void Native_Array_View_Base::synchronize()
		{
		}

		void * Native_Array_View_Base::get(int i)
		{
			return 0;
		}

		void Native_Array_View_Base::set(int i, void * value)
		{
		}
	}
}