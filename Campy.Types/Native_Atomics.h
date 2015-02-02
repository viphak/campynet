// Compiles with native.

#pragma once
#include <amp.h>

namespace AMP {
	inline int Atomic_Fetch_Add(concurrency::array_view<int, 1> _Dest, int index, int _Value) restrict(amp)
	{
		int orig = concurrency::atomic_fetch_add(&_Dest[index], _Value);
		return orig;
	};
}
