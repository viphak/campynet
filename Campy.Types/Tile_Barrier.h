// Extent.h

#pragma once

using namespace System;

namespace Campy {
	namespace Types {

		public ref class Tile_Barrier
		{
		public:
			Tile_Barrier();
			void Wait();
			void Wait_With_All_Memory_Fence();
			void Wait_With_Global_Memory_Fence();
			void Wait_With_Tile_Static_Memory_Fence();
		};
	}
}
