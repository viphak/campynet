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
			Index^ local;
			Index^ global;
			Index^ tile;
			Index^ tile_origin;
			Tile_Barrier^ barrier;
			Tiled_Index();
		};
	}
}
