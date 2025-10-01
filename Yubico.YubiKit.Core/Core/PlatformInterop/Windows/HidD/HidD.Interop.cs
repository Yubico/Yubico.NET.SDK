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

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.Core.PlatformInterop.Windows.HidD;

internal static partial class NativeMethods
{
    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    internal struct HIDD_ATTRIBUTES
    {
        public int Size;
        public short VendorId;
        public short ProductId;
        public short VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct HIDP_CAPS
    {
        public fixed short Reserved[17];
        public short Usage;
        public short UsagePage;
        public short InputReportByteLength;
        public short OutputReportByteLength;
        public short FeatureReportByteLength;
        public short NumberLinkCollectionNodes;

        public short NumberInputButtonCaps;
        public short NumberInputValueCaps;
        public short NumberInputDataIndices;

        public short NumberOutputButtonCaps;
        public short NumberOutputValueCaps;
        public short NumberOutputDataIndices;

        public short NumberFeatureButtonCaps;
        public short NumberFeatureValueCaps;
        public short NumberFeatureDataIndices;
    }

    #endregion

    #region P/Invoke DLL Imports

    [LibraryImport(Libraries.Hid, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HidD_FreePreparsedData(
        IntPtr PreparsedData
    );

    [LibraryImport(Libraries.Hid, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HidD_GetAttributes(
        SafeFileHandle HidDeviceObject,
        out HIDD_ATTRIBUTES Attributes
    );

    [LibraryImport(Libraries.Hid, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HidD_GetFeature(
        SafeFileHandle HidDeviceObject,
        byte[] ReportBuffer,
        int ReportBufferLength
    );

    [LibraryImport(Libraries.Hid, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HidD_GetManufacturerString(
        SafeFileHandle HidDeviceObject,
        byte[] Buffer,
        int BufferLength
    );

    [LibraryImport(Libraries.Hid, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HidD_GetPreparsedData(
        SafeFileHandle HidDeviceObject,
        out IntPtr PreparsedData
    );

    [LibraryImport(Libraries.Hid, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HidD_GetProductString(
        SafeFileHandle HidDeviceObject,
        byte[] Buffer,
        int BufferLength
    );

    [LibraryImport(Libraries.Hid, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HidD_SetFeature(
        SafeFileHandle HidDeviceObject,
        byte[] Buffer,
        int BufferLength
    );

    [LibraryImport(Libraries.Hid, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HidP_GetCaps(
        IntPtr PreparsedData,
        ref HIDP_CAPS Capabilities
    );

    #endregion
}