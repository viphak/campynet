#pragma once

using namespace System;

namespace Campy {
	namespace Types {

		public ref class TypesUtility
		{
		public:
			static bool IsSimpleCampyType(Type^ t);
			static bool IsCampyArrayType(Type^ t);
			static bool IsCampyArrayViewType(Type^ t);
			static bool IsCampyAcceleratorType(Type^ t);
			static bool IsCampyAcceleratorViewType(Type^ t);
			static bool IsCampyIndexType(Type^ t);
			static bool IsCampyExtentType(Type^ t);
			static bool IsCampyTileStaticType(Type^ t);
			static bool IsBaseType(Type^ type, Type^ basetype);
		};
	}
}
