#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Extent.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		template<int _Rank = 1>
		Native_Extent<_Rank>::Native_Extent(int size)
		{
			extent<1> * e;
			if (_Rank == 1)
			{
				e = new extent<1>(size);
				native = (void*)e;
			}
		}

		template<int _Rank = 1>
		Native_Extent<_Rank>::Native_Extent()
		{
			native = (void*) new extent<_Rank>();
		}

		// Instantiate templates.
		template Native_Extent<1>;
		template Native_Extent<2>;
		template Native_Extent<3>;

	}
}
