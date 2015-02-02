#include "stdafx.h"
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
			local = gcnew Index();
			global = gcnew Index();
			tile = gcnew Index();
			tile_origin = gcnew Index();
			barrier = gcnew Tile_Barrier();
		}
	}
}
