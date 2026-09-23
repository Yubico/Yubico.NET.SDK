// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using CoreLibraries = Yubico.YubiKit.Core.Native.Libraries;

namespace Yubico.YubiKit.Core.Native.MacOS.HidInput;

/// <summary>
///     Native entry points for the macOS persistent-input owner in <c>Yubico.NativeShims</c>.
/// </summary>
internal static partial class NativeMethods
{
    [LibraryImport(CoreLibraries.NativeShims, EntryPoint = "Native_HidInputCreate")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static unsafe partial nint HidInputCreate(
        nint device,
        nuint maxReport,
        nuint capacity,
        delegate* unmanaged[Cdecl]<nint, nint, nuint, void> report,
        delegate* unmanaged[Cdecl]<nint, int, void> terminal,
        nint context);

    [LibraryImport(CoreLibraries.NativeShims, EntryPoint = "Native_HidInputStart")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial int HidInputStart(nint owner);

    [LibraryImport(CoreLibraries.NativeShims, EntryPoint = "Native_HidInputCancel")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial void HidInputCancel(nint owner);

    [LibraryImport(CoreLibraries.NativeShims, EntryPoint = "Native_HidInputWaitShutdown")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial int HidInputWaitShutdown(nint owner, uint timeoutMs);

    [LibraryImport(CoreLibraries.NativeShims, EntryPoint = "Native_HidInputDestroy")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial int HidInputDestroy(nint owner);
}

// Values match hidinput/owner.h in NativeShims.
internal static class HidInputResult
{
    internal const int Ok = 0;
    internal const int Busy = 1;
    internal const int Timeout = 2;
    internal const int SelfWait = 3;
    internal const int Fault = 4;
    internal const int Invalid = 5;
    internal const int CloseFault = 6;
}

internal static class HidInputTerminalReason
{
    internal const int Removed = 1;
    internal const int Overflow = 2;
    internal const int Fault = 3;
}
