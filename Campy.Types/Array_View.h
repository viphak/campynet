// Array_View.h

#pragma once
#include "Array_View_Base.h"
#include "Basic_Types.h"
#include "Extent.h"
#include "Index.h"
#include "Native_Array_View.h"

using namespace System;
using namespace System::Runtime::InteropServices;


namespace Campy {
    namespace Types {

        generic<typename _Value_type>
            public ref class Array_View : Array_View_Base
            {

            private:
                String^ _element_cppcli_type_string;
                String^ _element_cppnat_type_string;
                Type^ _blittable_element_type; // type in C++ world.
                int _blittable_element_size; // bytes.
                Type^ _element_type; // type in C# world.
                int _Rank = 1;
                array<_Value_type> ^ _data;
                int _length;
                Extent ^ _extent;
                void * _native;
                GCHandle gchandle;
				IntPtr _native_data_buffer;
                static array<_Value_type>^ default_data = gcnew array<_Value_type>(1);
                static Array_View^ default_value = gcnew Array_View(default_data);

            public:
                Array_View(array<_Value_type> ^% data);

                property Extent^ extent
                {
                    Extent^ get();
                    void set(Extent^ extent);
                }

                property _Value_type default[int]
                {
                    _Value_type get(int i);
                    void set(int i, _Value_type value);
                }

                void synchronize();
                array<_Value_type>^ data();
                static property Array_View^ Default_Value
                {
                    Array_View^ get();
                }

            public:
                // Native array view, provided for kernels.
				virtual void * native() override;
            };
    }
}