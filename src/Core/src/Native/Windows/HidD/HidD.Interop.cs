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

namespace Yubico.YubiKit.Core.Native.Windows.HidD;

internal static partial class NativeMethods
{
    // HidD returns a one-byte BOOLEAN, unlike Kernel32's four-byte BOOL.
    // Synchronous feature calls borrow the pinned array; the caller retains the handle explicitly.
    [LibraryImport(Libraries.Hid, EntryPoint = "HidD_GetFeature", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial byte GetHidFeature(nint handle, byte[] report, uint reportLength);

    [LibraryImport(Libraries.Hid, EntryPoint = "HidD_SetFeature", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial byte SetHidFeature(nint handle, byte[] report, uint reportLength);

    [LibraryImport(Libraries.Hid, EntryPoint = "HidD_GetPreparsedData", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial byte GetHidPreparsedData(nint handle, out nint preparsedData);

    [LibraryImport(Libraries.Hid, EntryPoint = "HidD_FreePreparsedData", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial byte FreeHidPreparsedData(nint preparsedData);

    // Native HIDP_CAPS fields are unsigned 16-bit values. The report APIs reinterpret these
    // short fields as ushort; other consumers need separate review for values above 32767.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct HIDP_CAPS
    {
        public short Usage;
        public short UsagePage;
        public short InputReportByteLength;
        public short OutputReportByteLength;
        public short FeatureReportByteLength;
        public fixed short Reserved[17];
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

    internal const int HidpStatusSuccess = 0x00110000;

    [LibraryImport(Libraries.Hid)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial int HidP_GetCaps(
        IntPtr PreparsedData,
        ref HIDP_CAPS Capabilities
    );
}