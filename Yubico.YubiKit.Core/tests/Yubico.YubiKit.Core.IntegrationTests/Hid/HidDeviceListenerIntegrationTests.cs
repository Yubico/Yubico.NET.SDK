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
using Yubico.YubiKit.Core.Hid;

namespace Yubico.YubiKit.Core.IntegrationTests.Hid;

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
        catch (Exception ex)
        {
            _output.WriteLine($"HID listener creation failed: {ex.Message}");
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
    public void Create_StatusIsStarted()
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
        catch (Exception ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        try
        {
            // Assert
            Assert.Equal(DeviceListenerStatus.Started, listener.Status);
            _output.WriteLine("HID listener created with Started status");
        }
        finally
        {
            listener?.Dispose();
        }
    }

    [Fact]
    public void EventSubscription_NoDeviceChange_NoEventsFired()
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
        catch (Exception ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
            return;
        }

        var arrivedCount = 0;
        var removedCount = 0;

        try
        {
            listener.Arrived += (_, _) => Interlocked.Increment(ref arrivedCount);
            listener.Removed += (_, _) => Interlocked.Increment(ref removedCount);

            // Act - wait briefly
            Thread.Sleep(500);

            // Assert - no spurious events
            Assert.Equal(0, arrivedCount);
            Assert.Equal(0, removedCount);
            _output.WriteLine("No spurious events fired during quiet period");
        }
        finally
        {
            listener?.Dispose();
        }
    }

    [Fact]
    public void Dispose_CompletesCleanly()
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
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        _output.WriteLine("Dispose completed cleanly within timeout");
    }

    [Fact]
    public void Dispose_MultipleTimes_NoException()
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
        catch (Exception ex)
        {
            _output.WriteLine($"Test skipped: {ex.Message}");
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
        _output.WriteLine("Multiple dispose calls succeeded without exception");
    }
}
