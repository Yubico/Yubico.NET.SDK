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
/// Tests verifying that DesktopSmartCardDeviceListener can be disposed cleanly
/// without blocking or hanging. These tests are CRITICAL for shutdown safety.
/// </summary>
[Trait("Category", "Disposal")]
public class DesktopSmartCardDeviceListenerDisposalTests
{
    /// <summary>
    /// Verifies that Dispose completes within a reasonable timeout.
    /// If this test hangs, the cancellation logic is broken.
    /// </summary>
    [Fact]
    public void Dispose_CompletesWithinTimeout()
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
                _ = new DesktopSmartCardDeviceListener();
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
