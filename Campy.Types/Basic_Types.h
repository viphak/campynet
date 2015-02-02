#pragma once

namespace Campy {
	namespace Types {
		enum access_type
		{
			access_type_none,
			access_type_read,
			access_type_write,
			access_type_read_write = access_type_read | access_type_write,
			access_type_auto,
		};
	}
}