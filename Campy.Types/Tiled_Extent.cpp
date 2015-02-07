#include "Extent.h"
#include "Index.h"
#include "Tiled_Extent.h"
#include "Native_Extent.h"
#include "Native_Tiled_Extent.h"

using namespace System;

namespace Campy {
	namespace Types {

		Tiled_Extent::Tiled_Extent(Extent^ e)
			: Extent(e)
		{
			_Tile_Rank = 1;
			_Tile_Dim = gcnew array<int, 1>(_Tile_Rank);
			_Tile_Dim[0] = 1;
			_native = (void*) new Native_Tiled_Extent(1, (Native_Extent*)e->_native);
		}

		Tiled_Extent::Tiled_Extent(int _I0, Extent^ e)
			: Extent(e)
		{
			_Tile_Rank = 1;
			_Tile_Dim = gcnew array<int, 1>(_Tile_Rank);
			_Tile_Dim[0] = _I0;
			_native = (void*) new Native_Tiled_Extent(_I0, (Native_Extent*)e->_native);
		}

		Tiled_Extent::Tiled_Extent(int _I0, int _I1, Extent^ e)
			: Extent(e)
		{
			_Tile_Rank = 2;
			_Tile_Dim = gcnew array<int, 1>(_Tile_Rank);
			_Tile_Dim[0] = _I0;
			_Tile_Dim[1] = _I1;
			_native = (void*) new Native_Tiled_Extent(_I0, _I1, (Native_Extent*)e->_native);
		}

		Tiled_Extent::Tiled_Extent(int _I0, int _I1, int _I2, Extent^ e)
			: Extent(e)
		{
			_Tile_Rank = 3;
			_Tile_Dim = gcnew array<int, 1>(_Tile_Rank);
			_Tile_Dim[0] = _I0;
			_Tile_Dim[1] = _I1;
			_Tile_Dim[2] = _I1;
			_native = (void*) new Native_Tiled_Extent(_I0, _I1, _I2, (Native_Extent*)e->_native);
		}

		Tiled_Extent::Tiled_Extent(array<int>^ _Array, Extent^ e)
			: Extent(e)
		{
			_Tile_Rank = _Array->Length;
			_Tile_Dim = gcnew array<int, 1>(_Tile_Rank);
			pin_ptr<int> ptr = &_Tile_Dim[0];
			for (int i = 0; i < _Tile_Rank; ++i)
				_Tile_Dim[i] = _Array[i];
			_native = (void*) new Native_Tiled_Extent(_Tile_Rank, (int*)ptr, (Native_Extent*)e->_native);
		}
	}
}
