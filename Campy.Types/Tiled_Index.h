// Extent.h

#pragma once

using namespace System;

namespace Campy {
	namespace Types {

		ref class Tile_Barrier;
		ref class Index;

		public ref class Tiled_Index
		{
		internal:
			int _Rank;

		public:
			Index^ Local;
			Index^ Global;
			Index^ Tile;
			Index^ Tile_Origin;
			Tile_Barrier^ Barrier;
			Tiled_Index();
		};
	}
}
