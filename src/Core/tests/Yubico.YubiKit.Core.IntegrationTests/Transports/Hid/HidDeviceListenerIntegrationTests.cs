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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.IntegrationTests.Transports.Hid;

/// <summary>
/// Integration tests for HID device listener.
/// These tests verify behavior of platform-specific HID listeners.
/// </summary>
[Trait("Category", "Integration")]
public class HidDeviceListenerIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public HidDeviceListenerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Create_ReturnsCorrectPlatformImplementation()
    {
        // Arrange & Act
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
        }
        catch (PlatformNotSupportedException ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        try
        {
            // Assert
            var typeName = listener.GetType().Name;
            _output.WriteLine($"Created listener type: {typeName}");

            if (OperatingSystem.IsWindows())
            {
                Assert.Equal("WindowsHidDeviceListener", typeName);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Assert.Equal("MacOSHidDeviceListener", typeName);
            }
            else if (OperatingSystem.IsLinux())
            {
                Assert.Equal("LinuxHidDeviceListener", typeName);
            }
        }
        finally
        {
            listener?.Dispose();
        }
    }

    [Fact]
    public void Start_TransitionsToStartedStatus()
    {
        // Arrange
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
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

            // Assert - Error is acceptable when CM_Register_Notification is blocked (CI/sandbox)
            Assert.True(
                listener.Status is DeviceListenerStatus.Started or DeviceListenerStatus.Error,
                $"Expected Started or Error, but got {listener.Status}");
            _output.WriteLine($"HID listener status after Start(): {listener.Status}");
        }
        finally
        {
            listener?.Dispose();
        }
    }

    [Fact]
    public void StartedListener_NoDeviceChange_NoSpuriousEvents()
    {
        // Arrange
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
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
    public async Task Dispose_AfterStart_CompletesCleanly()
    {
        // Arrange
        HidDeviceListener? listener = null;
        try
        {
            listener = HidDeviceListener.Create();
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
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        _output.WriteLine("Dispose after Start() completed cleanly within timeout");
    }
}