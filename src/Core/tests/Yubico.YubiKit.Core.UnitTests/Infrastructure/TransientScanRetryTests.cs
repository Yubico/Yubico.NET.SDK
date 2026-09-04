// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Infrastructure;

public class TransientScanRetryTests
{
    private static InvalidOperationException Saturation() =>
        new(FindPcscDevices.WorkerSaturationMessage);

    [Fact]
    public async Task ScanAsync_WhenSaturationClears_RetriesAndReturnsResult()
    {
        var attempts = 0;
        var delays = new List<TimeSpan>();

        var result = await TransientScanRetry.ScanAsync(
            () =>
            {
                attempts++;
                return attempts < 3
                    ? throw Saturation()
                    : Task.FromResult("scanned");
            },
            duration =>
            {
                delays.Add(duration);
                return Task.CompletedTask;
            });

        Assert.Equal("scanned", result);
        Assert.Equal(3, attempts);
        Assert.Equal([TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(400)], delays);
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

    public static IEnumerable<object[]> NearMissMessages()
    {
        var message = FindPcscDevices.WorkerSaturationMessage;
        yield return [$" {message}"];
        yield return [$"{message} "];
        yield return [message.ToUpperInvariant()];
        yield return [message[..30]];
    }

    [Theory]
    [MemberData(nameof(NearMissMessages))]
    public async Task ScanAsync_NearMissMessage_PropagatesImmediately(string nearMiss)
    {
        var attempts = 0;
        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TransientScanRetry.ScanAsync<string>(() =>
            {
                attempts++;
                throw new InvalidOperationException(nearMiss);
            }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void IsExhaustion_UnrelatedInvalidOperationExceptions_ReturnsFalse()
    {
        Assert.False(TransientScanRetry.IsExhaustion(Saturation()));
        Assert.False(TransientScanRetry.IsExhaustion(
            new ObjectDisposedException(null, FindPcscDevices.WorkerSaturationMessage)));
    }

    [Fact]
    public async Task ScanAsync_WhenSaturationPersists_ReportsAttemptsAndDelaySchedule()
    {
        var attempts = 0;
        var delays = new List<TimeSpan>();

        var thrown = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            TransientScanRetry.ScanAsync<string>(
                () =>
                {
                    attempts++;
                    throw Saturation();
                },
                duration =>
                {
                    delays.Add(duration);
                    return Task.CompletedTask;
                }));

        Assert.Equal(5, attempts);
        Assert.Equal(
            [
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(400),
                TimeSpan.FromMilliseconds(800),
                TimeSpan.FromMilliseconds(1600)
            ],
            delays);
        Assert.Contains("5 scan attempts", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(FindPcscDevices.WorkerSaturationMessage, thrown.InnerException?.Message);
        Assert.True(TransientScanRetry.IsExhaustion(thrown));
    }
}