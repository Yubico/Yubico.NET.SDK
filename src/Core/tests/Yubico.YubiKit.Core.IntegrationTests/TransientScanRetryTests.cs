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

using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.IntegrationTests;

/// <summary>
///     Pins the narrowness of <see cref="TransientScanRetry" />.
/// </summary>
/// <remarks>
///     These need no hardware — they exist because the helper's whole value depends on its catch being
///     unable to swallow anything but the one transient condition. A retry that hides a real regression
///     is worse than the flake it was added to fix, so that boundary is asserted rather than asserted
///     about in a comment.
/// </remarks>
public class TransientScanRetryTests
{
    private static InvalidOperationException Saturation() =>
        new(FindPcscDevices.WorkerSaturationMessage);

    [Fact]
    public async Task ScanAsync_WhenSaturationClears_RetriesAndReturnsResult()
    {
        var attempts = 0;

        var result = await TransientScanRetry.ScanAsync(() =>
        {
            attempts++;
            return attempts < 3
                ? throw Saturation()
                : Task.FromResult("scanned");
        });

        Assert.Equal("scanned", result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ScanAsync_WhenScanSucceeds_DoesNotRetry()
    {
        var attempts = 0;

        _ = await TransientScanRetry.ScanAsync(() =>
        {
            attempts++;
            return Task.FromResult("scanned");
        });

        Assert.Equal(1, attempts);
    }

    /// <summary>
    ///     The regression this file exists for. <see cref="ObjectDisposedException" /> derives from
    ///     <see cref="InvalidOperationException" /> and carries a caller-supplied message, so without an
    ///     exact-type check a disposal defect wearing the saturation message would be retried away.
    /// </summary>
    [Fact]
    public async Task ScanAsync_DerivedExceptionCarryingSaturationMessage_PropagatesImmediately()
    {
        var attempts = 0;

        var thrown = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => TransientScanRetry.ScanAsync<string>(() =>
            {
                attempts++;
                throw new ObjectDisposedException(null, FindPcscDevices.WorkerSaturationMessage);
            }));

        Assert.Equal(1, attempts);
        Assert.Contains(FindPcscDevices.WorkerSaturationMessage, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScanAsync_UnrelatedInvalidOperationException_PropagatesImmediately()
    {
        var attempts = 0;

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransientScanRetry.ScanAsync<string>(() =>
            {
                attempts++;
                throw new InvalidOperationException("Something else went wrong.");
            }));

        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(" PC/SC device enumeration could not start because discovery worker capacity is saturated; retry the scan.")]
    [InlineData("PC/SC device enumeration could not start because discovery worker capacity is saturated; retry the scan. ")]
    [InlineData("PC/SC DEVICE ENUMERATION COULD NOT START BECAUSE DISCOVERY WORKER CAPACITY IS SATURATED; RETRY THE SCAN.")]
    [InlineData("PC/SC device enumeration could not start")]
    public async Task ScanAsync_NearMissMessage_PropagatesImmediately(string message)
    {
        var attempts = 0;

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransientScanRetry.ScanAsync<string>(() =>
            {
                attempts++;
                throw new InvalidOperationException(message);
            }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ScanAsync_WhenSaturationPersists_ReportsItRatherThanRetryingForever()
    {
        var attempts = 0;

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransientScanRetry.ScanAsync<string>(() =>
            {
                attempts++;
                throw Saturation();
            }));

        Assert.Equal(5, attempts);
        Assert.Contains("5 scan attempts", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(FindPcscDevices.WorkerSaturationMessage, thrown.InnerException?.Message);
    }
}