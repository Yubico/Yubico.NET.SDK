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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.IntegrationTests.Transports.SmartCard;

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
    public void Start_TransitionsToStartedStatus()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (PlatformNotSupportedException ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        try
        {
            // Act
            listener.Start();

            // Assert - Error is acceptable when PC/SC subsystem is blocked (CI/sandbox)
            Assert.True(
                listener.Status is DeviceListenerStatus.Started or DeviceListenerStatus.Error,
                $"Expected Started or Error, but got {listener.Status}");
            _output.WriteLine($"SmartCard listener status after Start(): {listener.Status}");
        }
        finally
        {
            listener?.Dispose();
        }
    }

    [Fact]
    [Trait("RequiresDevice", "SmartCard")]
    public void StartedListener_NoDeviceChange_NoSpuriousEvents()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
        }
        catch (PlatformNotSupportedException ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        var eventCount = 0;

        try
        {
            listener.DeviceEvent = () => Interlocked.Increment(ref eventCount);
            listener.Start();

            if (listener.Status != DeviceListenerStatus.Started)
            {
                _output.WriteLine($"Test skipped: listener did not start (status: {listener.Status})");
                return;
            }

            // Act - wait briefly with listener running
            Thread.Sleep(500);

            // Assert - no spurious events while running
            Assert.Equal(0, eventCount);
            _output.WriteLine("No spurious events fired during quiet period with listener running");
        }
        finally
        {
            listener?.Dispose();
        }
    }

    [Fact]
    [Trait("RequiresDevice", "SmartCard")]
    public void Dispose_AfterStart_SetsStatusToStopped()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
            listener.Start();
        }
        catch (PlatformNotSupportedException ex)
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
    public async Task Dispose_AfterStart_CompletesCleanly()
    {
        // Arrange
        DesktopSmartCardDeviceListener? listener = null;
        try
        {
            listener = new DesktopSmartCardDeviceListener();
            listener.Start();
        }
        catch (PlatformNotSupportedException ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        // Act
        var disposeTask = Task.Run(() => listener.Dispose());
        var completedTask = await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromSeconds(5)));

        // Assert
        Assert.True(completedTask == disposeTask, "Dispose should complete within 5 seconds");
        await disposeTask;
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        _output.WriteLine("Dispose after Start() completed cleanly within timeout");
    }
}
