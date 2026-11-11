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

namespace Yubico.YubiKit.Core.Native.Linux.Libc;

// This class represents the libc file descriptor, the return from a call to
// open.
internal class LinuxFileSafeHandle : SafeHandle
{
    private readonly Func<int, LinuxCloseResult>? _close;
    private LinuxReleaseOutcome _releaseOutcome;
    private int _releaseRecorded;

    public LinuxFileSafeHandle()
        : base(new IntPtr(-1), true)
    {
    }

    internal LinuxFileSafeHandle(Func<int, LinuxCloseResult> close) : this() => _close = close;

    // fd 0 is valid: open can return it when stdin is closed.
    public override bool IsInvalid => handle == new IntPtr(-1);

    internal void Initialize(int descriptor) => SetHandle(descriptor);

    internal LinuxReleaseOutcome CloseOnce()
    {
        Dispose();
        return Volatile.Read(ref _releaseRecorded) == 1 ? _releaseOutcome : default;
    }

    protected override bool ReleaseHandle()
    {
        try
        {
            LinuxCloseResult result;
            if (_close is not null)
            {
                result = _close(handle.ToInt32());
            }
            else
            {
                int nativeResult = NativeMethods.CloseNative(handle.ToInt32());
                int error = Marshal.GetLastPInvokeError();
                result = new LinuxCloseResult(nativeResult, error);
            }

            // Linux releases the descriptor even on EINTR/EIO. Never retry close: the fd
            // number may already belong to another owner. EBADF is an ownership violation,
            // not release proof; all other errors are conservative unknown-release outcomes.
            bool released = result.Result == 0 || (result.Result == -1 && result.Error is 4 or 5);
            _releaseOutcome = new LinuxReleaseOutcome(true, released, result.Result, result.Error);
            Volatile.Write(ref _releaseRecorded, 1);
            return released;
        }
        catch
        {
            // Finalization must not throw. No executed/released proof is published.
            return false;
        }
    }
}

internal readonly record struct LinuxCloseResult(int Result, int Error);

// Executed=false is not proof of release (e.g. Dispose while a native reference is held).
internal readonly record struct LinuxReleaseOutcome(bool Executed, bool Released, int NativeResult, int Error);