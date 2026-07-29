// Copyright 2026 Yubico AB
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
using Yubico.YubiKit.Core.Native.Desktop.SCard;

namespace Yubico.YubiKit.Core.Native.Windows.WinSCard;

/// <summary>
///     Windows-only WinSCard entry points that the cross-platform <c>Yubico.NativeShims</c> layer does not
///     expose. Bound directly against <see cref="Libraries.WinSCard" /> instead of through the shim,
///     following the same direct-import pattern as Cfgmgr32 and HidD in this folder: a shared
///     <see cref="Libraries" /> constant plus <c>DllImportSearchPath.System32</c>. These methods must only
///     ever be called on Windows — .NET resolves the import lazily on first call, so merely referencing
///     this type on macOS/Linux loads nothing.
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    ///     Maps a PC/SC reader name to the device instance ID of the reader's devnode (Windows 8+).
    /// </summary>
    /// <remarks>
    ///     Two-call pattern: pass a null buffer to learn the required character count, then call again with
    ///     a buffer of that size. Returns <c>SCARD_S_SUCCESS</c> or an <c>SCARD_E_*</c> code (see
    ///     <see cref="ErrorCode" />); <c>SCARD_E_UNKNOWN_READER</c> and
    ///     <c>SCARD_E_READER_UNAVAILABLE</c> are ordinary outcomes mid-hotplug, not faults.
    /// </remarks>
    [LibraryImport(Libraries.WinSCard, EntryPoint = "SCardGetReaderDeviceInstanceIdW",
        StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint SCardGetReaderDeviceInstanceId(
        SCardContext context,
        string readerName,
        [Out] char[]? deviceInstanceId,
        ref int deviceInstanceIdSizeInChars);
}