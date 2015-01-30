
#include "Extent.h"
#include "Index.h"
#include "Array_View.h"
#include "Native_Array_View.h"
#include "Native_Extent.h"

using namespace System;

namespace Campy {
	namespace Types {

		generic<typename _Value_type>
			Array_View<_Value_type>::Array_View(int length, array<_Value_type> ^% data)
			{
				this->_data = data;
				this->_length = length;
				this->_extent = gcnew Extent(length);
				// Convert generic parameter into template argument in order to create native array view.
				Type ^ t = _Value_type::typeid;
				if (t->FullName->Equals("System.Int32"))
				{
					pin_ptr<_Value_type> ptr = &data[0];
					int * p = (int *)ptr;
					this->_native = (void*) new Native_Array_View<int, 1>(length, p);
				}
			}

		generic<typename _Value_type>
			Extent^ Array_View<_Value_type>::extent::get()
			{
				return _extent;
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::extent::set(Extent^ extent)
			{
				_extent = extent;
			}

		generic<typename _Value_type>
			_Value_type Array_View<_Value_type>::default::get(int i)
			{
				return _data[i];
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::default::set(int i, _Value_type value)
			{
				_data[i] = value;
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::synchronize()
			{
				Type ^ t = _Value_type::typeid;
				if (t->FullName->Equals("System.Int32"))
				{
					Native_Array_View<int, 1> * nav = (Native_Array_View<int, 1>*)this->_native;
					nav->synchronize();
				}
			}

		generic<typename _Value_type>
			array<_Value_type>^ Array_View<_Value_type>::data()
			{
				return this->_data;
			}

		generic<typename _Value_type>
			void* Array_View<_Value_type>::native()
			{
				return this->_native;
			}

		generic<typename _Value_type>
			Array_View<_Value_type>^ Array_View<_Value_type>::Default_Value::get()
			{
				return Array_View<_Value_type>::default_value;
			}
	}
}