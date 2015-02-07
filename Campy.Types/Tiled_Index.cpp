#include "Extent.h"
#include "Index.h"
#include "Native_Extent.h"
#include "Tiled_Extent.h"
#include "Tiled_Index.h"
#include "Tile_Barrier.h"

using namespace System;

namespace Campy {
	namespace Types {

		Tiled_Index::Tiled_Index()
		{
			_Rank = 1;
			Local = gcnew Index();
			Global = gcnew Index();
			Tile = gcnew Index();
			Tile_Origin = gcnew Index();
			Barrier = gcnew Tile_Barrier();
		}
	}
}
