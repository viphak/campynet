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
			int _Tile_rank;
			array<int>^ _Tile_dim;

		public:
			Tiled_Extent(Extent^ e);
			Tiled_Extent(int _I0, Extent^ e);
			Tiled_Extent(int _I0, int _I1, Extent^ e);
			Tiled_Extent(int _I0, int _I1, int _I2, Extent^ e);
			Tiled_Extent(array<int>^ _Array, Extent^ e);
			property int tile_rank
			{
				int get()
				{
					return _Tile_rank;
				}
			}
			property array<int>^ tile_dims
			{
				array<int>^ get()
				{
					return _Tile_dim;
				}
			}

		};
	}
}
