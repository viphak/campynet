// Compiles with CLR or native.
// This class must exist in order to have an implementation using C++ AMP.
// C++ AMP's "amp.h" cannot compile in a /clr compiled source file, not even in
// pragma'ed unmanaged mode.

#pragma once
#pragma managed(push, off)

namespace Campy {
	namespace Types {

		template<typename _Value_type>
		class Native_Array_View
		{
		public:
			void * native;
			Native_Array_View();
			Native_Array_View(int length, _Value_type * ptr);
			void synchronize();
			_Value_type operator [](int i) const;
			_Value_type & store(int i);
		};
	}
}

#pragma managed(pop)
