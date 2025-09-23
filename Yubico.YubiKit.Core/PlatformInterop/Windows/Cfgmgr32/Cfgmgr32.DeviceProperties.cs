// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.PlatformInterop.Windows.Cfgmgr32;

internal static partial class NativeMethods
{
    //
    // Device properties
    //
    // These DEVPKEYs corespond to the SetupAPI SPDRP_XXX device properties
    //
    internal static readonly DEVPROPKEY DEVPKEY_Device_DeviceDesc =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 2); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_HardwareIds =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0,
            3); // DEVPROP_TYPE.STRING_LIST

    internal static readonly DEVPROPKEY DEVPKEY_Device_CompatibleIds =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0,
            4); // DEVPROP_TYPE.STRING_LIST

    internal static readonly DEVPROPKEY DEVPKEY_Device_Service =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 6); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_Class =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 9); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_ClassGuid =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 10); // DEVPROP_TYPE.GUID

    internal static readonly DEVPROPKEY DEVPKEY_Device_Driver =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 11); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_Manufacturer =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 13); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_FriendlyName =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 14); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_LocationInfo =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 15); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_PDOName =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 16); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_LocationPaths =
        new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0,
            37); // DEVPROP_TYPE.STRING_LIST

    internal static readonly DEVPROPKEY DEVPKEY_Device_InstanceId =
        new(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57, 256); // DEVPROP_TYPE.STRING

    //
    // Device properties
    //
    internal static readonly DEVPROPKEY DEVPKEY_Device_DevNodeStatus =
        new(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 2); // DEVPROP_TYPE.UINT32

    internal static readonly DEVPROPKEY DEVPKEY_Device_ProblemCode =
        new(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 3); // DEVPROP_TYPE.UINT32

    internal static readonly DEVPROPKEY DEVPKEY_Device_Parent =
        new(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7, 8); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_Children =
        new(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7,
            9); // DEVPROP_TYPE.STRING_LIST

    internal static readonly DEVPROPKEY DEVPKEY_Device_Siblings =
        new(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7,
            10); // DEVPROP_TYPE.STRING_LIST

    internal static readonly DEVPROPKEY DEVPKEY_Device_Model =
        new(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57, 39); // DEVPROP_TYPE.STRING

    internal static readonly DEVPROPKEY DEVPKEY_Device_ContainerId =
        new(0x8c7ed206, 0x3f8a, 0x4827, 0xb3, 0xab, 0xae, 0x9e, 0x1f, 0xae, 0xfc, 0x6c, 2); // DEVPROP_TYPE_GUID

    //
    // HID specific
    //
    internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_UsagePage =
        new(0xcbf38310, 0x4a17, 0x4310, 0xa1, 0xeb, 0x24, 0x7f, 0xb, 0x67, 0x59, 0x3b, 2); // DEVPROP_TYPE.UINT16

    internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_UsageId =
        new(0xcbf38310, 0x4a17, 0x4310, 0xa1, 0xeb, 0x24, 0x7f, 0xb, 0x67, 0x59, 0x3b, 3); // DEVPROP_TYPE.UINT16

    internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_IsReadOnly =
        new(0xcbf38310, 0x4a17, 0x4310, 0xa1, 0xeb, 0x24, 0x7f, 0xb, 0x67, 0x59, 0x3b, 4); // DEVPROP_TYPE.BOOLEAN

    internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_VendorId =
        new(0xcbf38310, 0x4a17, 0x4310, 0xa1, 0xeb, 0x24, 0x7f, 0xb, 0x67, 0x59, 0x3b, 5); // DEVPROP_TYPE.UINT16

    internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_ProductId =
        new(0xcbf38310, 0x4a17, 0x4310, 0xa1, 0xeb, 0x24, 0x7f, 0xb, 0x67, 0x59, 0x3b, 6); // DEVPROP_TYPE.UINT16

    internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_VersionNumber =
        new(0xcbf38310, 0x4a17, 0x4310, 0xa1, 0xeb, 0x24, 0x7f, 0xb, 0x67, 0x59, 0x3b, 7); // DEVPROP_TYPE.UINT16

    internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_BackgroundAccess =
        new(0xcbf38310, 0x4a17, 0x4310, 0xa1, 0xeb, 0x24, 0x7f, 0xb, 0x67, 0x59, 0x3b, 8); // DEVPROP_TYPE.BOOLEAN

    #region DEVPROPKEY and DEVPROP_TYPE definitions

    [StructLayout(LayoutKind.Sequential)]
    internal struct DEVPROPKEY
    {
        public Guid guid;
        public int pid;

        public DEVPROPKEY(Guid guid, int pid)
        {
            this.guid = guid;
            this.pid = pid;
        }

        public DEVPROPKEY(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j,
            byte k, int pid)
        {
            guid = new Guid(a, b, c, d, e, f, g, h, i, j, k);
            this.pid = pid;
        }
    }

    private const int DEVPROP_TYPEMOD_ARRAY = 0x00001000; // Array of fixed sized data elements
    private const int DEVPROP_TYPEMOD_LIST = 0x00002000; // List of variable-sized data elements

    internal enum DEVPROP_TYPE
    {
        EMPTY = 0x0000_0000, // Nothing, no property data
        NULL = 0x0000_0001, // Null property data
        SBYTE = 0x0000_0002, // 8-bit signed int (SBYTE)
        BYTE = 0x0000_0003, // 8-bit unsigned int (BYTE)
        INT16 = 0x0000_0004, // 16-bit signed int (SHORT)
        UINT16 = 0x0000_0005, // 16-bit unsigned int (USHORT)
        INT32 = 0x0000_0006, // 32-bit signed int (LONG)
        UINT32 = 0x0000_0007, // 32-bit unsigned int (ULONG)
        INT64 = 0x0000_0008, // 64-bit signed int (LONG64)
        UINT64 = 0x0000_0009, // 64-bit unsigned int (ULONG64)
        FLOAT = 0x0000_000A, // 32-bit floating point (FLOAT)
        DOUBLE = 0x0000_000B, // 64-bit floating point (DOUBLE)
        DECIMAL = 0x0000_000C, // 128-bit floating point (DECIMAL)
        GUID = 0x0000_000D, // 128-bit unique identifier (GUID)
        CURRENCY = 0x0000_000E, // 64 bit signed int currency value (CURRENCY)
        DATE = 0x0000_000F, // Date (DATE)
        FILETIME = 0x0000_0010, // File time (FILETIME)
        BOOLEAN = 0x0000_0011, // 8-bit boolean (DEVPROP_BOOLEAN)
        STRING = 0x0000_0012, // Null-terminated-string
        STRING_LIST = STRING | DEVPROP_TYPEMOD_LIST, // Multi-sz-string list
        SECURITY_DESCRIPTOR = 0x0000_0013, // Self-relative binary SECURITY_DESCRIPTOR
        SECURITY_DESCRIPTOR_STRING = 0x0000_0014, // Security descriptor string (SDDL format)
        DEVPROPKEY = 0x0000_0015, // Device property key (DEVPROPKEY)
        DEVPROPTYPE = 0x0000_0016, // Device property type (DEVPROPTYPE)
        BINARY = BYTE | DEVPROP_TYPEMOD_ARRAY, // Custom binary data
        ERROR = 0x0000_0017, // 32-bit Win32 system error code
        NTSTATUS = 0x0000_0018, // 32-bit NTSTATUS code
        STRING_INDIRECT = 0x0000_0019 // String resource (@[path\]<dllname>,-<strId>)
    }

    #endregion
}