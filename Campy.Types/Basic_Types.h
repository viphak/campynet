#pragma once

namespace Campy {
	namespace Types {

		public ref class Base_Tile_Static
		{
		public:
			Base_Tile_Static(){};
		};


		public enum Access_Type
		{
			access_type_auto,
			access_type_none,
			access_type_read,
			access_type_read_write,
			access_type_write
		};
	}
}