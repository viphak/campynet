
#include "stdafx.h"
#include "Accelerator.h"
#include "Accelerator_View.h"
#include "Array_View_Base.h"
#include "Basic_Types.h"
#include "Extent.h"
#include "Index.h"
#include "Native_Extent.h"
#include "Tiled_Extent.h"
#include "Tiled_Index.h"
#include "Tile_Barrier.h"
#include "Utils.h"

using namespace System;

namespace Campy {
	namespace Types {

		bool TypesUtility::IsSimpleCampyType(Type^ t)
		{
			for (;;)
			{
				if (t == nullptr)
					break;
				if (IsCampyArrayViewType(t))
					return true;
				if (t == Accelerator::typeid)
					return true;
				if (t == Accelerator_View::typeid)
					return true;
				if (t == Index::typeid)
					return true;
				if (t == Extent::typeid)
					return true;
				if (t == Tile_Barrier::typeid)
					return true;
				if (IsCampyTileStaticType(t))
					return true;
				if (t == Tiled_Extent::typeid)
					return true;
				if (t == Tiled_Index::typeid)
					return true;
				t = t->BaseType;
			}
			return false;
		}

		bool TypesUtility::IsCampyArrayViewType(Type^ t)
		{
			for (;;)
			{
				if (t == nullptr)
					break;
				if (t == Array_View_Base::typeid)
					return true;
				t = t->BaseType;
			}
			return false;
		}

		bool TypesUtility::IsCampyAcceleratorType(Type^ t)
		{
			for (;;)
			{
				if (t == nullptr)
					break;
				if (t == Accelerator::typeid)
					return true;
				t = t->BaseType;
			}
			return false;
		}

		bool TypesUtility::IsCampyAcceleratorViewType(Type^ t)
		{
			for (;;)
			{
				if (t == nullptr)
					break;
				if (t == Accelerator_View::typeid)
					return true;
				t = t->BaseType;
			}
			return false;
		}

		bool TypesUtility::IsCampyIndexType(Type^ t)
		{
			for (;;)
			{
				if (t == nullptr)
					break;
				if (t == Index::typeid)
					return true;
				t = t->BaseType;
			}
			return false;
		}

		bool TypesUtility::IsCampyExtentType(Type^ t)
		{
			for (;;)
			{
				if (t == nullptr)
					break;
				if (t == Extent::typeid)
					return true;
				t = t->BaseType;
			}
			return false;
		}

		bool TypesUtility::IsCampyTileStaticType(Type^ t)
		{
			for (;;)
			{
				if (t == nullptr)
					break;
				if (t == Base_Tile_Static::typeid)
					return true;
				t = t->BaseType;
			}
			return false;
		}

		bool TypesUtility::IsBaseType(Type^ t, Type^ basetype)
		{
			for (;;)
			{
				if (t == nullptr)
					break;
				if (t == basetype)
					return true;
				t = t->BaseType;
			}
			return false;
		}
	}
}
