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

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

/// <summary>
///     Multi-key contention tests: discovery and concurrent sessions with TWO OR MORE YubiKeys
///     plugged in simultaneously. Hardware-only — these skip unless at least two allow-listed keys
///     with a SmartCard interface are connected, and they mutate PIV state (reset) on those dedicated
///     test keys without requiring touch, insertion, or removal during execution.
/// </summary>
/// <remarks>
///     Companion single-key gates: <see cref="PivDiscoveryContentionTests" /> (discovery vs. open
///     session) and <see cref="PivConnectionConcurrencyTests" /> (exchange serialization on one
///     session). These tests cover what single-key runs cannot: enumeration that must merge one
///     in-use device and one free device in the same scan, and fully parallel sessions on distinct
///     physical devices (per-device exchange gates and registry entries must not cross-wire).
/// </remarks>
public class PivMultiKeyContentionTests(ITestOutputHelper output)
{
    private static readonly byte[] DefaultTripleDesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultAesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    private static IReadOnlyList<YubiKeyTestState> GetSmartCardStatesOrSkip(int minimum)
    {
        var states = AuthorizedDevices.GetAll()
            .Where(s => s.Device.SupportsConnection(ConnectionType.SmartCard))
            .ToList();

        Skip.If(states.Count < minimum,
            $"Requires {minimum} allow-listed YubiKeys with a SmartCard interface plugged in " +
            $"simultaneously; found {states.Count}. Plug in additional keys to run this test.");

        return states;
    }

    private static IReadOnlyList<YubiKeyTestState> GetRsa4096SmartCardStatesOrSkip(int minimum)
    {
        var states = AuthorizedDevices.GetAll()
            .Where(s => s.Device.SupportsConnection(ConnectionType.SmartCard)
                        && s.FirmwareVersion >= new FirmwareVersion(5, 7, 0))
            .ToList();

        Skip.If(states.Count < minimum,
            $"Requires {minimum} allow-listed firmware-5.7.0+ YubiKeys with a SmartCard interface " +
            $"plugged in simultaneously; found {states.Count}.");
        return states;
    }

    /// <summary>
    ///     Enumeration with one key in-use and one key free: the scan must fully identify the free
    ///     key, must not clobber the open authenticated session on the busy key, and must still
    ///     report at least as many devices as are physically present (the in-use key surfaces with
    ///     conservative/cached info because discovery skips reads on in-use devices by design).
    /// </summary>
    [SkippableFact]
    public async Task FindAllAsync_WithOpenSessionOnOneKey_IdentifiesOtherKeysAndPreservesSession()
    {
        var states = GetSmartCardStatesOrSkip(2);
        var keyA = states[0];
        var keyB = states[1];

        IReadOnlyList<Core.Abstractions.IYubiKey> devices;

        await using (var session = await keyA.Device.CreatePivSessionAsync())
        {
            await session.ResetAsync();
            await session.AuthenticateAsync(GetDefaultManagementKey(keyA.FirmwareVersion));
            _ = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256, new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Once });
            await session.VerifyPinAsync(DefaultPin);

            var digest = SHA256.HashData("multi-key contention"u8);
            var before = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
            Assert.NotEqual(0, before.Length);

            // Cold caches: identity + metadata reads run for every present device. Key A's smart-card
            // interface is in-use and must be skipped; key B is free and must be read and identified.
            devices = await FindYubiKeys.Create().FindAllAsync(ConnectionType.All);

            Assert.True(devices.Count >= states.Count,
                $"Scan returned {devices.Count} devices but {states.Count} allow-listed keys are plugged in; " +
                "an in-use device must still be enumerated (with conservative info), not dropped.");
            Assert.Contains(devices, device => device.DeviceId == $"ykphysical:{keyB.SerialNumber}");

            // The open, authenticated session on key A survives the multi-device scan.
            var after = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
            Assert.NotEqual(0, after.Length);
        }

        // Session A is closed now; identify SmartCard-capable scan results over user-initiated
        // Management reads (safe: no open session left to disturb). Standalone OTP rows may remain
        // after conservative macOS duplicate-PID disambiguation and cannot answer Management reads.
        var serials = new List<int?>();
        foreach (var device in devices.Where(device => device.SupportsConnection(ConnectionType.SmartCard)))
        {
            serials.Add((await device.GetDeviceInfoAsync()).SerialNumber);
        }

        Assert.Contains(keyA.SerialNumber, serials);
        Assert.Contains(keyB.SerialNumber, serials);
    }

    /// <summary>
    ///     Fully parallel PIV sessions on two distinct physical keys: per-device serialization must
    ///     not cross-wire devices — operations on key A and key B run concurrently and every
    ///     response must come back correct for its own device.
    /// </summary>
    [SkippableFact]
    public async Task ConcurrentPivSessions_OnTwoKeys_OperateIndependently()
    {
        var states = GetSmartCardStatesOrSkip(2);
        var keyA = states[0];
        var keyB = states[1];

        await using var sessionA = await keyA.Device.CreatePivSessionAsync();
        await using var sessionB = await keyB.Device.CreatePivSessionAsync();

        foreach (var (session, state) in new[] { (sessionA, keyA), (sessionB, keyB) })
        {
            await session.ResetAsync();
            await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
            _ = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256, new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Once });
            await session.VerifyPinAsync(DefaultPin);
        }

        const int iterations = 10;

        async Task<int> SignLoopAsync(PivSession session, string label)
        {
            var completed = 0;
            for (var i = 0; i < iterations; i++)
            {
                var digest = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{label}-{i}"));
                var signature = await session.SignOrDecryptAsync(
                    PivSlot.Authentication, PivAlgorithm.EccP256, digest);
                Assert.NotEqual(0, signature.Length);
                completed++;
            }

            return completed;
        }

        // Both loops run truly in parallel — distinct devices, distinct connections, distinct gates.
        var results = await Task.WhenAll(
            Task.Run(() => SignLoopAsync(sessionA, "key-a")),
            Task.Run(() => SignLoopAsync(sessionB, "key-b")));

        Assert.Equal([iterations, iterations], results);
    }

    /// <summary>
    ///     A long on-card operation on one physical key must not serialize PIV work on another key.
    ///     Both directions are exercised so reader enumeration order cannot hide a cross-key gate.
    /// </summary>
    [SkippableFact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey()
    {
        var states = GetRsa4096SmartCardStatesOrSkip(2);
        var keyA = states[0];
        var keyB = states[1];

        await using var sessionA = await keyA.Device.CreatePivSessionAsync();
        await using var sessionB = await keyB.Device.CreatePivSessionAsync();

        Exception? testFailure = null;
        try
        {
            foreach (var (session, state) in new[] { (sessionA, keyA), (sessionB, keyB) })
            {
                await session.ResetAsync();
                await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
                _ = await session.GenerateKeyAsync(
                    PivSlot.Authentication,
                    PivAlgorithm.EccP256,
                    new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Once, TouchPolicy = PivTouchPolicy.Never });
                await session.VerifyPinAsync(DefaultPin);
            }

            await AssertCrossKeyLivenessAsync(sessionA, keyA, sessionB, keyB);
            await AssertCrossKeyLivenessAsync(sessionB, keyB, sessionA, keyA);
        }
        catch (Exception exception)
        {
            testFailure = exception;
        }

        var cleanupFailure = await ResetBothPivApplicationsAsync(sessionA, keyA, sessionB, keyB);

        if (testFailure is not null)
        {
            if (cleanupFailure is not null)
            {
                testFailure.Data["PIV cleanup failure"] = cleanupFailure;
                output.WriteLine("Cleanup also failed after the primary test failure: {0}", cleanupFailure);
            }

            ExceptionDispatchInfo.Capture(testFailure).Throw();
        }

        if (cleanupFailure is not null)
            ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
    }

    private static async Task<Exception?> ResetBothPivApplicationsAsync(
        PivSession sessionA,
        YubiKeyTestState keyA,
        PivSession sessionB,
        YubiKeyTestState keyB)
    {
        var failures = new List<Exception>(2);

        foreach (var (session, key) in new[] { (sessionA, keyA), (sessionB, keyB) })
        {
            try
            {
                await session.ResetAsync();
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException(
                    $"Failed to restore PIV defaults on serial {key.SerialNumber}.", exception));
            }
        }

        return failures.Count switch
        {
            0 => null,
            1 => failures[0],
            _ => new AggregateException("Failed to restore PIV defaults on both test keys.", failures)
        };
    }

    private async Task AssertCrossKeyLivenessAsync(
        PivSession busySession,
        YubiKeyTestState busyKey,
        PivSession responsiveSession,
        YubiKeyTestState responsiveKey)
    {
        var keygenStopwatch = Stopwatch.StartNew();
        var keygenTask = busySession.GenerateKeyAsync(PivSlot.Retired1, PivAlgorithm.Rsa4096);
        var operationElapsed = TimeSpan.Zero;

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            Assert.False(keygenTask.IsCompleted,
                $"RSA-4096 key generation on serial {busyKey.SerialNumber} completed before overlap " +
                "was established; the liveness test proves nothing.");

            var digest = SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    $"cross-key-liveness-{busyKey.SerialNumber}-{responsiveKey.SerialNumber}"));
            var operationStopwatch = Stopwatch.StartNew();
            var signature = await responsiveSession
                .SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest)
                .WaitAsync(TimeSpan.FromSeconds(4));
            operationStopwatch.Stop();
            operationElapsed = operationStopwatch.Elapsed;

            Assert.NotEqual(0, signature.Length);
            Assert.False(keygenTask.IsCompleted,
                $"PIV operation on serial {responsiveKey.SerialNumber} did not finish before RSA-4096 " +
                $"key generation on serial {busyKey.SerialNumber}; cross-key independence was not proven.");
        }
        finally
        {
            // Never abandon an in-flight native transmit, including on a timeout or failed assertion.
            await ((Task)keygenTask).ConfigureAwait(
                ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        }

        var publicKey = await keygenTask;
        keygenStopwatch.Stop();
        Assert.NotNull(publicKey);

        output.WriteLine(
            "Busy serial {0}: RSA-4096 {1:F3}s; responsive serial {2}: PIV sign {3:F3}s.",
            busyKey.SerialNumber,
            keygenStopwatch.Elapsed.TotalSeconds,
            responsiveKey.SerialNumber,
            operationElapsed.TotalSeconds);
    }
}