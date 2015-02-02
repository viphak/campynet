// Extent.h

#pragma once

using namespace System;

namespace Campy {
	namespace Types {

		public ref class Tile_Barrier
		{
		public:
			Tile_Barrier();
			void wait();
			void wait_with_all_memory_fence();
			void wait_with_global_memory_fence();
			void wait_with_tile_static_memory_fence();
		};
	}
}
