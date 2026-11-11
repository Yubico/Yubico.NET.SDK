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

using Yubico.YubiKit.Core.Native.Windows.Kernel32;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

public class WindowsHidSafeHandleTests
{
    [Fact]
    public void CloseOnce_UninitializedHandleDoesNotInventNativeReleaseEvidence()
    {
        int calls = 0;
        using var handle = new WindowsHidSafeHandle(_ =>
        {
            calls++;
            return new WindowsHidRelease(true, 0);
        });

        Assert.False(handle.CloseOnce().NativeReleaseProven);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void CloseOnce_ReleaseThrowsDoesNotRetryOrProveRelease()
    {
        int calls = 0;
        using var handle = new WindowsHidSafeHandle(_ =>
        {
            calls++;
            throw new InvalidOperationException("Unknown native release outcome.");
        });
        handle.Initialize(123);

        Assert.Throws<InvalidOperationException>(() => handle.CloseOnce());
        Assert.False(handle.CloseOnce().NativeReleaseProven);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(true, 0u)]
    [InlineData(false, 6u)]
    public void CloseOnce_ReleaseResultIsCachedWithoutRetry(bool success, uint error)
    {
        int calls = 0;
        using var handle = new WindowsHidSafeHandle(value =>
        {
            Assert.Equal((nint)123, value);
            calls++;
            return new WindowsHidRelease(success, error);
        });
        handle.Initialize(123);

        Assert.Equal(new WindowsHidRelease(success, error), handle.CloseOnce());
        Assert.Equal(new WindowsHidRelease(success, error), handle.CloseOnce());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void CloseOnce_OutstandingReferenceDoesNotProveRelease()
    {
        int calls = 0;
        using var handle = new WindowsHidSafeHandle(_ =>
        {
            calls++;
            return new WindowsHidRelease(true, 0);
        });
        handle.Initialize(123);
        bool added = false;
        handle.DangerousAddRef(ref added);
        try
        {
            Assert.False(handle.CloseOnce().NativeReleaseProven);
            Assert.Equal(0, calls);
        }
        finally
        {
            if (added)
            {
                handle.DangerousRelease();
            }
        }

        Assert.True(handle.CloseOnce().NativeReleaseProven);
        Assert.Equal(1, calls);
    }
}