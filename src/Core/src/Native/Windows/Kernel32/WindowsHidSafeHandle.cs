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

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.Native.Windows.Kernel32;

/// <summary>Owns one HID handle and records actual CloseHandle evidence.</summary>
internal sealed class WindowsHidSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private readonly Func<nint, WindowsHidRelease> _close;
    private uint _releaseError;
    private int _releaseState;

    internal WindowsHidSafeHandle() : this(CloseNative) { }

    // Per-instance seam exercises SafeHandle reference drain without invoking Windows on test hosts.
    internal WindowsHidSafeHandle(Func<nint, WindowsHidRelease> close) : base(true) => _close = close;

    internal void Initialize(nint value) => SetHandle(value);

    internal WindowsHidRelease CloseOnce()
    {
        Dispose();
        int state = Volatile.Read(ref _releaseState);
        return new WindowsHidRelease(state == 1, _releaseError);
    }

    protected override bool ReleaseHandle()
    {
        WindowsHidRelease result = _close(handle);
        _releaseError = result.ErrorCode;
        Volatile.Write(ref _releaseState, result.NativeReleaseProven ? 1 : 2);
        return result.NativeReleaseProven;
    }

    private static WindowsHidRelease CloseNative(nint value)
    {
        int result = NativeMethods.CloseHidHandle(value);
        uint error = result == 0 ? unchecked((uint)Marshal.GetLastPInvokeError()) : 0;
        return new WindowsHidRelease(result != 0, error);
    }
}