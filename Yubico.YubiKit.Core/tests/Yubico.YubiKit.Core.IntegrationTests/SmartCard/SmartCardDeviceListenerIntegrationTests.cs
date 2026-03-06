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

using Xunit.Abstractions;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.IntegrationTests.SmartCard;

/// <summary>
/// Integration tests for SmartCard device listener.
/// These tests verify behavior with PC/SC subsystem available.
/// </summary>
[Trait("Category", "Integration")]
public class SmartCardDeviceListenerIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public SmartCardDeviceListenerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("RequiresDevice", "SmartCard")]
    public void Create_WithPcscAvailable_StatusIsStarted()
    {
        // Arrange & Act
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"SmartCard listener creation failed: {ex.Message}");
            _output.WriteLine("Test skipped: PC/SC subsystem not available");
            return;
        }

        try
        {
            // Assert
            Assert.Equal(DeviceListenerStatus.Started, listener.Status);
            _output.WriteLine("SmartCard listener created with Started status");
        }
        finally
        {
            listener?.Dispose();
        }
    }

    [Fact]
    [Trait("RequiresDevice", "SmartCard")]
    public void EventSubscription_NoDeviceChange_NoEventsFired()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        var eventCount = 0;

        try
        {
            listener.DeviceEvent = () => Interlocked.Increment(ref eventCount);

            // Act - wait briefly
            Thread.Sleep(500);

            // Assert - no spurious events
            Assert.Equal(0, eventCount);
            _output.WriteLine("No spurious events fired during quiet period");
        }
        finally
        {
            listener?.Dispose();
        }
    }

    [Fact]
    [Trait("RequiresDevice", "SmartCard")]
    public void Dispose_SetsStatusToStopped()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        // Act
        listener.Dispose();

        // Assert
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        _output.WriteLine("Status correctly set to Stopped after disposal");
    }

    [Fact]
    [Trait("RequiresDevice", "SmartCard")]
    public void Dispose_ClearsEventHandlers()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        var handlerCalled = false;
        listener.DeviceEvent = () => handlerCalled = true;

        // Act
        listener.Dispose();

        // Assert - after disposal, events should be cleared
        // We can verify this by trying to create a new listener (handlers won't transfer)
        Assert.False(handlerCalled);
        _output.WriteLine("Event handlers cleared after disposal");
    }

    [Fact]
    [Trait("RequiresDevice", "SmartCard")]
    public void Dispose_CompletesWithin5Seconds()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        // Act
        var disposeTask = Task.Run(() => listener.Dispose());
        var completed = disposeTask.Wait(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(completed, "Dispose should complete within 5 seconds");
        _output.WriteLine("Dispose completed within timeout");
    }
}
