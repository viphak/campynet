#pragma managed(push,off)

#include <amp.h>
#include <iostream>
#include "Native_Extent.h"
#include "Native_Tiled_Extent.h"

using namespace concurrency;    // Save some typing :)
using std::vector;     // Ditto. Comes from <vector> brought in by amp.h

namespace Campy {
	namespace Types {

		class Native_Tiled_Extent;

		Native_Extent::Native_Extent()
		{
			_rank = 1;
			_dims[0] = 1;
			extent<1> * e;
			e = new extent<1>(_dims[0]);
			native = (void*)e;
		}

		Native_Extent::Native_Extent(int dim0)
		{
			_rank = 1;
			_dims[0] = dim0;
			extent<1> * e;
			e = new extent<1>(_dims);
			native = (void*)e;
		}

		Native_Extent::Native_Extent(int dim0, int dim1)
		{
			_rank = 2;
			_dims[0] = dim0;
			_dims[1] = dim1;
			extent<2> * e;
			e = new extent<2>(_dims);
			native = (void*)e;
		}

		Native_Extent::Native_Extent(int dim0, int dim1, int dim2)
		{
			_rank = 3;
			_dims[0] = dim0;
			_dims[1] = dim1;
			_dims[2] = dim2;
			extent<3> * e;
			e = new extent<3>(_dims);
			native = (void*)e;
		}

		Native_Extent::Native_Extent(int rank, int * dims)
		{
			_rank = rank;
			for (int i = 0; i < rank; ++i)
				_dims[i] = dims[i];
			switch (rank)
			{
			case 1: {
						extent<1> * e = new extent<1>(dims);
						native = (void*)e;
				}
				break;
			case 2: {
						extent<2> * e = new extent<2>(dims);
						native = (void*)e;
				}
				break;
			case 3: {
						extent<3> * e = new extent<3>(dims);
						native = (void*)e;
				}
				break;
			default:
				break;
			}
		}


		Native_Tiled_Extent * Native_Extent::tile()
		{
			Native_Tiled_Extent * nte = new Native_Tiled_Extent(this);
			return nte;
		}

		Native_Tiled_Extent * Native_Extent::tile(int dim0)
		{
			extent<1> * e = (extent<1>*)native;
			Native_Tiled_Extent * nte = new Native_Tiled_Extent(dim0, this);
			return nte;
		}

		Native_Tiled_Extent * Native_Extent::tile(int dim0, int dim1)
		{
			extent<2> * e = (extent<2>*)native;
			Native_Tiled_Extent * nte = new Native_Tiled_Extent(dim0, dim1, this);
			return nte;
		}

		Native_Tiled_Extent * Native_Extent::tile(int dim0, int dim1, int dim2)
		{
			extent<3> * e = (extent<3>*)native;
			Native_Tiled_Extent * nte = new Native_Tiled_Extent(dim0, dim1, dim2, this);
			return nte;
		}
	}
}
