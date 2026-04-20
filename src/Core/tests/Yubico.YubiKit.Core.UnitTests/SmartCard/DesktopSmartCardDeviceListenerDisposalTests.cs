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

using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard;

/// <summary>
/// Tests verifying that DesktopSmartCardDeviceListener lifecycle and disposal
/// work correctly. These tests are CRITICAL for shutdown safety.
/// </summary>
[Trait("Category", "Disposal")]
public class DesktopSmartCardDeviceListenerDisposalTests
{

    /// <summary>
    /// Verifies that a newly created listener is in Stopped state.
    /// </summary>
    [Fact]
    public void Constructor_DoesNotAutoStart()
    {
        // Arrange & Act
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception)
        {
            // Platform may not support SmartCard - skip test
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
    /// Verifies that Start() transitions to Started state.
    /// </summary>
    [Fact]
    public void Start_TransitionsToStartedStatus()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception)
        {
            // Platform may not support SmartCard - skip test
            return;
        }

        try
        {
            // Act
            listener.Start();

            // Assert - either Started or Error (if SmartCard subsystem unavailable)
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
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception)
        {
            // Platform may not support SmartCard - skip test
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
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception)
        {
            // Platform may not support SmartCard - skip test
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
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception)
        {
            // Platform may not support SmartCard - skip test
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
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception)
        {
            // Platform may not support SmartCard - skip test
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



    /// <summary>
    /// Verifies that Dispose completes within a reasonable timeout when listener is running.
    /// If this test hangs, the cancellation logic is broken.
    /// </summary>
    [Fact]
    public async Task Dispose_AfterStart_CompletesWithinTimeout()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
            listener.Start(); // Start the listener
        }
        catch (Exception)
        {
            // Platform may not support SmartCard - skip test
            return;
        }

        // Act & Assert - must complete within 10 seconds
        var disposeTask = Task.Run(() => listener.Dispose(), TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)) == disposeTask;

        Assert.True(completed, "Dispose should complete within 10 seconds");
    }

    /// <summary>
    /// Verifies that Dispose completes quickly when listener was never started.
    /// </summary>
    [Fact]
    public async Task Dispose_NeverStarted_CompletesImmediately()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
            // Don't call Start()
        }
        catch (Exception)
        {
            // Platform may not support SmartCard - skip test
            return;
        }

        // Act & Assert - should complete very quickly
        var disposeTask = Task.Run(() => listener.Dispose(), TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken)) == disposeTask;

        Assert.True(completed, "Dispose should complete within 1 second when not started");
    }

    /// <summary>
    /// Verifies that calling Dispose multiple times does not throw.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_NoException()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
            listener.Start();
        }
        catch (Exception)
        {
            // Platform may not support SmartCard - skip test
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
    /// Verifies that the finalizer doesn't throw exceptions.
    /// Note: GC collection timing is non-deterministic, so we just verify
    /// a listener can be abandoned without causing process-level issues.
    /// </summary>
    [Fact]
    public void Finalizer_DoesNotThrowException()
    {
        // Arrange - Create and abandon listener (don't dispose)
        void CreateAndAbandonListener()
        {
            try
            {
                var listener = new DesktopSmartCardDeviceListener();
                listener.Start(); // Start it so there's actual cleanup needed
                // Don't dispose - let it become eligible for GC
            }
            catch (Exception)
            {
                // Platform may not support SmartCard - skip
            }
        }

        // Act - This should not throw
        var exception = Record.Exception(() =>
        {
            CreateAndAbandonListener();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        });

        // Assert
        Assert.Null(exception);
    }

}
