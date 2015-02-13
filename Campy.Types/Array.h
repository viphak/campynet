// Array.h

#pragma once
#include "Array_Base.h"
#include "Array_View.h"
#include "Array_View_Base.h"
#include "Accelerator_View.h"
#include "Basic_Types.h"
#include "Extent.h"
#include "Index.h"
#include "Native_Array.h"
#include "Native_Array_View.h"

using namespace System;
using namespace System::Runtime::InteropServices;


namespace Campy {
	namespace Types {

		generic<typename _Value_type>
			public ref class Array : Array_Base
			{

			private:
				String^ _element_cppcli_type_string;
				String^ _element_cppnat_type_string;
				Type^ _blittable_element_type; // type in C++ world.
				int _blittable_element_size; // bytes.
				Type^ _element_type; // type in C# world.
				int _Rank = 1;
				array<_Value_type> ^ _data;
				Extent ^ _extent;
				void * _native;
				GCHandle gchandle;
				//IntPtr _native_data_buffer;
				//bool dirty_managed_side;
				//void do_late_binding();
				Accelerator_View^ _accelerator_view;
				Accelerator_View^ _associated_accelerator_view;
				static Array^ default_value = gcnew Array(1);


			public:
				Array(int _E0);
				Array(int _E0, int _E1);
				//Array(int _E0, int _E1, int _E2);
				Array(Extent^ extent, Accelerator_View^ ac, Access_Type at);
				Array(int _E0, Accelerator_View^ ac, Access_Type at);
				//Array(int _E0, int _E1, Accelerator_View ac, Access_Type at);
				//Array(int _E0, int _E1, int _E2, Accelerator_View ac, Access_Type at);
				//Array(Extent extent, Accelerator_View^ ac, Accelerator_View ac2);
				//Array(int _E0, Accelerator_View^ ac, Accelerator_View ac2);
				//Array(int _E0, int _E1, Accelerator_View ac, Accelerator_View ac2);
				//Array(int _E0, int _E1, int _E2, Accelerator_View ac, Accelerator_View ac2);
				Array(array<_Value_type> ^% data);
				Array(IntPtr data, int length, Native_Array_View_Base * nav);

				property Extent^ Extent
				{
					Campy::Types::Extent^ get();
					void set(Campy::Types::Extent^ extent);
				}

				property _Value_type default[int]
				{
					_Value_type get(int i);
					void set(int i, _Value_type value);
				}

				array<_Value_type>^ Data();
				void Reinterpret_As();
				Array_View<_Value_type>^ Section(int  _I0, int _E0);
				Accelerator_View^ Get_Accelerator_View();
				Accelerator_View^ Get_Associated_Accelerator_View();
				static property Array^ Default_Value
				{
					Array^ get();
				}

			public:
				// Native array, provided for kernels.
				virtual void * native() override;
			};
	}
}