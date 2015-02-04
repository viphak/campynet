
#include "Extent.h"
#include "Index.h"
#include "Array_View.h"
#include "Native_Array_View.h"
#include "Native_Array_View_Base.h"
#include "Native_Extent.h"
#include "CSCPP.h"

using namespace System;

namespace Campy {
	namespace Types {

		ref struct foo {
			float x;
			float y;
		};

		generic<typename _Value_type>
			Array_View<_Value_type>::Array_View(array<_Value_type> ^% data)
			{
				int length = data->Length;

				// Record basics about the array view.
				this->_data = data;
				this->_length = length;
				this->_extent = gcnew Extent(length);
				this->_element_type = _Value_type::typeid;

				// Set up the equivalent managed and unmanaged C++ type strings for the element type.
				this->_element_cppcli_type_string = CSCPP::ConvertToCPPCLI(_Value_type::typeid, 0);
				this->_element_cppnat_type_string = CSCPP::ConvertToCPP(_Value_type::typeid, 0);
				IntPtr ptrToNativeString = Marshal::StringToHGlobalAnsi(this->_element_cppnat_type_string);
				char* nat_cpp_unm = static_cast<char*>(ptrToNativeString.ToPointer());

				void * p = 0;

				// For non-value types, set up the array if it's not set with all non-null values.
				if (!this->_element_type->IsValueType)
				{
					// Init every element of array if it is an array of classes/structs (same thing, just
					// different access rules) Elements in an array view in C++ will
					// have to be value types; over here in C# world, it's an array of
					// pointers.
					for (int i = 0; i < length; ++i)
					{
						if (data[i] == nullptr)
						{
							data[i] = safe_cast<_Value_type>(System::Activator::CreateInstance(_Value_type::typeid));
						}
					}

					// C# array is not blittable.
					// Convert into array of structs and pin that.
					// Don't worry about the declaration for the unmanaged struct type:
					// layout handled here.
					this->_blittable_element_type = Campy::Types::Utils::Utility::CreateBlittableType(this->_element_type);
					this->_blittable_element_size = Marshal::SizeOf(this->_blittable_element_type);
					p = (void*)Campy::Types::Utils::Utility::CreateNativeArray(data, this->_blittable_element_type);

					// In order to create an array_view in C++ AMP, we have to have the representation
					// for the data type of the array. We have that, but it is a string that has to be
					// placed in a data type in an assembly, which we can actually call at this point.
					// To do that, we're going to have to call a builder for anything but the simpliest
					// array_view.
				}
				else
				{
					this->_blittable_element_size = Marshal::SizeOf(this->_element_type);

					// Pin the damn C# array.
					this->gchandle = GCHandle::Alloc(data, GCHandleType::Pinned);
					System::IntPtr ptr = gchandle.AddrOfPinnedObject();
					p = (void *)ptr.ToPointer();
					if (this->_element_type == System::Int32::typeid)
						this->_native = (void*) new Native_Array_View<int>(length, this->_blittable_element_size, p, nat_cpp_unm);
					else if (this->_element_type == System::Int64::typeid)
						this->_native = (void*) new Native_Array_View<long>(length, this->_blittable_element_size, p, nat_cpp_unm);
					else if (this->_element_type == System::UInt32::typeid)
						this->_native = (void*) new Native_Array_View<unsigned int>(length, this->_blittable_element_size, p, nat_cpp_unm);
					else if (this->_element_type == System::UInt64::typeid)
						this->_native = (void*) new Native_Array_View<unsigned long>(length, this->_blittable_element_size, p, nat_cpp_unm);
					else
						throw gcnew Exception("Unhandled type.");
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
				Type ^ t = _Value_type::typeid;
				Native_Array_View_Base * nav = (Native_Array_View_Base*)this->_native;

				_Value_type v;

				// Copy from native array view.
				void * ptr = nav->get(i);
				if (!this->_element_type->IsValueType)
				{
				}
				else
				{
					if (this->_element_type == System::Int32::typeid)
					{
						void * p = nav->get(i);
						v = (_Value_type)*(int*)p;
					}
					else if (this->_element_type == System::Int64::typeid)
					{
						void * p = nav->get(i);
						v = (_Value_type)*(long*)p;
					}
					else if (this->_element_type == System::UInt32::typeid)
					{
						void * p = nav->get(i);
						v = (_Value_type)*(unsigned int*)p;
					}
					else if (this->_element_type == System::UInt64::typeid)
					{
						void * p = nav->get(i);
						v = (_Value_type)*(unsigned long*)p;
					}
					else
						throw gcnew Exception("Unhandled type.");
				}
				_data[i] = v;
				return v;
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::default::set(int i, _Value_type value)
			{
				_data[i] = value;

				Native_Array_View_Base * nav = (Native_Array_View_Base*)this->_native;
				if (!this->_element_type->IsValueType)
				{
				}
				else
				{
					if (this->_element_type == System::Int32::typeid)
					{
						void * p = (void*)&value;
						nav->set(i, p);
					}
					else if (this->_element_type == System::Int64::typeid)
					{
						void * p = (void*)&value;
						nav->set(i, p);
					}
					else if (this->_element_type == System::UInt32::typeid)
					{
						void * p = (void*)&value;
						nav->set(i, p);
					}
					else if (this->_element_type == System::UInt64::typeid)
					{
						void * p = (void*)&value;
						nav->set(i, p);
					}
					else
						throw gcnew Exception("Unhandled type.");
				}
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::synchronize()
			{
				Type ^ t = _Value_type::typeid;
				Native_Array_View_Base * nav = (Native_Array_View_Base*)this->_native;
				nav->synchronize();
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