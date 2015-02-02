#include "Tile_Static.h"

using namespace System;

namespace Campy {
	namespace Types {

		generic<typename _Value_type>
			Tile_Static<_Value_type>::Tile_Static(int length)
			{
				this->_length = length;
			}

		generic<typename _Value_type>
			_Value_type Tile_Static<_Value_type>::default::get(int i)
			{
				Type ^ t = _Value_type::typeid;
				_Value_type v = _Value_type();
				return v;
			}

		generic<typename _Value_type>
			void Tile_Static<_Value_type>::default::set(int i, _Value_type value)
			{
			}

	}
}
