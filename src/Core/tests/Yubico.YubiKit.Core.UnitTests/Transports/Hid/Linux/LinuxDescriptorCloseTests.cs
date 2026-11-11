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
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

public class LinuxDescriptorCloseTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(-1, 4, true)]
    [InlineData(-1, 5, true)]
    [InlineData(-1, 9, false)]
    [InlineData(-1, 22, false)]
    public void CheckedClose_CachesExactOutcomeWithoutRetry(int result, int error, bool released)
    {
        int calls = 0;
        using var handle = new LinuxFileSafeHandle(_ =>
        {
            calls++;
            return new LinuxCloseResult(result, error);
        });
        handle.Initialize(0);
        Assert.False(handle.IsInvalid);
        var first = handle.CloseOnce();
        Assert.Equal(new LinuxReleaseOutcome(true, released, result, error), first);
        Assert.Equal(first, handle.CloseOnce());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void CheckedClose_WithBorrower_DoesNotClaimReleaseUntilLastReferenceDrains()
    {
        int calls = 0;
        using var handle = new LinuxFileSafeHandle(_ =>
        {
            calls++;
            return new LinuxCloseResult(0, 0);
        });
        handle.Initialize(17);
        bool acquired = false;
        handle.DangerousAddRef(ref acquired);
        try
        {
            Assert.Equal(default, handle.CloseOnce());
            Assert.Equal(0, calls);
        }
        finally
        {
            if (acquired)
                handle.DangerousRelease();
        }
        Assert.True(handle.CloseOnce().Released);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void RealClose_ReusedDescriptorIsNotClosedByRepeatedDispose()
    {
        if (!OperatingSystem.IsLinux())
            return;
        using var handle = NativeMethods.Open("/dev/null", NativeMethods.OpenFlags.O_RDWR);
        int fd = handle.DangerousGetHandle().ToInt32();
        Assert.True(handle.CloseOnce().Released);
        using var replacement = NativeMethods.Open("/dev/null", NativeMethods.OpenFlags.O_RDWR);
        // Isolated fd0 probe additionally proves exact-number reuse without other test threads.
        handle.CloseOnce();
        Assert.Equal(1, NativeMethods.Write(replacement, [42], 1));
        Assert.True(fd >= 0);
    }

    [Fact]
    public async Task ConcurrentClose_WhileNativeReleaseIsWithheld_DoesNotPublishPrematureProof()
    {
        using var probe = new WithheldNativeProbe();
        int calls = 0;
        using var handle = new LinuxFileSafeHandle(_ =>
        {
            Interlocked.Increment(ref calls);
            probe.EnterNative();
            return new LinuxCloseResult(-1, 5);
        });
        handle.Initialize(21);
        // The worker is test-owned, not a production async guarantee in L1.
        probe.Start(() => Task.Run(() => handle.CloseOnce(), TestContext.Current.CancellationToken));
        try
        {
            probe.AssertResponsive();
            Assert.Equal(default, handle.CloseOnce());
            Assert.Equal(1, Volatile.Read(ref calls));
        }
        finally
        {
            await probe.ReleaseAndDrainAsync();
        }
        Assert.Equal(new LinuxReleaseOutcome(true, true, -1, 5), handle.CloseOnce());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ThrowingRelease_DoesNotEscapeOrPublishFalseReleaseProof()
    {
        int calls = 0;
        using var handle = new LinuxFileSafeHandle(_ =>
        {
            calls++;
            throw new InvalidOperationException("Injected release failure");
        });
        handle.Initialize(19);
        Assert.Equal(default, handle.CloseOnce());
        Assert.Equal(default, handle.CloseOnce());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void LastBorrowerRelease_PreservesOperationErrnoAcrossDeferredClose()
    {
        using var handle = new LinuxFileSafeHandle(_ =>
        {
            Marshal.SetLastPInvokeError(5);
            return new LinuxCloseResult(-1, 5);
        });
        handle.Initialize(20);
        var lease = new LinuxDescriptorLease(handle);
        Assert.Equal(default, handle.CloseOnce());
        Marshal.SetLastPInvokeError(11);
        lease.Dispose();
        Assert.Equal(11, Marshal.GetLastPInvokeError());
        Assert.Equal(new LinuxReleaseOutcome(true, true, -1, 5), handle.CloseOnce());
    }
}