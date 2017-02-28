#pragma once

namespace Campy {
    namespace Types {

        public ref class Array_Base
        {
        public:
            Array_Base(){};

            // Native array view, provided for kernels.
            virtual void * native() {
                return nullptr;
            }
        };
    }
}
