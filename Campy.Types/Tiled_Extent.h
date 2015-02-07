// Extent.h

#pragma once
#include "Index.h"
#include "Extent.h"

using namespace System;

namespace Campy {
	namespace Types {

		public ref class Tiled_Extent : Extent
		{
		internal:
			int _Tile_Rank;
			array<int>^ _Tile_Dim;

		public:
			Tiled_Extent(Extent^ e);
			Tiled_Extent(int _I0, Extent^ e);
			Tiled_Extent(int _I0, int _I1, Extent^ e);
			Tiled_Extent(int _I0, int _I1, int _I2, Extent^ e);
			Tiled_Extent(array<int>^ _Array, Extent^ e);
			property int tile_Rank
			{
				int get()
				{
					return _Tile_Rank;
				}
			}
			property array<int>^ Tile_Dims
			{
				array<int>^ get()
				{
					return _Tile_Dim;
				}
			}

		};
	}
}
