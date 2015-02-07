#include "Extent.h"
#include "Index.h"

using namespace System;

namespace Campy {
	namespace Types {
		Index^ Index::Default_Value::get()
		{
			return Index::default_value;
		}
	}
}