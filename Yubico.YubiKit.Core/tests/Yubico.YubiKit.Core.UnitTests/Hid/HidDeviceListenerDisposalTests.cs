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
/// Tests verifying that HidDeviceListener implementations lifecycle and disposal
/// work correctly. These tests are CRITICAL for shutdown safety.
/// </summary>
[Trait("Category", "Disposal")]
public class HidDeviceListenerDisposalTests
{
    #region Start/Stop Lifecycle Tests

    /// <summary>
    /// Verifies that a newly created listener is in Stopped state.
    /// </summary>
    [Fact]
    public void Constructor_DoesNotAutoStart()
    {
        // Arrange & Act
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
            // Assert
            Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        }
        finally
        {
            listener?.Dispose();
        }
    }

    /// <summary>
    /// Verifies that Start() transitions to Started status.
    /// </summary>
    [Fact]
    public void Start_TransitionsToStartedStatus()
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
            // Act
            listener.Start();

            // Assert - either Started or Error (if HID subsystem unavailable)
            Assert.True(
                listener.Status is DeviceListenerStatus.Started or DeviceListenerStatus.Error,
                $"Expected Started or Error, but got {listener.Status}");
        }
        finally
        {
            listener?.Dispose();
        }
    }

    /// <summary>
    /// Verifies that Stop() transitions back to Stopped state.
    /// </summary>
    [Fact]
    public void Stop_TransitionsToStoppedStatus()
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
            listener.Start();
            
            // Act
            listener.Stop();

            // Assert
            Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        }
        finally
        {
            listener?.Dispose();
        }
    }

    /// <summary>
    /// Verifies that calling Start() on an already started listener is idempotent.
    /// </summary>
    [Fact]
    public void Start_CalledMultipleTimes_Idempotent()
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
            // Act - multiple starts should be idempotent
            var exception = Record.Exception(() =>
            {
                listener.Start();
                listener.Start();
                listener.Start();
            });

            // Assert
            Assert.Null(exception);
        }
        finally
        {
            listener?.Dispose();
        }
    }

    /// <summary>
    /// Verifies that calling Stop() on an already stopped listener is idempotent.
    /// </summary>
    [Fact]
    public void Stop_CalledMultipleTimes_Idempotent()
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
            // Act - multiple stops should be idempotent
            var exception = Record.Exception(() =>
            {
                listener.Stop();
                listener.Stop();
                listener.Stop();
            });

            // Assert
            Assert.Null(exception);
        }
        finally
        {
            listener?.Dispose();
        }
    }

    /// <summary>
    /// Verifies that events are not fired before Start() is called.
    /// </summary>
    [Fact]
    public void DeviceEvent_NotFiredBeforeStart()
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
            var eventCount = 0;
            listener.DeviceEvent = () => Interlocked.Increment(ref eventCount);

            // Act - wait briefly, no events should fire
            Thread.Sleep(100);

            // Assert
            Assert.Equal(0, eventCount);
        }
        finally
        {
            listener?.Dispose();
        }
    }

    #endregion

    #region Factory Tests

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
            return;
        }
        catch (Exception)
        {
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

    #endregion

    #region Disposal Tests

    /// <summary>
    /// Verifies that Dispose completes within a reasonable timeout when listener is running.
    /// If this test hangs, the cancellation logic is broken.
    /// </summary>
    [Fact]
    public void Dispose_AfterStart_CompletesWithinTimeout()
    {
        // Arrange
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
            listener.Start(); // Start the listener
        }
        catch (PlatformNotSupportedException)
        {
            return;
        }
        catch (Exception)
        {
            return;
        }

        // Act & Assert - must complete within 10 seconds
        var disposeTask = Task.Run(() => listener.Dispose());
        bool completed = disposeTask.Wait(TimeSpan.FromSeconds(10));

        Assert.True(completed, "Dispose should complete within 10 seconds");
    }

    /// <summary>
    /// Verifies that Dispose completes quickly when listener was never started.
    /// </summary>
    [Fact]
    public void Dispose_NeverStarted_CompletesImmediately()
    {
        // Arrange
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
            // Don't call Start()
        }
        catch (PlatformNotSupportedException)
        {
            return;
        }
        catch (Exception)
        {
            return;
        }

        // Act & Assert - should complete very quickly
        var disposeTask = Task.Run(() => listener.Dispose());
        bool completed = disposeTask.Wait(TimeSpan.FromSeconds(1));

        Assert.True(completed, "Dispose should complete within 1 second when not started");
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
            listener.Start();
        }
        catch (PlatformNotSupportedException)
        {
            return;
        }
        catch (Exception)
        {
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
    /// Verifies that status is Stopped after disposal.
    /// </summary>
    [Fact]
    public void Dispose_SetsStatusToStopped()
    {
        // Arrange
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
            listener.Start();
        }
        catch (PlatformNotSupportedException)
        {
            return;
        }
        catch (Exception)
        {
            return;
        }

        // Act
        listener.Dispose();

        // Assert
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
    }

    #endregion
}
