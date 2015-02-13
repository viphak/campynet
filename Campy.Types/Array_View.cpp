
#include "Extent.h"
#include "Index.h"
#include "Array_View.h"
#include "Native_Array_View.h"
#include "Native_Array_View_Base.h"
#include "Native_Extent.h"

using namespace System;
using namespace Campy::Types::Utils;


namespace Campy {
	namespace Types {

		generic<typename _Value_type>
			void Array_View<_Value_type>::do_late_binding()
			{
				if (this->_native_data_buffer == IntPtr(nullptr))
				{
					// Implicitly reading somehow.
					// Set up data buffer for reading and writing.
					this->_native_data_buffer =
						Campy::Types::Utils::Utility::CreateNativeArray(this->_extent->Size(),
						this->_blittable_element_size);
					this->dirty_managed_side = false;
				}
				if (this->_data == nullptr)
				{
					// Implicitly reading somehow.
					// Set up data buffer for reading and writing.
					this->_data = gcnew array<_Value_type>(this->_extent->Size());
					this->dirty_managed_side = false;
				}
				if (!this->dirty_managed_side)
					return;
				this->dirty_managed_side = false;
				if (!this->_element_type->IsValueType)
				{
					// Copy to native.
					Campy::Types::Utils::Utility::CopyToNativeArray(
						_data,
						this->_native_data_buffer,
						this->_blittable_element_type);
				}
			}

			generic<typename _Value_type>
				void Array_View<_Value_type>::InitializeCommon(array<_Value_type> ^% data)
				{
					int length = data->Length;
					this->_data = data;
					this->_extent = gcnew Campy::Types::Extent(length);
					this->_element_type = _Value_type::typeid;
					this->dirty_managed_side = true;
					this->_element_cppcli_type_string = CSCPP::ConvertToCPPCLI(_Value_type::typeid, 0);
					this->_element_cppnat_type_string = CSCPP::ConvertToCPP(_Value_type::typeid, 0);
					IntPtr ptrToNativeString = Marshal::StringToHGlobalAnsi(this->_element_cppnat_type_string);
					char* nat_cpp_unm = static_cast<char*>(ptrToNativeString.ToPointer());
					this->_native_data_buffer = IntPtr(nullptr);
					this->_blittable_element_type = nullptr;
					this->_blittable_element_size = 0;
					this->dirty_managed_side = true;

					void * p = 0;

					// For non-value types, set up the array if it's not set with all non-null values.
					if (!this->_element_type->IsValueType)
					{
						// C# array of class is not blittable.
						// Convert into array of structs and pin that.
						this->_blittable_element_type = Campy::Types::Utils::Utility::CreateBlittableType(this->_element_type, true);
						this->_blittable_element_size = Marshal::SizeOf(this->_blittable_element_type);
						this->_native_data_buffer = Campy::Types::Utils::Utility::CreateNativeArray(data, this->_blittable_element_type);
						Campy::Types::Utils::NativeArrayViewGenerator^ gen = gcnew Campy::Types::Utils::NativeArrayViewGenerator();
						this->_native = gen->Generate(
							this->_blittable_element_type,
							length,
							this->_blittable_element_size,
							this->_native_data_buffer,
							ptrToNativeString
							).ToPointer();
					}
					else
					{
						this->_blittable_element_type = this->_element_type;
						this->_blittable_element_size = Marshal::SizeOf(this->_element_type);
						this->gchandle = GCHandle::Alloc(data, GCHandleType::Pinned);
						System::IntPtr ptr = gchandle.AddrOfPinnedObject();
						this->_native_data_buffer = ptr;
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
				Array_View<_Value_type>::Array_View(array<_Value_type> ^% data)
				{
					int length = data->Length;
					this->_data = data;
					//this->_length = length;
					this->_extent = gcnew Campy::Types::Extent(length);
					this->_element_type = _Value_type::typeid;
					this->dirty_managed_side = true;
					this->_element_cppcli_type_string = CSCPP::ConvertToCPPCLI(_Value_type::typeid, 0);
					this->_element_cppnat_type_string = CSCPP::ConvertToCPP(_Value_type::typeid, 0);
					IntPtr ptrToNativeString = Marshal::StringToHGlobalAnsi(this->_element_cppnat_type_string);
					char* nat_cpp_unm = static_cast<char*>(ptrToNativeString.ToPointer());
					this->_native_data_buffer = IntPtr(nullptr);
					this->_blittable_element_type = nullptr;
					this->_blittable_element_size = 0;
					this->dirty_managed_side = true;

					void * p = 0;

					// For non-value types, set up the array if it's not set with all non-null values.
					if (!this->_element_type->IsValueType)
					{
						// C# array of class is not blittable.
						// Convert into array of structs and pin that.
						this->_blittable_element_type = Campy::Types::Utils::Utility::CreateBlittableType(this->_element_type, true);
						this->_blittable_element_size = Marshal::SizeOf(this->_blittable_element_type);
						this->_native_data_buffer = Campy::Types::Utils::Utility::CreateNativeArray(data, this->_blittable_element_type);
						Campy::Types::Utils::NativeArrayViewGenerator^ gen = gcnew Campy::Types::Utils::NativeArrayViewGenerator();
						this->_native = gen->Generate(
							this->_blittable_element_type,
							length,
							this->_blittable_element_size,
							this->_native_data_buffer,
							ptrToNativeString
							).ToPointer();
					}
					else
					{
						this->_blittable_element_type = this->_element_type;
						this->_blittable_element_size = Marshal::SizeOf(this->_element_type);
						this->gchandle = GCHandle::Alloc(data, GCHandleType::Pinned);
						System::IntPtr ptr = gchandle.AddrOfPinnedObject();
						this->_native_data_buffer = ptr;
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
				Array_View<_Value_type>::Array_View(IntPtr data, int length, Native_Array_View_Base * nav)
				{
					this->_data = gcnew array<_Value_type>(length);
					//this->_length = length;
					this->_extent = gcnew Campy::Types::Extent(length);
					this->_element_type = _Value_type::typeid;
					this->dirty_managed_side = false;
					this->_element_cppcli_type_string = CSCPP::ConvertToCPPCLI(_Value_type::typeid, 0);
					this->_element_cppnat_type_string = CSCPP::ConvertToCPP(_Value_type::typeid, 0);
					IntPtr ptrToNativeString = Marshal::StringToHGlobalAnsi(this->_element_cppnat_type_string);
					char* nat_cpp_unm = static_cast<char*>(ptrToNativeString.ToPointer());
					this->_native_data_buffer = data;
					this->_blittable_element_type = nullptr;
					this->_blittable_element_size = 0;
					this->dirty_managed_side = true;
					this->_native = (void*)nav;
					// For non-value types, set up the array if it's not set with all non-null values.
					if (!this->_element_type->IsValueType)
					{
						// C# array of class is not blittable.
						// Convert into array of structs and pin that.
						this->_blittable_element_type = Campy::Types::Utils::Utility::CreateBlittableType(this->_element_type, true);
						this->_blittable_element_size = Marshal::SizeOf(this->_blittable_element_type);
					}
					else
					{
						this->_blittable_element_type = this->_element_type;
						this->_blittable_element_size = Marshal::SizeOf(this->_element_type);
					}
				}

			generic<typename _Value_type>
				Array_View<_Value_type>::Array_View(int length, Native_Array_View_Base * nav)
				{
					this->_data = nullptr;
					this->_native_data_buffer = IntPtr(nullptr);
					//this->_length = length;
					this->_extent = gcnew Campy::Types::Extent(length);
					this->_element_type = _Value_type::typeid;
					this->dirty_managed_side = false;
					this->_element_cppcli_type_string = CSCPP::ConvertToCPPCLI(_Value_type::typeid, 0);
					this->_element_cppnat_type_string = CSCPP::ConvertToCPP(_Value_type::typeid, 0);
					IntPtr ptrToNativeString = Marshal::StringToHGlobalAnsi(this->_element_cppnat_type_string);
					char* nat_cpp_unm = static_cast<char*>(ptrToNativeString.ToPointer());
					this->_blittable_element_type = nullptr;
					this->_blittable_element_size = 0;
					this->dirty_managed_side = true;
					this->_native = (void*)nav;
					// For non-value types, set up the array if it's not set with all non-null values.
					if (!this->_element_type->IsValueType)
					{
						// C# array of class is not blittable.
						// Convert into array of structs and pin that.
						this->_blittable_element_type = Campy::Types::Utils::Utility::CreateBlittableType(this->_element_type, true);
						this->_blittable_element_size = Marshal::SizeOf(this->_blittable_element_type);
					}
					else
					{
						this->_blittable_element_type = this->_element_type;
						this->_blittable_element_size = Marshal::SizeOf(this->_element_type);
					}
				}

						generic<typename _Value_type>
			void Array_View<_Value_type>::Discard_Data()
			{
				Native_Array_View_Base * nav = (Native_Array_View_Base*)this->_native;
				nav->Discard_Data();
				this->dirty_managed_side = false;
			}

		generic<typename _Value_type>
			Extent^ Array_View<_Value_type>::Extent::get()
			{
				return _extent;
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::Extent::set(Campy::Types::Extent^ extent)
			{
				_extent = extent;
			}

		generic<typename _Value_type>
			_Value_type Array_View<_Value_type>::default::get(int i)
			{
				Native_Array_View_Base * nav = (Native_Array_View_Base*)this->_native;
				do_late_binding();
				// Copy from native array view.
				if (!this->_element_type->IsValueType)
				{
					void * p = nav->Get(i);
					//Object^ bo = System::Activator::CreateInstance(this->_blittable_element_type);
					Object^ bo = Marshal::PtrToStructure(IntPtr(p), this->_blittable_element_type);
					Campy::Types::Utils::Utility::CopyFromBlittableType(bo, (Object^)_data[i]);
					return _data[i];
				}
				else
				{
					_Value_type v;
					if (this->_element_type == System::Int32::typeid)
					{
						void * p = nav->Get(i);
						v = (_Value_type)*(int*)p;
					}
					else if (this->_element_type == System::Int64::typeid)
					{
						void * p = nav->Get(i);
						v = (_Value_type)*(long*)p;
					}
					else if (this->_element_type == System::UInt32::typeid)
					{
						void * p = nav->Get(i);
						v = (_Value_type)*(unsigned int*)p;
					}
					else if (this->_element_type == System::UInt64::typeid)
					{
						void * p = nav->Get(i);
						v = (_Value_type)*(unsigned long*)p;
					}
					else
						throw gcnew Exception("Unhandled type.");
					_data[i] = v;
					return v;
				}
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::default::set(int i, _Value_type value)
			{
				do_late_binding();
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
						nav->Set(i, p);
					}
					else if (this->_element_type == System::Int64::typeid)
					{
						void * p = (void*)&value;
						nav->Set(i, p);
					}
					else if (this->_element_type == System::UInt32::typeid)
					{
						void * p = (void*)&value;
						nav->Set(i, p);
					}
					else if (this->_element_type == System::UInt64::typeid)
					{
						void * p = (void*)&value;
						nav->Set(i, p);
					}
					else
						throw gcnew Exception("Unhandled type.");
				}
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::Synchronize()
			{
				Type ^ t = _Value_type::typeid;
				Native_Array_View_Base * nav = (Native_Array_View_Base*)this->_native;
				nav->Synchronize();
				if (!this->_element_type->IsValueType)
				{
					// Copy from native.
					Campy::Types::Utils::Utility::CopyFromNativeArray(
						this->_native_data_buffer,
						_data,
						this->_blittable_element_type);
				}
				this->dirty_managed_side = false;
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::Synchronize_Async()
			{
				Type ^ t = _Value_type::typeid;
				Native_Array_View_Base * nav = (Native_Array_View_Base*)this->_native;
				nav->Synchronize();
			}

		generic<typename _Value_type>
			array<_Value_type>^ Array_View<_Value_type>::Data()
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

		generic<typename _Value_type>
			void Array_View<_Value_type>::Reinterpret_As()
			{}

		generic<typename _Value_type>
			Array_View<_Value_type>^ Array_View<_Value_type>::Section(int  _I0, int _E0)
			{
				// Copy _E0 * blittable_element_size bytes from native.
				Native_Array_View_Base * nav = (Native_Array_View_Base *)this->_native;
				Native_Array_View_Base * new_nav = nav->Section(_I0, _E0);
				IntPtr mem = this->_native_data_buffer + this->_blittable_element_size * _I0;
				Array_View<_Value_type>^ result = gcnew Array_View<_Value_type>(mem, _E0, new_nav);
				return result;
			}

		generic<typename _Value_type>
			void Array_View<_Value_type>::Refresh()
			{}
	}
}