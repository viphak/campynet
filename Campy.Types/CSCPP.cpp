
#include <vcclr.h>          // PtrToStringChars
#include "CSCPP.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace System::Reflection;

namespace Campy {
	namespace Types {

		System::String^ CSCPP::ConvertToCPP(System::Type^ type, int level)
		{
			// Use reflection to create atring equivalent to type in C++ unmanaged world.
			if (type->FullName->Equals("System.Int32"))
			{
				return gcnew System::String("int");
			}
			else if (type->FullName->Equals("System.UInt32"))
			{
				return gcnew System::String("unsigned int");
			}
			else if (!type->IsValueType)
			{
				// Complex type.
				System::String^ result = gcnew System::String("");
				result += "struct ";
				return result;
			}
			else return nullptr;
		}

		System::String^ CSCPP::ConvertToCPPCLI(System::Type^ type, int level)
		{
			// Use reflection to create atring equivalent to type in C++ unmanaged world.
			if (type->FullName->Equals("System.Int32"))
			{
				return gcnew System::String("int");
			}
			else if (type->FullName->Equals("System.UInt32"))
			{
				return gcnew System::String("unsigned int");
			}
			else if (type->FullName->Equals("System.Single"))
			{
				return gcnew System::String("float");
			}
			else if (!type->IsValueType)
			{
				// Complex type.
				String^ result = gcnew System::String("");
				String^ ind = "";
				for (int i = 0; i < level; ++i)
					ind += "    ";
				result += ind + "ref struct " + type->Name + "\n";
				result += ind + "{\n";
				System::Reflection::BindingFlags flags = BindingFlags::Public | BindingFlags::NonPublic |
					BindingFlags::Static | BindingFlags::Instance |
					BindingFlags::DeclaredOnly;
				array<FieldInfo^>^ fields = type->GetFields(flags);
				for (int i = 0; i < fields->Length; ++i)
				{
					FieldInfo^ fi = fields[i];
					Type^ tf = fi->FieldType;
					result += ConvertToCPPCLI(tf, level + 1) + " " + fi->Name + ";\n";
				}
				result += ind + "} " + type->Name + ";\n";
				return result;
			}
			else return nullptr;
		}
	}
}
