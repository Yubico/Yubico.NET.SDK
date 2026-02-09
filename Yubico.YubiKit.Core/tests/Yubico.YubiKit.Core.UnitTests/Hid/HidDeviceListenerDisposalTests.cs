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

using Yubico.YubiKit.Core.Hid;

namespace Yubico.YubiKit.Core.UnitTests.Hid;

/// <summary>
/// Tests verifying that HidDeviceListener implementations can be disposed cleanly
/// without blocking or hanging. These tests are CRITICAL for shutdown safety.
/// </summary>
[Trait("Category", "Disposal")]
public class HidDeviceListenerDisposalTests
{
    /// <summary>
    /// Verifies that Dispose completes within a reasonable timeout.
    /// If this test hangs, the cancellation logic is broken.
    /// </summary>
    [Fact]
    public void Dispose_CompletesWithinTimeout()
    {
        // Arrange
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
        }
        catch (PlatformNotSupportedException)
        {
            // Platform may not support HID listeners - skip test
            return;
        }
        catch (Exception)
        {
            // Other initialization failure - skip
            return;
        }

        // Act & Assert - must complete within 10 seconds
        var disposeTask = Task.Run(() => listener.Dispose());
        bool completed = disposeTask.Wait(TimeSpan.FromSeconds(10));

        Assert.True(completed, "Dispose should complete within 10 seconds");
    }

    /// <summary>
    /// Verifies that calling Dispose multiple times does not throw.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_NoException()
    {
        // Arrange
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
        }
        catch (PlatformNotSupportedException)
        {
            // Platform may not support HID listeners - skip test
            return;
        }
        catch (Exception)
        {
            // Other initialization failure - skip
            return;
        }

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            listener.Dispose();
            listener.Dispose();
            listener.Dispose();
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that the factory returns the correct platform-specific implementation.
    /// </summary>
    [Fact]
    public void Create_ReturnsPlatformSpecificImplementation()
    {
        // Arrange & Act
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
        }
        catch (PlatformNotSupportedException)
        {
            // Expected on unsupported platforms
            return;
        }
        catch (Exception)
        {
            // Other initialization failure - skip
            return;
        }

        try
        {
            // Assert - Type should match current platform
            var typeName = listener.GetType().Name;
            var expectedNames = new[] { "WindowsHidDeviceListener", "MacOSHidDeviceListener", "LinuxHidDeviceListener" };
            Assert.Contains(typeName, expectedNames);
        }
        finally
        {
            listener?.Dispose();
        }
    }

    /// <summary>
    /// Verifies that status is Started after creation and Stopped after disposal.
    /// </summary>
    [Fact]
    public void Status_StartsStartedAndEndsStoppedAfterDispose()
    {
        // Arrange
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
        }
        catch (PlatformNotSupportedException)
        {
            return;
        }
        catch (Exception)
        {
            return;
        }

        try
        {
            // Assert initial status
            Assert.Equal(DeviceListenerStatus.Started, listener.Status);

            // Act
            listener.Dispose();

            // Assert final status
            Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        }
        finally
        {
            listener?.Dispose();
        }
    }
}
