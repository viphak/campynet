#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Extent.h"
#include "Native_Tiled_Extent.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		Native_Tiled_Extent::Native_Tiled_Extent(Native_Extent * e)
			: Native_Extent(*e)
		{
			_rank = 1;
			_dims[0] = 32;
			deal_with_incredibly_stupid_design_decision_in_cppamp();
		}

		Native_Tiled_Extent::Native_Tiled_Extent(int _I0, Native_Extent * e)
			: Native_Extent(*e)
		{
			_rank = 1;
			_dims[0] = _I0;
			deal_with_incredibly_stupid_design_decision_in_cppamp();
		}

		Native_Tiled_Extent::Native_Tiled_Extent(int _I0, int _I1, Native_Extent * e)
			: Native_Extent(*e)
		{
			_rank = 2;
			_dims[0] = _I0;
			_dims[1] = _I1;
			deal_with_incredibly_stupid_design_decision_in_cppamp();
		}

		Native_Tiled_Extent::Native_Tiled_Extent(int _I0, int _I1, int _I2, Native_Extent * e)
			: Native_Extent(*e)
		{
			_rank = 3;
			_dims[0] = _I0;
			_dims[1] = _I1;
			_dims[2] = _I2;
			deal_with_incredibly_stupid_design_decision_in_cppamp();
		}

		Native_Tiled_Extent::Native_Tiled_Extent(int rank, int * dim, Native_Extent * e)
			: Native_Extent(*e)
		{
			_rank = rank;
			for (int i = 0; i < _rank; ++i)
				_dims[i] = dim[i];
			deal_with_incredibly_stupid_design_decision_in_cppamp();
		}

		void Native_Tiled_Extent::deal_with_incredibly_stupid_design_decision_in_cppamp()
		{
			switch (_rank)
			{
			case 1:
				switch (_dims[0])
				{
				case 16:
					{
						   extent<1> * e = (extent<1> *)this->native;
						   tiled_extent<16> * te = new tiled_extent<16>(e->tile<64>());
						   break;
					}
				case 32:
					{
						extent<1> * e = (extent<1> *)this->native;
						tiled_extent<32> * te = new tiled_extent<32>(e->tile<32>());
						this->native = (void*)te;
						break;
					}
				case 64:
					{
						   extent<1> * e = (extent<1> *)this->native;
						   tiled_extent<64> * te = new tiled_extent<64>(e->tile<64>());
						   this->native = (void*)te;
						break;
					}
				case 128:
					{
							extent<1> * e = (extent<1> *)this->native;
							tiled_extent<128> * te = new tiled_extent<128>(e->tile<64>());
							this->native = (void*)te;
						break;
					}
				case 256:
					{
							extent<1> * e = (extent<1> *)this->native;
							tiled_extent<256> * te = new tiled_extent<256>(e->tile<64>());
							this->native = (void*)te;
						break;
					}
				case 512:
					{
							extent<1> * e = (extent<1> *)this->native;
							tiled_extent<512> * te = new tiled_extent<512>(e->tile<64>());
							this->native = (void*)te;
							break;
					}
				case 1024:
					{
							 extent<1> * e = (extent<1> *)this->native;
							 tiled_extent<1024> * te = new tiled_extent<1024>(e->tile<64>());
							 this->native = (void*)te;
							 break;
					}
				default:
					break;
				}
				break;
			default:
				break;
			}
		}
	}
}
