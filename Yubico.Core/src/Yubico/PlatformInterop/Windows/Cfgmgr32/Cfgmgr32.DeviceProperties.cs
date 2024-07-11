// Copyright 2021 Yubico AB
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

using System;
using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop
{
    internal static partial class NativeMethods
    {
        //
        // Device properties
        //
        // These DEVPKEYs corespond to the SetupAPI SPDRP_XXX device properties
        //
        internal static readonly DEVPROPKEY DEVPKEY_Device_DeviceDesc = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 2); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_HardwareIds = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 3); // DEVPROP_TYPE.STRING_LIST

        internal static readonly DEVPROPKEY DEVPKEY_Device_CompatibleIds = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 4); // DEVPROP_TYPE.STRING_LIST

        internal static readonly DEVPROPKEY DEVPKEY_Device_Service = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 6); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_Class = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 9); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_ClassGuid = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 10); // DEVPROP_TYPE.GUID

        internal static readonly DEVPROPKEY DEVPKEY_Device_Driver = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 11); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_Manufacturer = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 13); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_FriendlyName = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 14); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_LocationInfo = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 15); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_PDOName = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 16); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_LocationPaths = new DEVPROPKEY(
            a: 0xa45c254e, b: 0xdf1c, c: 0x4efd, d: 0x80, e: 0x20, f: 0x67, g: 0xd1, h: 0x46, i: 0xa8, j: 0x50, k: 0xe0,
            pid: 37); // DEVPROP_TYPE.STRING_LIST

        internal static readonly DEVPROPKEY DEVPKEY_Device_InstanceId = new DEVPROPKEY(
            a: 0x78c34fc8, b: 0x104a, c: 0x4aca, d: 0x9e, e: 0xa4, f: 0x52, g: 0x4d, h: 0x52, i: 0x99, j: 0x6e, k: 0x57,
            pid: 256); // DEVPROP_TYPE.STRING

        //
        // Device properties
        //
        internal static readonly DEVPROPKEY DEVPKEY_Device_DevNodeStatus = new DEVPROPKEY(
            a: 0x4340a6c5, b: 0x93fa, c: 0x4706, d: 0x97, e: 0x2c, f: 0x7b, g: 0x64, h: 0x80, i: 0x08, j: 0xa5, k: 0xa7,
            pid: 2); // DEVPROP_TYPE.UINT32

        internal static readonly DEVPROPKEY DEVPKEY_Device_ProblemCode = new DEVPROPKEY(
            a: 0x4340a6c5, b: 0x93fa, c: 0x4706, d: 0x97, e: 0x2c, f: 0x7b, g: 0x64, h: 0x80, i: 0x08, j: 0xa5, k: 0xa7,
            pid: 3); // DEVPROP_TYPE.UINT32

        internal static readonly DEVPROPKEY DEVPKEY_Device_Parent = new DEVPROPKEY(
            a: 0x4340a6c5, b: 0x93fa, c: 0x4706, d: 0x97, e: 0x2c, f: 0x7b, g: 0x64, h: 0x80, i: 0x08, j: 0xa5, k: 0xa7,
            pid: 8); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_Children = new DEVPROPKEY(
            a: 0x4340a6c5, b: 0x93fa, c: 0x4706, d: 0x97, e: 0x2c, f: 0x7b, g: 0x64, h: 0x80, i: 0x08, j: 0xa5, k: 0xa7,
            pid: 9); // DEVPROP_TYPE.STRING_LIST

        internal static readonly DEVPROPKEY DEVPKEY_Device_Siblings = new DEVPROPKEY(
            a: 0x4340a6c5, b: 0x93fa, c: 0x4706, d: 0x97, e: 0x2c, f: 0x7b, g: 0x64, h: 0x80, i: 0x08, j: 0xa5, k: 0xa7,
            pid: 10); // DEVPROP_TYPE.STRING_LIST

        internal static readonly DEVPROPKEY DEVPKEY_Device_Model = new DEVPROPKEY(
            a: 0x78c34fc8, b: 0x104a, c: 0x4aca, d: 0x9e, e: 0xa4, f: 0x52, g: 0x4d, h: 0x52, i: 0x99, j: 0x6e, k: 0x57,
            pid: 39); // DEVPROP_TYPE.STRING

        internal static readonly DEVPROPKEY DEVPKEY_Device_ContainerId = new DEVPROPKEY(
            a: 0x8c7ed206, b: 0x3f8a, c: 0x4827, d: 0xb3, e: 0xab, f: 0xae, g: 0x9e, h: 0x1f, i: 0xae, j: 0xfc, k: 0x6c,
            pid: 2); // DEVPROP_TYPE_GUID

        //
        // HID specific
        //
        internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_UsagePage = new DEVPROPKEY(
            a: 0xcbf38310, b: 0x4a17, c: 0x4310, d: 0xa1, e: 0xeb, f: 0x24, g: 0x7f, h: 0xb, i: 0x67, j: 0x59, k: 0x3b,
            pid: 2); // DEVPROP_TYPE.UINT16

        internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_UsageId = new DEVPROPKEY(
            a: 0xcbf38310, b: 0x4a17, c: 0x4310, d: 0xa1, e: 0xeb, f: 0x24, g: 0x7f, h: 0xb, i: 0x67, j: 0x59, k: 0x3b,
            pid: 3); // DEVPROP_TYPE.UINT16

        internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_IsReadOnly = new DEVPROPKEY(
            a: 0xcbf38310, b: 0x4a17, c: 0x4310, d: 0xa1, e: 0xeb, f: 0x24, g: 0x7f, h: 0xb, i: 0x67, j: 0x59, k: 0x3b,
            pid: 4); // DEVPROP_TYPE.BOOLEAN

        internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_VendorId = new DEVPROPKEY(
            a: 0xcbf38310, b: 0x4a17, c: 0x4310, d: 0xa1, e: 0xeb, f: 0x24, g: 0x7f, h: 0xb, i: 0x67, j: 0x59, k: 0x3b,
            pid: 5); // DEVPROP_TYPE.UINT16

        internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_ProductId = new DEVPROPKEY(
            a: 0xcbf38310, b: 0x4a17, c: 0x4310, d: 0xa1, e: 0xeb, f: 0x24, g: 0x7f, h: 0xb, i: 0x67, j: 0x59, k: 0x3b,
            pid: 6); // DEVPROP_TYPE.UINT16

        internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_VersionNumber = new DEVPROPKEY(
            a: 0xcbf38310, b: 0x4a17, c: 0x4310, d: 0xa1, e: 0xeb, f: 0x24, g: 0x7f, h: 0xb, i: 0x67, j: 0x59, k: 0x3b,
            pid: 7); // DEVPROP_TYPE.UINT16

        internal static readonly DEVPROPKEY DEVPKEY_DeviceInterface_HID_BackgroundAccess = new DEVPROPKEY(
            a: 0xcbf38310, b: 0x4a17, c: 0x4310, d: 0xa1, e: 0xeb, f: 0x24, g: 0x7f, h: 0xb, i: 0x67, j: 0x59, k: 0x3b,
            pid: 8); // DEVPROP_TYPE.BOOLEAN

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

            public DEVPROPKEY(
                uint a,
                ushort b,
                ushort c,
                byte d,
                byte e,
                byte f,
                byte g,
                byte h,
                byte i,
                byte j,
                byte k,
                int pid)
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
}
